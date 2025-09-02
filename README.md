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

### Settings / Параметры

*Appsettings* с именем исполняемой программы - в отличие от принятого при
разработке в .NET единого имени, так можно все файлы настроек разных
программ размещать в одной папке для скриптов, но при этом какие-то общие
параметры (будут иметь приоритет) вынести из папки с программой, где они
могут быть нечаянно затерты при обновлении версии или конфиденциальная
информация может попасть в дистрибутивный архив:

- `FTPSReportsDownloader.config.json` (located with App `exe`)
- `%ProgramData%\Diev\FTPSReportsDownloader.config.json` (these settings
overwrite if exist)

Однако, при запуске в Linux программа может не получать путь к папке
с программой - тогда она будет искать традиционный `appsettings.json`
в текущей папке.

*Windows Credential Manager* в Панели управления - *Диспетчер учетных данных*
(все пароли для всех программ меняются в одном месте и скрыты от
пользователей, как было бы при хранении в индивидуальных файлах настроек
к каждой программе). Программа использует поле `"TargetName"` из `json`
для указания, где искать `username` и `password`:

- `"host"` (name: `{host}`, user: `{username}`, pass: `{password}`)

Однако в Linux такой программы нет, и все параметры надо написать в JSON
открытым текстом (в Windows тоже так можно, игнорируя безопасность, или
же можно вынести в отдаленную папку в `%ProgramData%`):

- `"TargetName": "{host} {username} {password}"` (три параметра)

### Build / Построение

Build an app with many dlls.
В папке дистрибутива будет `exe` и очень много сопутствующих отдельных
`dll` - вариант разработки в .NET по умолчанию:

    dotnet publish \FTPSReportsDownloader.csproj -o Distr

Build a single-file app when NET Desktop runtime required.
Будет один исполняемый файл `exe`, но требуется установка .NET -
предпочитаемый мною вариант:

    dotnet publish FTPSReportsDownloader\FTPSReportsDownloader.csproj -o Distr -r win-x64 -p:PublishSingleFile=true --no-self-contained

Build a single-file app when no runtime required.
Вариант, когда .NET устанавливать не нужно - всё есть в одном большом файле,
который можно перенести в закрытую систему, где ничего устанавливать нельзя:

    dotnet publish FTPSReportsDownloader\FTPSReportsDownloader.csproj -o Distr -r win-x64 -p:PublishSingleFile=true

Build an app on Windows and transfer binary files to Linux.
Вариант, когда папку с файлами, собранную на Windows, можно просто перенести
на Linux и там запустить, ничего там не устанавливая:

    dotnet publish FTPSReportsDownloader\FTPSReportsDownloader.csproj -o Distr -r linux-x64 --self-contained

Для использования одного/нескольких из этих вариантов можно применять
`build.cmd` - он создаст нужный дистрибутив(ы) с архивом исходных файлов
текущей версии.

Исходный код [здесь](v9).

### Breaking Notes

.NET still supports `(FtpWebRequest)WebRequest.Create()`
because just `WebRequest.Create()` is marked as obsoleted only!

### Requirements / Требования

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

### Обновленная логика программы

1. Сверить размер файла `/UpdateHistory.txt` на сервере и локально.
2. Если есть разница в большую сторону - докачать разницу.
3. По списку в этой разнице скачать файлы с отчетами.
4. Если разница в меньшую сторону или файла нет - скачать его целиком
и по нему скачать файлы с отчетами за указанное число последних дней.

- Поэтому в случае любых ошибок с отчетами - надо удалить некоторое
число последних строк из файла истории - программа их перекачает.
- Если поврежден файл истории - удалить его.

Исходный код [здесь](v2).

### Breaking Notes

.NET 6+ does not contain FTP functionality anymore.
It has been suggested to use other libraries.

### Requirements / Требования

- .Net Framework 4.7.2-4.8

## v1.0 (c) MOEX 2016

Из Руководства пользователя "Личного кабинета участника":

> Раздел «Отчеты» содержит ссылки на торговые и клиринговые отчеты
в подписанном и/или зашифрованном виде. Скачивание отчета возможно
осуществить в течение 14 календарных дней с момента размещения
ссылки. В правом верхнем углу расположен пример ~~Инструмента для
скачивания отчетов "FTPSReportsDownloader"~~ с FTPS (исходный код
программы по скачиванию отчетов возможно дописать с учетом
собственных требований).

Ссылка на пример (лицензия MIT) у них устарела.
Исходный код [здесь](v1).

### Requirements / Требования

- .Net Framework 4.5

## Versioning / Порядок версий

Номер версии программы указывается по нарастающему принципу:

* Протестированная максимальная версия .NET (9);
* Год текущей разработки (2025);
* Месяц без первого нуля и день редакции (902 - 02.09.2025);
* Номер билда - просто нарастающее число для внутренних отличий.
Если настроен сервис AppVeyor, то это его автоинкремент.
Или часто просто 0.

Продукт развивается для собственных нужд, а не по коробочной
стратегии, и поэтому *Breaking Changes* могут случаться чаще,
чем это принято в *SemVer*.

## License / Лицензия

Licensed under the [Apache License, Version 2.0](LICENSE).  
Вы можете использовать эти материалы под свою ответственность.

[![Telegram](https://img.shields.io/badge/t.me-dievdo-blue?logo=telegram)](https://t.me/dievdo)
