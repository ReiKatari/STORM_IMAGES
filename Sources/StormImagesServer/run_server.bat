@echo off
chcp 65001 >nul
title STORM IMAGES 0.0.3 - AI Backend Server
echo ============================================================
echo   STORM IMAGES 0.0.3 - AI BACKEND SERVER (Qwen-Image-Edit)
echo ============================================================
echo.

cd /d "%~dp0"

:: Kill any stale process listening on port 7860 to prevent WinError 10048
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :7860 ^| findstr LISTENING') do (
    echo [*] Releasing port 7860 (terminating stale process PID %%a)...
    taskkill /F /PID %%a >nul 2>nul
)

:: Check for uv package manager in PATH or default locations
set "UV_EXE="
if exist "%USERPROFILE%\.local\bin\uv.exe" set "UV_EXE=%USERPROFILE%\.local\bin\uv.exe"
if exist "%LOCALAPPDATA%\Programs\uv\uv.exe" set "UV_EXE=%LOCALAPPDATA%\Programs\uv\uv.exe"
if "%UV_EXE%"=="" where uv.exe >nul 2>nul && set "UV_EXE=uv"

if not "%UV_EXE%"=="" (
    echo [*] Launching AI Server via uv environment...
    "%UV_EXE%" run --with fastapi,uvicorn,httpx,pillow,pydantic,diffusers,torch app.py
    if %errorLevel% equ 0 goto :done
)

:: Fallback to python / py
echo [*] Checking python executable...
where python.exe >nul 2>nul
if %errorLevel% equ 0 (
    python app.py
    if %errorLevel% equ 0 goto :done
)

where py.exe >nul 2>nul
if %errorLevel% equ 0 (
    py -3 app.py
    if %errorLevel% equ 0 goto :done
)

echo [!] ERROR: Neither uv nor python could start app.py!
echo [!] Please make sure Python 3.10+ or uv is available.

:done
pause