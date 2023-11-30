# FTPSReportsDownloader

Downloads all files from a remote FTPS server to a local path.

Reference code v1.0 (c) MOEX 2016 updated to v2.0 2023

## v2.0

- Update project to .Net Framework 4.8
- Refactor sync alghoritm completely (use new lines in `/UpdateHistory.txt` or download `days` before files)
- Change `<add key="DownloadLog" value="logs\{0:yyyy-MM}\{0:yyyy-MM-dd}.log"/>` to write dated logs if specified

- Add checking SIZE of `/UpdateHistory.txt` before new download
- Add resume download of `/UpdateHistory.txt`
- Add `<add key="DownloadHistory" value="ftp\UpdateHistory.txt"/>` (optional, default in `DownloadDirectory`)
- Add `<add key="DownloadDays" value="14"/>` (optional, default up to 14 days before)

- Remove use of `lastSync.file` - it is simple to delete few last lines from local `UpdateHistory.txt` instead

## Requirements

- .Net Framework 4.8

## Breaking Notes

.NET 6+ does not contain FTP functionality anymore. It has been suggested to use other libraries.

## License

Licensed under the [Apache License, Version 2.0].

[Apache License, Version 2.0]: LICENSE
