@echo off
setlocal
echo 正在创建 DigYourWindows 发布包...

REM 先编译 Release 版本
echo.
echo [1/3] 正在编译 Release 版本 (cargo build --release)...
cargo build --release
if errorlevel 1 (
    echo 编译失败，打包中止。
    pause
    exit /b 1
)

REM 获取当前日期（用于包名）
echo.
echo [2/3] 正在生成发布目录...
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /format:list') do set datetime=%%I
set "YYYY=%datetime:~0,4%"
set "MM=%datetime:~4,2%"
set "DD=%datetime:~6,2%"

set "PACKAGEDIR=DigYourWindows_v0.1.0_%YYYY%%MM%%DD%"
if exist "%PACKAGEDIR%" rmdir /s /q "%PACKAGEDIR%"
mkdir "%PACKAGEDIR%"

REM 复制必要文件（从 target\release 拷贝最新 exe，模板从 src 拷贝）
copy /Y "target\release\DigYourWindows_Rust.exe" "%PACKAGEDIR%\" >nul
copy /Y "release\run.bat" "%PACKAGEDIR%\" >nul
copy /Y "release\README.md" "%PACKAGEDIR%\" >nul
copy /Y "src\template.html" "%PACKAGEDIR%\" >nul
copy /Y "src\template_new.html" "%PACKAGEDIR%\" >nul
copy /Y "src\template_simple.html" "%PACKAGEDIR%\" >nul

REM 压缩为 ZIP
echo.
echo [3/3] 正在压缩为 ZIP...
powershell -command "Compress-Archive -Path '%PACKAGEDIR%' -DestinationPath '%PACKAGEDIR%.zip' -Force"

REM 清理临时目录
rmdir /s /q "%PACKAGEDIR%"

echo.
echo 发布包已创建: %PACKAGEDIR%.zip
pause
endlocal