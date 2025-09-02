using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

namespace FTPSReportsDownload
{
    public static class Helper
    {
        public static bool DownloadFile(string ftpServer, string ftpUserName, string ftpUserPassword, string sourceFilePath, string localDestinationPath)
        {
            try
            {
                Log("Download file: " + @"ftp://" + ftpServer + sourceFilePath);
                var request = (FtpWebRequest)WebRequest.Create(@"ftp://" + ftpServer + sourceFilePath.Replace(@"\", "/"));
                request.Proxy = null; // Игнорировать настройки прокси.
                request.Credentials = new NetworkCredential(ftpUserName, ftpUserPassword);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.EnableSsl = true;
                request.UseBinary = true;

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                var localDestFilePath = localDestinationPath + @"\" + sourceFilePath;
                CreatePath(localDestFilePath);
                FileStream writeStream = new FileStream(localDestFilePath, FileMode.Create);

                var length = 4096;
                var buffer = new byte[length];
                int bytesRead = responseStream.Read(buffer, 0, length);
                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = responseStream.Read(buffer, 0, length);
                }

                Log("Status: " + response.StatusDescription);

                writeStream.Close();
                response.Close();

                return true;
            }
            catch (WebException e)
            {
                var x = (FtpWebResponse)e.Response;
                if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    Log("Status: Файл был удален в связи с истечением срока давности 30 дней.");
                    return false;
                }
                Log("Download file -> Error: " + e.Message);
                return false;
            }
        }

        public static string GetConfigValue(string key, bool isRequired = true, string message = null)
        {
            string result = result = ConfigurationManager.AppSettings[key];

            if (result == null)
            {
                Log(message ?? $"Ошибка в конфиг. файле: Параметр {key} не задан в конфиг. файле.");

                if (isRequired)
                {
                    Environment.Exit(-1);
                }
            }

            return result;
        }

        public static void CreatePath(string path)
        {
            var extractedPath = Path.GetDirectoryName(path.Contains('.') ? path : path + "\\");

            if (!Directory.Exists(extractedPath))
            {
                Directory.CreateDirectory(extractedPath);
            }
        }

        public static void Log(string text)
        {
            var message = DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss ") + text;
            var downloadLog = ConfigurationManager.AppSettings["DownloadLog"];

            if (string.IsNullOrEmpty(downloadLog) == false)
            {
                CreatePath(downloadLog);
            }

            try
            {
                if (string.IsNullOrEmpty(text) == false)
                {
                    Console.WriteLine(message);
                    using (var log = File.AppendText(downloadLog))
                    {
                        log.WriteLine(message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка: Не могу записать лог файл.");
                Console.WriteLine("Подсказка: Проверьте настройку параметра DownloadLog в конфиг. файле.");
                Console.WriteLine("Детали ошибки: " + e.Message + "\n");
            }
        }
    }
}