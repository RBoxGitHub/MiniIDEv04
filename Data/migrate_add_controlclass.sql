-- MiniIDEv04 Migration: Add ControlClass column to sys_Panels
-- Run against existing miniIDE.db if upgrading from a pre-spawn build
-- Safe to run multiple times — ALTER TABLE fails silently if column exists

ALTER TABLE sys_Panels ADD COLUMN ControlClass TEXT DEFAULT '';

UPDATE sys_Panels SET ControlClass = 'QuickAddPanelControl'      WHERE PanelKey = 'QuickAddPanel';
UPDATE sys_Panels SET ControlClass = 'SysManagerLauncherControl' WHERE PanelKey = 'SysManagerLauncher';
UPDATE sys_Panels SET ControlClass = 'SysManagerPanelControl'    WHERE PanelKey = 'SysManagerPanel';

-- GitHubPushPanel — insert if not already present
INSERT OR IGNORE INTO sys_Panels
    (PanelKey, PanelName, Description, IsVisible, IsPinned, IsCloned,
     PosLeft, PosTop, PanelWidth, PanelHeight, TitleBarColor,
     LaunchTarget, ControlClass, HasSaveButton, Version, SortOrder,
     CreatedAt, UpdatedAt)
VALUES
    ('GitHubPushPanel', '🐙 GitHub Push',
     'Commit and push current build to GitHub',
     1, 0, 0,
     660, 0, 480, 200, '#FF1B5E20',
     '', 'GitHubPushPanelControl', 0, '4.0.0', 3,
     datetime('now'), datetime('now'));
