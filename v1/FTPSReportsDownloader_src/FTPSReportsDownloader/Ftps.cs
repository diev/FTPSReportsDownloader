using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FTPSReportsDownload
{
    public static class Ftps
    {
        public static void Sync()
        {
            var server = Helper.GetConfigValue("Server");
            var userName = Helper.GetConfigValue("UserName");
            var password = Helper.GetConfigValue("Password");
            var downloadDirectory = Helper.GetConfigValue("DownloadDirectory");

            Helper.DownloadFile(server, userName, password, "/UpdateHistory.txt", downloadDirectory);

            var updateHistory = File.ReadAllLines(downloadDirectory + @"\UpdateHistory.txt");
            var syncFlag = false;
            var lastSyncFile = "";
            try
            {
                lastSyncFile = File.ReadAllText(downloadDirectory + @"\lastSync.file");
            }
            catch
            {
                Helper.Log($@"Ошибка: Файл {downloadDirectory}\lastSync.file не был найден.");
                Helper.Log("Подсказка: lastSync.file - файл создается после успешного прохождения процесса синхронизации.");
            }

            var dateFrom = DateTime.Now.AddDays(-30); // Скачать все файлы за последние 30 дней с ftps, если файл lastSync.file не был найден
            var startSyncFromList = new Dictionary<string, string>();
            startSyncFromList.Add(dateFrom.ToString("/yyyyMM") + dateFrom.ToString("dd"), "файлов за последние 30 дней.");
            startSyncFromList.Add(dateFrom.ToString("/yyyyMM") + dateFrom.ToString("dd")[0], "файлов за последние 3* дней.");
            startSyncFromList.Add(dateFrom.ToString("/yyyyMM"), "файлов с предыдущего месяца.");
            startSyncFromList.Add(dateFrom.ToString("/yyyy"), "файлов за год.");
            startSyncFromList.Add("SyncAll", "всех файлов.");

            foreach (var startSyncFrom in startSyncFromList)
            {
                if (string.IsNullOrEmpty(lastSyncFile))
                {
                    Helper.Log("Начало процесса синхронизации " + startSyncFrom.Value);
                }

                if (startSyncFrom.Key == "SyncAll") syncFlag = true;

                foreach (var file in updateHistory)
                {
                    if (string.IsNullOrEmpty(lastSyncFile) && file.Contains(startSyncFrom.Key))
                    {
                        syncFlag = true;
                    }

                    if (syncFlag)
                    {
                        Helper.DownloadFile(server, userName, password, file, downloadDirectory);
                    }

                    if (syncFlag == false && (string.IsNullOrEmpty(lastSyncFile) == false && file == lastSyncFile))
                    {
                        syncFlag = true;
                    }
                }

                if (syncFlag) break;
            }

            try
            {
                File.WriteAllText(downloadDirectory + @"\lastSync.file", updateHistory.Last());
            }
            catch (Exception e)
            {
                Helper.Log($@"Ошибка: Файл {downloadDirectory}\lastSync.file НЕ был сохранен.");
                Helper.Log("Детали ошибки: " + e.Message + "\n");
            }
        }
    }
}
