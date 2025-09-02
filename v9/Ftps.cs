#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Open source software https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System.Net;
using System.Text;

using Diev.Extensions.Credentials;
using Diev.Extensions.LogFile;
using Diev.Extensions.Tools;

using Microsoft.Extensions.Configuration;

namespace FTPSReportsDownloader;

public static class Ftps
{
    private static readonly IConfiguration _config;

    /// <summary>
    /// Учетная запись в Credential Manager.
    /// </summary>
    public static string TargetName { get; }

    /// <summary>
    /// Логин и пароль доступа к серверу.
    /// </summary>
    public static NetworkCredential User { get; }

    /// <summary>
    /// Папка для загрузки файлов на локальный диск.
    /// </summary>
    public static string DownloadDirectory { get; }

    /// <summary>
    /// За сколько дней скачивать, если файл истории выкладок отсутствует.
    /// </summary>
    public static int DownloadDays { get; }

    static Ftps()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // required for Logger in win-1251

        _config = AppSettingsHelper.Config();

        TargetName = _config[nameof(TargetName)] ?? "ftp://ftps.moex.com";
        DownloadDirectory = _config[nameof(DownloadDirectory)] ?? "temp";
        DownloadDays = int.Parse(_config[nameof(DownloadDays)] ?? "14");

        var cred = CredentialManager.ReadCredential(TargetName);
        User = new NetworkCredential(cred.UserName, cred.Password);
    }

    /// <summary>
    /// Задача синхронизации файлов на сервере и локальном диске.
    /// </summary>
    /// <returns>Код возврата для программы.</returns>
    public static async Task<int> SyncAsync()
    {
        const int retries = 5;
        const int timeout = 1000;

        string server = _config["RemoteHistory"] ?? "/UpdateHistory.txt";
        string client = _config["LocalHistory"] ?? "UpdateHistory.txt";

        Logger.FileNameFormat = _config["Logger:FileNameFormat"] ?? @"logs\{0:yyyy-MM}\{0:yyyy-MM-dd}.log";
        Logger.LineFormat = _config["Logger:LineFormat"] ?? @"{0:HH:mm:ss} {1}";
        Logger.LogToConsole = bool.Parse(_config["Logger:LogToConsole"] ?? "false");
        Logger.AutoFlush = bool.Parse(_config["Logger:AutoFlush"] ?? "false");
        Logger.Reset();

        Directory.CreateDirectory(DownloadDirectory);

        //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; //TODO obsolete

        int compare = CompareSizeOfFile(server, client);

        if (compare == 0)
        {
            Logger.Flush();
            return 0;
        }

        string[] list;

        if (compare > 0)
        {
            list = await GetDiffAsync(server, client); //TODO Diff!
        }
        else if (await DownloadFileAsync(server, client))
        {
            list = await File.ReadAllLinesAsync(client);
            Logger.TimeLine($"Перезагрузка за последние {DownloadDays} дней.");
        }
        else
        {
            Logger.Flush();
            return 0;
        }

        var dateFrom = DateTime.Now.AddDays(-DownloadDays).ToString("yyyyMMdd");
        int counter = 0;

        foreach (var item in list)
        {
            // "/EQ/20230526/PC01101_EQMLIST_001_260523_025153489.xml.p7s.zip.p7e"
            // Допстраховка от сбоев, когда возникает желание перезагрузить весь список от начала...
            if (item[3] != '/' || string.Compare(item, 4, dateFrom, 0, 8) < 0)
            {
                continue;
            }

            string file = Path.Combine(DownloadDirectory, Path.GetFileName(item));
            bool ok = false;

            for (int i = 0; i <= retries; i++)
            {
                if (await DownloadFileAsync(item, file)) //overwrite, no resume
                {
                    ok = true;
                    counter++;
                    break;
                }

                Thread.Sleep(timeout);
                Logger.TimeLine($"Попытка повторить {i + 1}/{retries}...");
            }

            if (!ok)
            {
                Logger.TimeLine($"Не удалось загрузить {item}");
            }
        }

        Logger.TimeLine($"Загружено {counter} из {list.Length}.");
        Logger.Flush();

        return 0;
    }

    /// <summary>
    /// Скачать файл истории выкладок, что на сервере, в файл на клиенте.
    /// </summary>
    /// <param name="serverPath">Путь к файлу истории на сервере.</param>
    /// <param name="localPath">Путь к файлу истории на локальном диске.</param>
    /// <returns>Массив строк с новыми файлами по сравнению с локальной копией файла истории.</returns>
    private static async Task<string[]> GetDiffAsync(string serverPath, string localPath)
    {
        var lines = new List<string>();

        try
        {
            using var stream = await GetFtpDataAsync(serverPath, localPath, true);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is null) break;
                lines.Add(line);
            }

            return [.. lines];
        }
        catch (WebException e)
        {
            Logger.TimeLine("Download file Error: " + e.Message);
            return [];
        }
    }

    /// <summary>
    /// Скачать файл с сервера.
    /// </summary>
    /// <param name="serverPath">Путь к файлу на сервере.</param>
    /// <param name="localPath">Путь к файлу на локальном диске.</param>
    /// <param name="resume">Использовать ли докачку.</param>
    /// <returns>Выполнение завершено успешно (true/false).</returns>
    private static async Task<bool> DownloadFileAsync(string serverPath, string localPath, bool resume = false)
    {
        try
        {
            var file = await SaveFtpDataAsync(serverPath, localPath, resume);

            if (file.Exists && file.Length == 0)
            {
                Logger.TimeLine("Файл был скачан нулевой длины и будет удален - проверьте!");
                file.Delete();
                return false;
            }

            return file.Exists;
        }
        catch (WebException e)
        {
            var x = (FtpWebResponse?)e.Response;

            if (x == null)
            {
                Logger.TimeLine("Ответ от сервера не получен.");
                return false;
            }

            if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
            {
                Logger.TimeLine("Файл был удален в связи с истечением срока давности.");
                return true; // it's normal to skip it
            }

            Logger.TimeLine("Download file Error: " + e.Message);

            var file = new FileInfo(localPath);

            if (file.Exists)
            {
                file.Delete();
            }

            return false;
        }
    }

    /// <summary>
    /// Сравнить размер файла на сервере и на диске.
    /// </summary>
    /// <param name="serverPath">Путь к файлу на сервере.</param>
    /// <param name="localPath">Путь к файлу на локальном диске.</param>
    /// <returns>0 - скачивать не надо (или нет связи);
    /// 1 - надо докачать (размер вырос);
    /// -1 - ошибка (надо скачать заново).</returns>
    private static int CompareSizeOfFile(string serverPath, string localPath)
    {
        var file = new FileInfo(localPath);

        if (!file.Exists)
        {
            return -1;
        }

        try
        {
            long size = GetFtpSize(serverPath);
            long localSize = file.Length;

            return (size == localSize)
                ? 0
                : (size > localSize)
                    ? 1
                    : -1;
        }
        catch (WebException e)
        {
            var x = (FtpWebResponse?)e.Response;

            if (x == null)
            {
                Logger.TimeLine("Ответ от сервера не получен.");
                return 0;
            }

            if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
            {
                Logger.TimeLine("Файл был удален в связи с истечением срока давности.");
                return 0;
            }

            Logger.TimeLine("Size of file Error: " + e.Message);
            return -1;
        }
    }

    private static async Task<MemoryStream> GetFtpDataAsync(string serverPath, string localPath, bool resume = false)
    {
        var file = new FileInfo(localPath);
        long position = (resume && file.Exists) ? file.Length : 0;

        var stream = new MemoryStream();
        using (var ftpStream = GetResponse(serverPath, WebRequestMethods.Ftp.DownloadFile, position).GetResponseStream())
            await ftpStream.CopyToAsync(stream);

        stream.Position = position;
        var mode = position > 0 ? FileMode.Append : FileMode.Create;
        using (var fileStream = new FileStream(localPath, mode))
            await stream.CopyToAsync(fileStream);

        stream.Position = position;
        return stream;
    }

    private static async Task<FileInfo> SaveFtpDataAsync(string serverPath, string localPath, bool resume = false)
    {
        var file = new FileInfo(localPath);
        long position = (resume && file.Exists) ? file.Length : 0;

        var mode = position > 0 ? FileMode.Append : FileMode.Create;
        using (var fileStream = new FileStream(localPath, mode))
        using (var ftpStream = GetResponse(serverPath, WebRequestMethods.Ftp.DownloadFile, position).GetResponseStream())
            await ftpStream.CopyToAsync(fileStream);

        return new FileInfo(localPath);
    }

    private static long GetFtpSize(string serverPath)
    {
        using var response = GetResponse(serverPath, WebRequestMethods.Ftp.GetFileSize);
        return response.ContentLength;
    }

    /// <summary>
    /// Получить бинарное содержимое указанного файла.
    /// </summary>
    /// <param name="serverPath">Путь к файлу на сервере.</param>
    /// <param name="method">Метод получения (FTP, загрузить файл).</param>
    /// <param name="contentOffset">Место смещения в файле (не 0 при докачивании).</param>
    /// <returns>Возвращает ответ FTP-сервера.</returns>
    private static FtpWebResponse GetResponse(string serverPath, string method = WebRequestMethods.Ftp.DownloadFile, long contentOffset = 0)
    {
#pragma warning disable SYSLIB0014 // Тип или член устарел (WebRequest.Create да, но не FtpWebRequest!)
        var request = (FtpWebRequest)WebRequest.Create(TargetName + serverPath); //TODO replace obsolete
#pragma warning restore SYSLIB0014 // Тип или член устарел

        request.Proxy = null; // Игнорировать настройки прокси (TODO: сделать поддержку прокси).
        request.Credentials = User;
        request.Method = method;
        request.EnableSsl = true;
        request.UseBinary = true;
        request.ContentLength = contentOffset;

        Logger.TimeLine(contentOffset > 0
            ? $"< {method} {serverPath} {contentOffset}"
            : $"< {method} {serverPath}");

        var response = (FtpWebResponse)request.GetResponse();

        if (response.StatusCode != FtpStatusCode.DataAlreadyOpen && response.StatusCode != FtpStatusCode.OpeningData)
        {
            // StatusDescription tails an extra NewLine!
            string line = (response.StatusDescription ?? "").Replace("\r\n", ""); //TODO better

            Logger.TimeLine($"> {line}");
        }

        return response;
    }
}
