@echo off
rem 866!
rem 2023-10-20

set home=%~dp0
rem Входной буфер для очистки
set in=%home%..\IN
rem Выходное хранилище чистых файлов для АБС
set out=%home%..\LOAD
rem Справочник сертификатов
set spr=%home%..\SPR

%home%FTPSReportsDownloader.exe

set profile=Profile2023

set dd=%date:~-10,2%
set mm=%date:~-7,2%
set yyyy=%date:~-4%

set lymd=%yyyy%%mm%%dd%

set log=%home%logs\%yyyy%-%mm%
if not exist %log%\nul md %log% 2>nul
set log=%log%\%yyyy%-%mm%-%dd%_%~n0.log

if not exist temp\nul md temp 2>nul

rem --------------------------------------------------
rem BugFix to reload if certificates fail
for %%i in (%out%\*.sha1) do del "%%~i"
for %%i in (%out%\*.p7?) do move/y "%%~i" %in%\

for %%i in (%in%\*.*) do call :in1 "%%~i"
rem --------------------------------------------------
goto :eof

:in1
copy /y "%~1" temp\
echo %date% %time:~0,8% %username% %~nx1>>%log%

for %%f in (temp\*.p7a) do (
 zpki1utl.exe -profile %profile% -decrypt -in "%%~f" -out "temp\%%~nf.p7s"
 if exist "temp\%%~nf.p7s" del "%%~f"
)

for %%f in (temp\*.p7e) do (
 zpki1utl.exe -profile %profile% -decrypt -in "%%~f" -out "temp\%%~nf"
 if exist "temp\%%~nf" del "%%~f"
)

for %%f in (temp\*.zip) do (
 7z.exe e -y "%%~f" -otemp\
 if errorlevel 0 del "%%~f"
)

for %%f in (temp\*.p7s) do (
 zpki1utl.exe -profile %profile% -verify -in "%%~f" -out "temp\%%~nf" -delete 1
 if exist "temp\%%~nf" del "%%~f"
)

for %%f in (temp\*.sha1) do (
 del "%%~f"
)

move /y temp\*.* %out%\
move /y "%~1" %in%\BAK\
goto :eof

:stamp
if exist %1 goto :stamp_ok
set tymd=0000-00-00-0000
echo %tymd% %~f1
goto :eof

:stamp_ok
set d=%~t1
rem d=02.09.2020 14:02

set tymd=%d:~6,4%-%d:~3,2%-%d:~0,2%-%d:~11,2%%d:~14,2%
rem tymd=2020-09-02-1402

echo %tymd% %~f1
goto :eof
