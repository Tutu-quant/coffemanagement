@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Khong tim thay .NET SDK. Vui long cai .NET 10 SDK.
    pause
    exit /b 1
)

set "PROJECT_FILE="
for %%F in (*.csproj) do set "PROJECT_FILE=%%F"
if not defined PROJECT_FILE (
    echo [ERROR] Khong tim thay file .csproj trong %CD%.
    pause
    exit /b 1
)

echo [1/3] Restoring packages...
dotnet restore "%PROJECT_FILE%"
if errorlevel 1 goto :failed

echo [2/3] Building project...
dotnet build "%PROJECT_FILE%" --no-restore
if errorlevel 1 goto :failed

echo [3/3] Starting BrewPoint...
echo Open the URL shown below in your browser. Press Ctrl+C to stop.
dotnet run --project "%PROJECT_FILE%" --no-build
exit /b %errorlevel%

:failed
echo.
echo [ERROR] Khong the khoi chay du an. Xem thong bao loi o tren.
pause
exit /b 1
