@echo off
set GITREPO=D:\GrokCryptoTrack\Production-Claude\MiniIDEv04
set STARTER=D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04\ProjectNotes\MiniIDEv04_SessionStarter.txt

echo Copying SessionStarter to git repo...
copy /Y "%STARTER%" "%GITREPO%\ProjectNotes\MiniIDEv04_SessionStarter.txt"

echo.
echo Running git...
cd /d "%GITREPO%"
git add -f ProjectNotes\MiniIDEv04_SessionStarter.txt
git status
git commit -m "MiniIDEv04 - Mar 18 2026 - Update SessionStarter to Phase 3"
git push origin main

echo.
echo Done!
pause
