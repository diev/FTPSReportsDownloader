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
    private static int _count = 0;

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
        //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; //TODO obsolete

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
        try
        {
            Logger.FileNameFormat = _config["Logger:FileNameFormat"] ?? @"logs\{0:yyyy-MM}\{0:yyyy-MM-dd}.log";
            Logger.LineFormat = _config["Logger:LineFormat"] ?? @"{0:HH:mm:ss} {1}";
            Logger.LogToConsole = bool.Parse(_config["Logger:LogToConsole"] ?? "false");
            Logger.AutoFlush = bool.Parse(_config["Logger:AutoFlush"] ?? "false");
            Logger.Reset();

            Directory.CreateDirectory(DownloadDirectory);

            string serverPath = _config["RemoteHistory"] ?? "/UpdateHistory.txt";
            string localPath = _config["LocalHistory"] ?? "UpdateHistory.txt";

            long serverSize = await GetFileSizeAsync(serverPath);

            var history = new FileInfo(localPath);

            if (history.Exists)
            {
                long localSize = history.Length;

                if (serverSize == localSize)
                {
                    return 0;
                }

                if (serverSize > localSize)
                {
                    using var stream = new MemoryStream((int)(serverSize - localSize));
                    await DownloadStreamAsync(serverPath, stream, localSize);

                    using var fileStream = new FileStream(localPath, FileMode.Append, FileAccess.Write);
                    using var writer = new StreamWriter(fileStream);

                    stream.Position = 0;
                    using var reader = new StreamReader(stream, Encoding.ASCII);

                    while (!reader.EndOfStream)
                    {
                        var item = reader.ReadLine()!;
                        var path = Path.Combine(DownloadDirectory, Path.GetFileName(item));
                        await DownloadFileAsync(item, path);

                        if (!File.Exists(path))
                        {
                            throw new Exception("Файл не загружен.");
                        }

                        _count++;
                        writer.WriteLine(item);
                        //TODO writer.Write(item + "\r\n"); //not Windows
                    }

                    return 0;
                }

                File.Delete(localPath);
            }

            Logger.TimeLine($"Перезагрузка за последние {DownloadDays} дней.");
            var dateFrom = DateTime.Now.AddDays(-DownloadDays).ToString("yyyyMMdd");

            {
                using var stream = new MemoryStream((int)serverSize);
                await DownloadStreamAsync(serverPath, stream, 0);

                using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(fileStream);

                stream.Position = 0;
                using var reader = new StreamReader(stream, Encoding.ASCII);

                while (!reader.EndOfStream)
                {
                    var item = reader.ReadLine()!;

                    // "/EQ/20230526/PC01101_EQMLIST_001_260523_025153489.xml.p7s.zip.p7e"
                    // Допстраховка от сбоев, когда возникает желание перезагрузить весь список от начала...
                    if (item.Length > 12
                        && item[3] == '/'
                        && string.Compare(item, 4, dateFrom, 0, 8) >= 0)
                    {
                        var path = Path.Combine(DownloadDirectory, Path.GetFileName(item));
                        await DownloadFileAsync(item, path);

                        if (!File.Exists(path))
                        {
                            throw new Exception("Файл не загружен.");
                        }

                        _count++;
                    }

                    writer.WriteLine(item);
                    //TODO writer.Write(item + "\r\n"); //not Windows
                }
            }

            Logger.TimeLine($"Загружено {_count} файлов.");
            return 0;
        }
        catch (Exception e)
        {
            if (_count > 0)
            {
                Logger.TimeLine($"Загружено {_count} файлов.");
            }

            Logger.TimeLine(e.Message);
            return 1;
        }
        finally
        {
            Logger.Flush();
        }
    }

    private static async Task<long> GetFileSizeAsync(string serverPath)
    {
        using var response = await GetResponseAsync(serverPath, WebRequestMethods.Ftp.GetFileSize);
        return response.ContentLength;
    }

    private static async Task DownloadStreamAsync(string serverPath, Stream stream, long contentOffset)
    {
        using var response = await GetResponseAsync(serverPath, WebRequestMethods.Ftp.DownloadFile, contentOffset);
        using var responseStream = response.GetResponseStream();

        await responseStream.CopyToAsync(stream);
    }

    private static async Task DownloadFileAsync(string serverPath, string localPath, long contentOffset = 0)
    {
        var mode = contentOffset == 0 ? FileMode.Create : FileMode.Append;
        using var fileStream = new FileStream(localPath, mode, FileAccess.Write);

        await DownloadStreamAsync(serverPath, fileStream, contentOffset);
    }

    /// <summary>
    /// Получить бинарное содержимое указанного файла.IZE
    /// </summary>
    /// <param name="serverPath">Путь к файлу на сервере.</param>
    /// <param name="method">Метод получения (FTP, загрузить файл).</param>
    /// <param name="contentOffset">Место смещения в файле (не 0 при докачивании).</param>
    /// <returns>Возвращает ответ FTP-сервера.</returns>
    private static async Task<FtpWebResponse> GetResponseAsync(string serverPath, string method = WebRequestMethods.Ftp.DownloadFile, long contentOffset = 0)
    {
        const int retries = 5;
        const int timeout = 3;

#pragma warning disable SYSLIB0014 // Тип или член устарел (WebRequest.Create да, но не FtpWebRequest!)
        var request = (FtpWebRequest)WebRequest.Create(TargetName + serverPath); //TODO replace obsolete
#pragma warning restore SYSLIB0014 // Тип или член устарел

        //request.Proxy = new WebProxy("http://proxy.com:3128");
        request.Proxy = null;
        request.Credentials = User;
        request.Method = method;
        request.EnableSsl = true;
        request.ContentOffset = contentOffset;

        Logger.TimeLine(contentOffset > 0
            ? $"< {method} {serverPath} {contentOffset}"
            : $"< {method} {serverPath}");

        string error = "Ошибка FTP.";

        for (int retry = 1; retry <= retries; retry++)
        {
            try
            {
                var response = (FtpWebResponse)request.GetResponse();

                if (response.StatusCode != FtpStatusCode.DataAlreadyOpen && response.StatusCode != FtpStatusCode.OpeningData)
                {
                    // StatusDescription tails an extra NewLine!
                    string line = (response.StatusDescription ?? "").Replace("\r\n", ""); //TODO better

                    Logger.TimeLine($"> {line}");
                }

                return response;
            }
            catch (WebException e)
            {
                var x = (FtpWebResponse?)e.Response;

                if (x is null)
                {
                    error = "Ответ сервера не получен.";
                }
                else
                {
                    error = $"> {x.StatusDescription}";

                    if ((int)x.StatusCode > 499)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                break;
            }

            Logger.TimeLine(error);
            var delay = TimeSpan.FromSeconds(Math.Pow(timeout, retry));
            Logger.TimeLine($"Повтор {retry}/{retries} через {delay.Seconds} сек...");

            await Task.Delay(delay);
        }

        throw new Exception(error);
    }
}
