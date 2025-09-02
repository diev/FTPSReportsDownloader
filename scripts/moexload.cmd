@echo off
rem 2025-08-28

if not exist pki1.conf (
  echo pki1.conf not found in the current directory
  exit /b 1
)


rem Входной буфер для очистки
set in=C:\FORMS\MOEX\IN
rem Выходное хранилище чистых файлов для АБС
set out=C:\FORMS\MOEX\LOAD

echo Start FtpsClient
FTPSReportsDownloader.exe
echo Stop FtpsClient

rem 866!
set profile=Profile2024

set dd=%date:~-10,2%
set mm=%date:~-7,2%
set yyyy=%date:~-4%

set lymd=%yyyy%%mm%%dd%

rem logs\2021\202106\20210608_this.log
rem set log=%home%\logs\%lymd:~0,4%\%lymd:~0,6%
set log=logs\%yyyy%-%mm%
if not exist %log%\nul md %log% 2>nul
set log=%log%\%yyyy%-%mm%-%dd%_%~n0.log

set temp=logs\temp
if not exist %temp%\nul md %temp% 2>nul

rem --------------------------------------------------
rem BugFix to reload if certificates fail
for %%i in (%out%\*.sha1) do del "%%~i"
for %%i in (%out%\*.p7?) do move/y "%%~i" %in%\

for %%i in (%in%\*.*) do call :in1 "%%~i"
rem --------------------------------------------------
exit /b 0
goto :eof

:in1
copy /y "%~1" %temp%\
echo %date% %time:~0,8% %username% %~nx1>>%log%

for %%f in (%temp%\*.p7a) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 "C:\Program Files\Validata\zpki\zpki1utl.exe" -profile %profile% -decrypt -in "%%~f" -out "%temp%\%%~nf.p7s"
 if exist "%temp%\%%~nf.p7s" del "%%~f"
)

for %%f in (%temp%\*.p7e) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 "C:\Program Files\Validata\zpki\zpki1utl.exe" -profile %profile% -decrypt -in "%%~f" -out "%temp%\%%~nf"
 if exist "%temp%\%%~nf" del "%%~f"
)

for %%f in (%temp%\*.zip) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 "C:\Program Files\7-Zip\7z.exe" e -y "%%~f" -o%temp%\
 if errorlevel 0 del "%%~f"
)

for %%f in (%temp%\*.p7s) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 "C:\Program Files\Validata\zpki\zpki1utl.exe" -profile %profile% -verify -in "%%~f" -out "%temp%\%%~nf" -delete 1
 if exist "%temp%\%%~nf" del "%%~f"
)

for %%f in (%temp%\*.p7d) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 if exist "%temp%\%%~nf" del "%%~f"
)

for %%f in (%temp%\*.sha1) do (
 del "%%~f"
)

rem move /y %temp%\*.* %out%\
for %%f in (%temp%\*.*) do call :dated %%~nf %%f

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

:dated
rem MM00001_CCX18_A00_051224_017321947.xml
for /F "tokens=4 delims=_" %%d in ("%1") do call :moved %%d %2
goto :eof

:moved
set d=%1
set d2=%d:~0,2%
set m2=%d:~2,2%
set y4=20%d:~-2%
set outd=%out%\%y4%\%y4%-%m2%-%d2%
md %outd%
move /y %2 %outd%\
goto :eof
