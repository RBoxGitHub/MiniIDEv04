@echo off
echo Searching for .git folder...
echo.

for /d %%G in (
    "D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder\MiniIDEv04"
    "D:\GrokCryptoTrack\Production-Claude\MiniIDE-WorkFolder"
    "D:\GrokCryptoTrack\Production-Claude"
    "D:\GrokCryptoTrack"
) do (
    if exist "%%G\.git" (
        echo Found .git in: %%G
        cd /d "%%G"
        echo.
        echo Running git status...
        git status
        echo.
        echo Adding SessionStarter...
        git add -f "MiniIDEv04/ProjectNotes/MiniIDEv04_SessionStarter.txt"
        git add -f "ProjectNotes/MiniIDEv04_SessionStarter.txt"
        git commit -m "MiniIDEv04 — Mar 18 2026 — Update SessionStarter to Phase 3"
        git push origin main
        echo.
        echo Done!
        goto :end
    )
)

echo Could not find .git folder in any expected location.

:end
pause
