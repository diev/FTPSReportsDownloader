using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace FTPSReportsDownload
{
    public static class Tests
    {
        public static void RunAllTests()
        {
            var t1 = Test1();
            var t2 = Test2();
            var t3 = Test3();
        }

        static bool Test1()
        {
            Helper.Log("Тест 1: Проверка сетевого доступа для установки канала передачи данных ftps.");

            var server = Helper.GetConfigValue("Server");
            var request = (HttpWebRequest)WebRequest.Create($"http://{server.Split(':')[0]}:65000");
            request.Proxy = null; // Игнорировать настройки прокси.

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Helper.Log("Тест 1. Пройден.");
                    return true;
                }
            }
            catch (Exception e)
            {
                Helper.Log("Тест 1. Подсказка: Необходимо разрешить (открыть) порты с 49152 по 65534 для хоста " + request.Host.Split(':')[0] + " для передачи данных по протоколу ftps.");
                Helper.Log("Тест 1: НЕ пройден.");
                Helper.Log("Тест 1: Детали ошибки: " + e.Message + "\n");
            }
            return false;
        }

        static bool Test2()
        {
            Helper.Log("Тест 2: Проверка сетевого доступа для отправки ftps команд.");

            using (TcpClient tcpClient = new TcpClient())
            {

                var server = Helper.GetConfigValue("Server");
                var serverPort = server.Split(':');
                var host = serverPort[0];
                var port = 21;

                if (server.Contains(':'))
                {
                    port = int.Parse(serverPort[1]);
                }

                try
                {
                    tcpClient.Connect(host, port);
                    Helper.Log("Тест 2: Пройден.");
                    return true;
                }
                catch (Exception e)
                {
                    Helper.Log($"Тест 2. Подсказка: Необходимо разрешить (открыть) доступ на {port} порт для хоста {host} для передачи команд по протоколу ftps.");
                    Helper.Log("Тест 2: НЕ пройден.");
                    Helper.Log("Тест 2: Детали ошибки: " + e.Message + "\n");
                    return false;
                }
            }
        }

        static bool Test3()
        {
            Helper.Log("Тест 3: Проверка подключения к ftps серверу с именем пользователя и паролем.");

            var server = Helper.GetConfigValue("Server");
            var request = (FtpWebRequest)WebRequest.Create("ftp://" + server);
            request.Proxy = null; // Игнорировать настройки прокси.
            try
            {
                var ftpUserName = Helper.GetConfigValue("UserName");
                var ftpUserPassword = Helper.GetConfigValue("Password");

                request.Credentials = new NetworkCredential(ftpUserName, ftpUserPassword);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.EnableSsl = true;
                request.UseBinary = true;

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                var response = (FtpWebResponse)request.GetResponse();
                Helper.Log("Тест 3: Пройден.");
                return true;
            }
            catch (Exception e)
            {
                Helper.Log("Тест 3. Подсказка: Проверьте правильность имени пользователя и пароля для доступа к ftps серверу в конфиг. файле и повторите попытку.");
                Helper.Log("Тест 3: НЕ пройден.");
                Helper.Log("Тест 3: Детали ошибки: " + e.Message + "\n");
                return false;
            }
        }
    }
}