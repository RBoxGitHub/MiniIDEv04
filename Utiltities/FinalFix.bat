@echo off
set GITREPO=D:\GrokCryptoTrack\Production-Claude\MiniIDEv04
set WORKFOLDER=D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04

echo Copying new SessionStarter to git repo...
copy /Y "%WORKFOLDER%\ProjectNotes\MiniIDEv04_SessionStarter.txt" "%GITREPO%\ProjectNotes\MiniIDEv04_SessionStarter.txt"

echo Copying new .gitignore to git repo...
copy /Y "%~dp0.gitignore" "%GITREPO%\.gitignore"

echo.
cd /d "%GITREPO%"

git add -f ProjectNotes\MiniIDEv04_SessionStarter.txt
git add .gitignore
git status
git commit -m "MiniIDEv04 - Mar 18 2026 - Fix gitignore + SessionStarter Phase 3"
git push origin main

echo.
echo Done!
pause
