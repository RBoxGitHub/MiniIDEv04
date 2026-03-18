# Claude Session Rules — RBoxGitHub
*Fetch this file at the start of every session before doing anything else.*
*URL: https://raw.githubusercontent.com/RBoxGitHub/ClaudeRules/main/ClaudeRules.md*

---

## Session Startup Protocol

1. Fetch and read this file first
2. Ask: **"Which GitHub project are we working on today?"**
3. Fetch the `ProjectNotes` folder from the selected repo to load project context
4. Confirm the current phase and what we are building next before writing any code

---

## File Delivery Rules

1. **Zip files** — only create a zip when the drop list has more than 4 files
2. **Project-wide zip** — always prompt before creating a full solution zip
3. **Individual files** — always deliver files individually when 4 or fewer
4. **Always include `_manifest.json`** in every zip for the DropZone deployer
5. **Always include a drop checklist** `.txt` with every file delivery session
6. **Always update `MiniIDEv04_SessionStarter.txt`** in `ProjectNotes\` when project state changes significantly

---

## Build Rules

1. **Always ask before building anything** — confirm the plan first, wait for approval
2. **One task at a time** — do not chain multiple features without checking in
3. **Always present a drop list** before delivering files
4. **Never create a project-wide zip without explicit prompt and approval**

---

## Git / GitHub Rules

1. Always include `_manifest.json` in every zip
2. Repo path convention: `https://github.com/RBoxGitHub/{ProjectName}`
3. Default commit message format: `{ProjectName} — {MMM dd yyyy HH:mm:ss}`
4. Always suggest a GitHub push at the end of a productive session
5. **miniIDE.db is pushed to GitHub with every commit** — full backup of all notes, panels, git log, and drop log

---

## Data Access Rules

1. **Never call `SQLiteConnection` or `SQLiteAsyncConnection` directly from UI code-behind** — always use a repository class
2. All data access goes through repository classes (e.g. `SqliteThingsToDoRepository`) — interfaces are optional
3. **Never access GitHub data directly from the app** — GitHub is backup and version control only
4. The database is the single source of truth for all runtime state

---

## Code Style Rules

1. Use `CommunityToolkit.Mvvm` source generators — `[ObservableProperty]`, `[RelayCommand]`
2. All panels must implement `IDraggablePanel` interface
3. All new panel types must be registered in `PanelControlFactory`
4. All new DB tables must be added to `ProjectDatabase.Initialize()`
5. Namespace: always match project name (e.g. `MiniIDEv04`)

---

## Active Projects

| Project | Repo | Current Phase |
|---------|------|---------------|
| MiniIDEv04 | https://github.com/RBoxGitHub/MiniIDEv04 | Phase 2 |

---

*Last updated: March 17, 2026*
