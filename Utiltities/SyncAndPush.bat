@echo off
chcp 65001 >nul
echo.
echo ══════════════════════════════════════════════════════════
echo  MiniIDEv04 — Sync WorkFolder to Git Repo + Push
echo ══════════════════════════════════════════════════════════
echo.
echo Source:  D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04
echo Target:  D:\GrokCryptoTrack\Production-Claude\MiniIDEv04
echo.

set SOURCE=D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04
set TARGET=D:\GrokCryptoTrack\Production-Claude\MiniIDEv04

if not exist "%SOURCE%" (
    echo ERROR: Source folder not found: %SOURCE%
    pause
    exit /b 1
)

if not exist "%TARGET%" (
    echo ERROR: Target folder not found: %TARGET%
    pause
    exit /b 1
)

echo Copying files...
echo.

xcopy "%SOURCE%\*" "%TARGET%\" /E /Y /I /EXCLUDE:%~dp0sync_exclude.txt

echo.
echo Copy complete. Running git...
echo.

cd /d "%TARGET%"

git add .
git commit -m "MiniIDEv04 — Mar 18 2026 — Sync Phase 2 complete codebase to repo"
git push origin main

echo.
echo ══════════════════════════════════════════════════════════
echo  Done!
echo ══════════════════════════════════════════════════════════
echo.
pause
