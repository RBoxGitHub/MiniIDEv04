@echo off
set GITREPO=D:\GrokCryptoTrack\Production-Claude\MiniIDEv04

cd /d "%GITREPO%"

echo Adding exception to .gitignore...
echo !ProjectNotes/MiniIDEv04_SessionStarter.txt>> .gitignore

echo Force adding SessionStarter...
git add -f "ProjectNotes/MiniIDEv04_SessionStarter.txt"

echo Adding .gitignore...
git add .gitignore

echo Committing...
git commit -m "MiniIDEv04 - Mar 18 2026 - Add SessionStarter exception to gitignore"

echo Pushing...
git push origin main

echo.
echo Done!
pause
