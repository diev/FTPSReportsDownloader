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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using static Lib.Config;
using static Lib.Logger;

namespace FTPSReportsDownloader
{
    public static class Ftps
    {
        /// <summary>
        /// Uri сервера.
        /// </summary>
        public static string Server { get; set; }

        /// <summary>
        /// Логин и пароль доступа к серверу.
        /// </summary>
        public static NetworkCredential User { get; set; }

        /// <summary>
        /// Папка для загрузки файлов на локальный диск.
        /// </summary>
        public static string DownloadDirectory { get; set; }

        /// <summary>
        /// Файл с историей выкладок файлов на сервере.
        /// </summary>
        public static string UpdateHistory { get; set; } = "/UpdateHistory.txt";

        /// <summary>
        /// За сколько дней скачивать, если файл истории выкладок отсутствует.
        /// </summary>
        public static int DownloadDays { get; set; } = 14;

        /// <summary>
        /// Вести подробный лог (true/false)
        /// </summary>
        public static bool Verbose { get; set; } = false;

        /// <summary>
        /// Задача синхронизации файлов на сервере и локальном диске.
        /// </summary>
        /// <returns>Код возврата для программы.</returns>
        public static int Sync()
        {
            var downloadDirectory = GetValue("DownloadDirectory");
            var historyFile = GetValue("DownloadHistory")
                ?? Path.Combine(downloadDirectory, Path.GetFileName(UpdateHistory));

            int compare = CompareSizeOfFile(UpdateHistory, historyFile);

            if (compare == 0)
            {
                if (Verbose)
                {
                    TWriteLine("Нет обновлений.");
                }

                return 0;
            }

            string[] list;
            int counter = 0;
            var dateFrom = DateTime.Now.AddDays(-DownloadDays).ToString("yyyyMMdd");

            if (compare > 0)
            {
                list = DownloadHistory(UpdateHistory, historyFile);

                if (Verbose)
                {
                    TWriteLine($"Есть обновления ({list.Length}):");
                }
            }
            else // compare < 0
            {
                DownloadFile(UpdateHistory, historyFile);
                list = File.ReadAllLines(historyFile);
                TWriteLine($"Перезагрузка за последние {DownloadDays} дней.");
            }

            foreach (var file in list)
            {
                // "/EQ/20230526/PC01101_EQMLIST_001_260523_025153489.xml.p7s.zip.p7e"
                // Допстраховка от сбоев, когда возникает желание перезагрузить весь список от начала...
                if (file[3] == '/' && string.Compare(file, 4, dateFrom, 0, 8) > 0)
                {
                    if (DownloadFile(file))
                    {
                        counter++;
                    }
                }
            }

            //if (Verbose)
            {
                TWriteLine($"Загружено {counter} из {list.Length}.");
            }

            return 0;
        }

        /// <summary>
        /// Получить бинарное содержимое указанного файла.
        /// </summary>
        /// <param name="serverPath">Путь к файлу на сервере.</param>
        /// <param name="method">Метод получения (FTP, загрузить файл).</param>
        /// <param name="contentOffset">Место смещения в файле (не 0 при докачивании).</param>
        /// <returns>Возвращает ответ FTP-сервера.</returns>
        public static FtpWebResponse GetResponse(string serverPath, string method = WebRequestMethods.Ftp.DownloadFile, long contentOffset = 0)
        {
            var path = serverPath.Replace(@"\", "/");
            var request = (FtpWebRequest)WebRequest.Create(Server + path);
            request.Proxy = null; // Игнорировать настройки прокси (TODO: сделать поддержку прокси).
            request.Credentials = User;
            request.Method = method;
            request.EnableSsl = true;
            request.UseBinary = true;
            request.ContentLength = contentOffset;

            TWriteLine(contentOffset > 0
                ? $"< {method} {serverPath} {contentOffset}"
                : $"< {method} {serverPath}");

            var response = (FtpWebResponse)request.GetResponse();

            if (response.StatusCode != FtpStatusCode.DataAlreadyOpen && response.StatusCode != FtpStatusCode.OpeningData)
            {
                TWrite($"> {response.StatusDescription}"); // StatusDescription tails an extra NewLine
            }

            return response;
        }

        /// <summary>
        /// Скачать файл истории выкладок на сервере.
        /// </summary>
        /// <param name="serverPath">Путь к файлу истории на сервере.</param>
        /// <param name="localPath">Путь к файлу истории на локальном диске.</param>
        /// <returns>Массив строк с новыми файлами по сравнению с локальной копией файла истории.</returns>
        public static string[] DownloadHistory(string serverPath, string localPath)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    FileMode mode = File.Exists(localPath) ? FileMode.Append : FileMode.Create;
                    long resumePosition;

                    using (var writeStream = new FileStream(localPath, mode))
                    {
                        resumePosition = writeStream.Position;

                        using (var responseStream = GetResponse(serverPath, 
                            WebRequestMethods.Ftp.DownloadFile, resumePosition).GetResponseStream())
                        {
                            responseStream.CopyToAsync(memoryStream).Wait();
                        }

                        memoryStream.Position = resumePosition;
                        memoryStream.CopyToAsync(writeStream).Wait();
                    }

                    memoryStream.Position = resumePosition;
                    var lines = new List<string>();

                    using (var reader = new StreamReader(memoryStream)) //, Encoding.ASCII))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }

                    return lines.ToArray();
                }
            }
            catch (WebException e)
            {
                TWriteLine("Download file Error: " + e.Message);
                return new string[] { };
            }
        }

        /// <summary>
        /// Скачать файл с сервера.
        /// </summary>
        /// <param name="serverPath">Путь к файлу на сервере.</param>
        /// <param name="localPath">Путь к файлу на локальном диске.</param>
        /// <param name="resume">Использовать ли докачку.</param>
        /// <returns>Выполнение завершено успешно (true/false).</returns>
        public static bool DownloadFile(string serverPath, string localPath = null, bool resume = false)
        {
            try
            {
                var path = localPath ?? Path.Combine(DownloadDirectory, Path.GetFileName(serverPath));
                FileMode mode = (File.Exists(path) && resume) ? FileMode.Append : FileMode.Create;

                using (var writeStream = new FileStream(path, mode))
                {
                    using (var response = GetResponse(serverPath, WebRequestMethods.Ftp.DownloadFile, writeStream.Position))
                    using (var responseStream = response.GetResponseStream())
                    {
                        responseStream.CopyTo(writeStream);
                    }
                }

                var file = new FileInfo(path);

                if (file.Exists && file.Length == 0)
                {
                    TWriteLine("Файл был скачан нулевой длины и будет удален - проверьте!");
                    file.Delete();
                    return false;
                }

                return true;
            }
            catch (WebException e)
            {
                var x = (FtpWebResponse)e.Response;

                if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    TWriteLine("Файл был удален в связи с истечением срока давности.");
                    return true; // it's normal to skip it
                }

                TWriteLine("Download file Error: " + e.Message);
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
        public static int CompareSizeOfFile(string serverPath, string localPath = null)
        {
            try
            {
                var response = GetResponse(serverPath, WebRequestMethods.Ftp.GetFileSize);
                var serverSize = response.ContentLength;
                response.Close();

                var path = localPath ?? Path.Combine(DownloadDirectory, Path.GetFileName(serverPath));

                if (!File.Exists(path))
                {
                    return -1;
                }

                var localSize = new FileInfo(path).Length;

                return (serverSize == localSize)
                    ? 0
                    : (serverSize > localSize)
                        ? 1
                        : -1;
            }
            catch (WebException e)
            {
                var x = (FtpWebResponse)e.Response;

                if (x == null)
                {
                    TWriteLine("Ответ от сервера не получен.");
                    return 0;
                }

                if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    TWriteLine("Файл был удален в связи с истечением срока давности.");
                    return 0;
                }

                TWriteLine("Size of file Error: " + e.Message);
                return -1;
            }
        }
    }
}
