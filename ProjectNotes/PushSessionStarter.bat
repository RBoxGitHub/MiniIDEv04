@echo off
cd /d "D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04"

echo.
echo ══════════════════════════════════════════════════
echo  MiniIDEv04 — Push SessionStarter to GitHub
echo ══════════════════════════════════════════════════
echo.

git add ProjectNotes/MiniIDEv04_SessionStarter.txt
git commit -m "MiniIDEv04 — Mar 18 2026 — Update SessionStarter to Phase 3"
git push origin main

echo.
echo ══════════════════════════════════════════════════
echo  Done!
echo ══════════════════════════════════════════════════
echo.
pause
