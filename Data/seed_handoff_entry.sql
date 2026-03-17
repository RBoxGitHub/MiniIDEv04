-- sys_ThingsToDo seed — run against ProjectNotes/miniIDE.db if needed
-- ProjectDatabase.Initialize() handles this automatically on first run

INSERT INTO sys_ThingsToDo
  (IsComplete, SideNote, ReferenceObject, TypeReference, FileName, FilePath, Phase, Priority, CreatedAt, UpdatedAt)
VALUES
  (1,
   'MiniIDEv03 Handoff PDF generated. Full architecture, DB schema, file inventory, Phase 2 task list, session restart instructions. Attach to any new Claude session to resume without context loss.',
   'MiniIDEv03_Handoff',
   'MiniIDEv03.Documentation.Handoff',
   'MiniIDEv01_Handoff.pdf',
   'ProjectNotes/MiniIDEv01_Handoff.pdf',
   1, 3,
   datetime('now'), datetime('now'));
