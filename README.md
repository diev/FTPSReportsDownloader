# FTPSReportsDownloader

[![Build status](https://ci.appveyor.com/api/projects/status/i44do7h2vhwsxm6f?svg=true)](https://ci.appveyor.com/project/diev/ftpsreportsdownloader)
[![.NET8 Desktop](https://github.com/diev/FTPSReportsDownloader/actions/workflows/dotnet8-desktop.yml/badge.svg)](https://github.com/diev/FTPSReportsDownloader/actions/workflows/dotnet8-desktop.yml)
[![GitHub Release](https://img.shields.io/github/release/diev/FTPSReportsDownloader.svg)](https://github.com/diev/FTPSReportsDownloader/releases/latest)

Downloads all files from a remote FTPS server to a local path.

Reference code v1 (c) MOEX 2016 updated by me to v2.2023, to v9.2025.

## v9.2025

- Update project to .NET 8-9 for Windows and Linux.
- Refactor config to `json`.
- Refactor to use `Diev.Extensions` (//TODO Nuget).
- Secure the network credential with *Windows Credential Manager*.

### Settings / ���������

*Appsettings* � ������ ����������� ��������� - � ������� �� ��������� ���
���������� � .NET ������� �����, ��� ����� ��� ����� �������� ������
�������� ��������� � ����� ����� ��� ��������, �� ��� ���� �����-�� �����
��������� (����� ����� ���������) ������� �� ����� � ����������, ��� ���
����� ���� �������� ������� ��� ���������� ������ ��� ����������������
���������� ����� ������� � �������������� �����:

- `FTPSReportsDownloader.config.json` (located with App `exe`)
- `%ProgramData%\Diev\FTPSReportsDownloader.config.json` (these settings
overwrite if exist)

������, ��� ������� � Linux ��������� ����� �� �������� ���� � �����
� ���������� - ����� ��� ����� ������ ������������ `appsettings.json`
� ������� �����.

*Windows Credential Manager* � ������ ���������� - *��������� ������� ������*
(��� ������ ��� ���� �������� �������� � ����� ����� � ������ ��
�������������, ��� ���� �� ��� �������� � �������������� ������ ��������
� ������ ���������). ��������� ���������� ���� `"TargetName"` �� `json`
��� ��������, ��� ������ `username` � `password`:

- `"host"` (name: `{host}`, user: `{username}`, pass: `{password}`)

������ � Linux ����� ��������� ���, � ��� ��������� ���� �������� � JSON
�������� ������� (� Windows ���� ��� �����, ��������� ������������, ���
�� ����� ������� � ���������� ����� � `%ProgramData%`):

- `"TargetName": "{host} {username} {password}"` (��� ���������)

### Build / ����������

Build an app with many dlls.
� ����� ������������ ����� `exe` � ����� ����� ������������� ���������
`dll` - ������� ���������� � .NET �� ���������:

    dotnet publish \FTPSReportsDownloader.csproj -o Distr

Build a single-file app when NET Desktop runtime required.
����� ���� ����������� ���� `exe`, �� ��������� ��������� .NET -
�������������� ���� �������:

    dotnet publish FTPSReportsDownloader\FTPSReportsDownloader.csproj -o Distr -r win-x64 -p:PublishSingleFile=true --no-self-contained

Build a single-file app when no runtime required.
�������, ����� .NET ������������� �� ����� - �� ���� � ����� ������� �����,
������� ����� ��������� � �������� �������, ��� ������ ������������� ������:

    dotnet publish FTPSReportsDownloader\FTPSReportsDownloader.csproj -o Distr -r win-x64 -p:PublishSingleFile=true

Build an app on Windows and transfer binary files to Linux.
�������, ����� ����� � �������, ��������� �� Windows, ����� ������ ���������
�� Linux � ��� ���������, ������ ��� �� ������������:

    dotnet publish FTPSReportsDownloader\FTPSReportsDownloader.csproj -o Distr -r linux-x64 --self-contained

��� ������������� ������/���������� �� ���� ��������� ����� ���������
`build.cmd` - �� ������� ������ �����������(�) � ������� �������� ������
������� ������.

�������� ��� [�����](v9).

### Breaking Notes

.NET still supports `(FtpWebRequest)WebRequest.Create()`
because just `WebRequest.Create()` is marked as obsoleted only!

### Requirements / ����������

- .NET 8-9 Runtime

## v2.2023

- Update project to .Net Framework 4.8.
- Refactor sync alghoritm completely (use new lines in `/UpdateHistory.txt`
or download `days` before files).
- Change `<add key="DownloadLog" value="logs\{0:yyyy-MM}\{0:yyyy-MM-dd}.log"/>`
to write dated logs if specified.

- Add checking SIZE of `/UpdateHistory.txt` before new download.
- Add resume download of `/UpdateHistory.txt`.
- Add `<add key="DownloadHistory" value="ftp\UpdateHistory.txt"/>`
(optional, default in `DownloadDirectory`).
- Add `<add key="DownloadDays" value="14"/>` (optional, default up to 14 days
before).

- Remove use of `lastSync.file` - it is simple to delete few last lines
from local `UpdateHistory.txt` instead.

### ����������� ������ ���������

1. ������� ������ ����� `/UpdateHistory.txt` �� ������� � ��������.
2. ���� ���� ������� � ������� ������� - �������� �������.
3. �� ������ � ���� ������� ������� ����� � ��������.
4. ���� ������� � ������� ������� ��� ����� ��� - ������� ��� �������
� �� ���� ������� ����� � �������� �� ��������� ����� ��������� ����.

- ������� � ������ ����� ������ � �������� - ���� ������� ���������
����� ��������� ����� �� ����� ������� - ��������� �� ����������.
- ���� ��������� ���� ������� - ������� ���.

�������� ��� [�����](v2).

### Breaking Notes

.NET 6+ does not contain FTP functionality anymore.
It has been suggested to use other libraries.

### Requirements / ����������

- .Net Framework 4.7.2-4.8

## v1.0 (c) MOEX 2016

�� ����������� ������������ "������� �������� ���������":

> ������ �������� �������� ������ �� �������� � ����������� ������
� ����������� �/��� ������������� ����. ���������� ������ ��������
����������� � ������� 14 ����������� ���� � ������� ����������
������. � ������ ������� ���� ���������� ������ ~~����������� ���
���������� ������� "FTPSReportsDownloader"~~ � FTPS (�������� ���
��������� �� ���������� ������� �������� �������� � ������
����������� ����������).

������ �� ������ (�������� MIT) � ��� ��������.
�������� ��� [�����](v1).

### Requirements / ����������

- .Net Framework 4.5

## Versioning / ������� ������

����� ������ ��������� ����������� �� ������������ ��������:

* ���������������� ������������ ������ .NET (9);
* ��� ������� ���������� (2025);
* ����� ��� ������� ���� � ���� �������� (902 - 02.09.2025);
* ����� ����� - ������ ����������� ����� ��� ���������� �������.
���� �������� ������ AppVeyor, �� ��� ��� �������������.
��� ����� ������ 0.

������� ����������� ��� ����������� ����, � �� �� ����������
���������, � ������� *Breaking Changes* ����� ��������� ����,
��� ��� ������� � *SemVer*.

## License / ��������

Licensed under the [Apache License, Version 2.0](LICENSE).  
�� ������ ������������ ��� ��������� ��� ���� ���������������.

[![Telegram](https://img.shields.io/badge/t.me-dievdo-blue?logo=telegram)](https://t.me/dievdo)
