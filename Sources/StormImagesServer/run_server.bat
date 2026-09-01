@echo off
chcp 65001 >nul
title STORM IMAGES - AI Backend Server
echo ============================================================
echo   STORM IMAGES 0.0.1 - AI BACKEND SERVER (Qwen-Image-Edit)
echo ============================================================
echo.
cd /d "%~dp0"
echo [*] Checking Python environment and launching FastAPI server...
python app.py
if %errorLevel% neq 0 (
    echo [!] Direct python failed, attempting uv or py...
    py -3 app.py
)
pause