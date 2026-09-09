using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    [ToolPage(4)]
    public sealed class UtilitiesPage : IToolPage
    {
        private enum SubTab
        {
            Export,
            Import,
            Backup,
            RepairReferences,
            LibraryDiff,
            DependencyViewer
        }

        public string Title => "Utilities";

        private SubTab activeSubTab;
        private SpriteLibraryAsset target;
        private Rect targetDropRect;
        private SpriteLibraryModel cachedModel;

        // Export state
        private int exportFormatIndex;

        // Backup state
        private List<string> backupList;

        // Repair state
        private RepairMethod repairMethod;
        private DefaultAsset repairFolderAsset;
        private List<RepairCandidate> repairCandidates;
        private Vector2 repairScroll;

        // Diff state
        private SpriteLibraryAsset diffLibraryB;
        private Rect diffLibraryBDropRect;
        private List<DiffEntry> diffResults;
        private Vector2 diffScroll;

        // Dependency Viewer state
        private Sprite dependencySprite;
        private List<(string category, string label)> dependencyResults;

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            DrawSubTabs();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Target Library", EditorStyles.boldLabel);
            SpriteLibraryAsset newTarget = LibraryPickerField.Draw("Sprite Library Asset", target, ref targetDropRect);
            if (newTarget != target)
            {
                target = newTarget;
                RefreshModel();
            }
            else if (cachedModel == null && target != null)
            {
                RefreshModel();
            }

            EditorGUILayout.Space();

            switch (activeSubTab)
            {
                case SubTab.Export:
                    DrawExport();
                    break;
                case SubTab.Import:
                    DrawImport();
                    break;
                case SubTab.Backup:
                    DrawBackup();
                    break;
                case SubTab.RepairReferences:
                    DrawRepair();
                    break;
                case SubTab.LibraryDiff:
                    DrawDiff();
                    break;
                case SubTab.DependencyViewer:
                    DrawDependencyViewer();
                    break;
            }
        }

        private void RefreshModel()
        {
            cachedModel = target != null ? SpriteLibraryModel.FromAsset(target) : null;
            repairCandidates = null;
            backupList = null;
            diffResults = null;
            dependencyResults = null;
        }

        private void DrawSubTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            activeSubTab = (SubTab)EditorGUILayout.Popup(
                (int)activeSubTab,
                new[] { "Export", "Import", "Backup", "Repair References", "Library Diff", "Dependency Viewer" },
                EditorStyles.toolbarPopup,
                GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        // ----- Export -----

        private void DrawExport()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to export it.", MessageType.Info);
                return;
            }

            if (cachedModel.Categories.Count == 0)
            {
                EditorGUILayout.HelpBox("This library has no categories yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Format", EditorStyles.boldLabel);
            exportFormatIndex = EditorGUILayout.Popup(exportFormatIndex, new[] { "JSON", "CSV" });

            EditorGUILayout.Space();
            if (GUILayout.Button("Export...", GUILayout.Height(30)))
                RunExport();
        }

        private void RunExport()
        {
            bool json = exportFormatIndex == 0;
            string extension = json ? "json" : "csv";

            string path = EditorUtility.SaveFilePanel("Export Sprite Library", string.Empty, target.name, extension);
            if (string.IsNullOrEmpty(path))
                return;

            string content = json ? LibraryExportService.ToJson(cachedModel, target.name) : LibraryExportService.ToCsv(cachedModel);
            System.IO.File.WriteAllText(path, content);

            ToolStatus.Set($"Exported '{target.name}' to '{path}'.");
            EditorUtility.RevealInFinder(path);
        }

        // ----- Import -----

        private void DrawImport()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to import into.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Existing labels with a matching category/label name will be overwritten.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import JSON...", GUILayout.Height(30)))
                RunImport(true);
            if (GUILayout.Button("Import CSV...", GUILayout.Height(30)))
                RunImport(false);
            EditorGUILayout.EndHorizontal();
        }

        private void RunImport(bool json)
        {
            string extension = json ? "json" : "csv";
            string path = EditorUtility.OpenFilePanel("Import Sprite Library", string.Empty, extension);
            if (string.IsNullOrEmpty(path))
                return;

            string content = System.IO.File.ReadAllText(path);
            string error;
            List<ImportEntry> entries = json
                ? LibraryImportService.ParseJson(content, out error)
                : LibraryImportService.ParseCsv(content, out error);

            if (!string.IsNullOrEmpty(error))
            {
                ToolStatus.Set($"Import failed: {error}");
                Debug.LogWarning($"Sprite Library Tools: import failed - {error}");
                return;
            }

            int resolved = entries.Count(e => e.ResolvedSprite != null);
            int unresolved = entries.Count - resolved;

            target = LibraryImportService.Apply(target, entries, "Import Sprite Library");
            InspectorRefresher.RefreshOpenInspectors();
            RefreshModel();
            ToolStatus.Set($"Import: added {resolved} label(s)." + (unresolved > 0 ? $" {unresolved} sprite(s) could not be found." : string.Empty));
        }

        // ----- Backup -----

        private void DrawBackup()
        {
            EditorGUILayout.LabelField("Automatic Backups", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Every write this suite makes to a .spriteLib file is backed up first (a raw file copy, stored outside Assets/). " +
                "This matters because .spriteLib edits bypass Unity's Undo system entirely.",
                MessageType.Info);

            bool enabled = EditorGUILayout.ToggleLeft("Enable Automatic Backups", LibraryBackupService.Enabled);
            if (enabled != LibraryBackupService.Enabled)
                LibraryBackupService.Enabled = enabled;

            int maxBackups = EditorGUILayout.IntField("Max Backups Per Library", LibraryBackupService.MaxBackupsPerLibrary);
            if (maxBackups != LibraryBackupService.MaxBackupsPerLibrary)
                LibraryBackupService.MaxBackupsPerLibrary = Mathf.Max(1, maxBackups);

            EditorGUILayout.Space();

            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to create or restore a backup.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Create Backup Now", GUILayout.Height(24)))
            {
                string path = AssetDatabase.GetAssetPath(target);
                string backupPath = LibraryBackupService.CreateBackup(path);
                ToolStatus.Set(backupPath != null ? $"Backup created: '{backupPath}'." : "Backup failed - is this a .spriteLib asset?");
                backupList = null;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Restore", EditorStyles.boldLabel);

            if (backupList == null)
                backupList = LibraryBackupService.GetBackups(target);

            if (backupList.Count == 0)
            {
                EditorGUILayout.HelpBox("No backups found for this library yet.", MessageType.Info);
                return;
            }

            foreach (string backupPath in backupList)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(System.IO.Path.GetFileName(backupPath));
                if (GUILayout.Button("Restore", GUILayout.Width(70)))
                    RunRestore(backupPath);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RunRestore(string backupPath)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Restore Backup",
                $"Replace the current contents of '{target.name}' with this backup? This overwrites the library and cannot be undone.",
                "Restore",
                "Cancel");

            if (!confirmed)
                return;

            LibraryBackupService.Restore(target, backupPath);
            target = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(AssetDatabase.GetAssetPath(target));
            InspectorRefresher.RefreshOpenInspectors();
            RefreshModel();
            ToolStatus.Set($"Restored '{target.name}' from backup.");
        }

        // ----- Repair References -----

        private void DrawRepair()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to repair its references.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Method", EditorStyles.boldLabel);
            repairMethod = (RepairMethod)EditorGUILayout.Popup((int)repairMethod, new[] { "Filename (whole project)", "Folder Search" });

            if (repairMethod == RepairMethod.FolderSearch)
                repairFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Search Folder", repairFolderAsset, typeof(DefaultAsset), false);

            EditorGUILayout.Space();
            if (GUILayout.Button("Scan For Missing References"))
            {
                List<RepairCandidate> missing = ReferenceRepairService.FindMissing(cachedModel);
                string folderPath = repairFolderAsset != null ? AssetDatabase.GetAssetPath(repairFolderAsset) : null;
                repairCandidates = ReferenceRepairService.ResolveCandidates(missing, repairMethod, folderPath);
            }

            DrawRepairResults();

            EditorGUILayout.Space();
            bool canApply = repairCandidates != null && repairCandidates.Any(c => c.FoundSprite != null);
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Repair", GUILayout.Height(30)))
                    ApplyRepair();
            }
        }

        private void DrawRepairResults()
        {
            if (repairCandidates == null)
            {
                EditorGUILayout.HelpBox("Click Scan For Missing References to check this library.", MessageType.Info);
                return;
            }

            if (repairCandidates.Count == 0)
            {
                EditorGUILayout.HelpBox("No missing references found.", MessageType.Info);
                return;
            }

            repairScroll = EditorGUILayout.BeginScrollView(repairScroll, GUILayout.Height(140));
            foreach (RepairCandidate candidate in repairCandidates)
            {
                string line = candidate.FoundSprite != null
                    ? $"{candidate.Category}/{candidate.Label} -> found '{candidate.FoundSprite.name}'"
                    : $"{candidate.Category}/{candidate.Label}: no match found";
                EditorGUILayout.LabelField(line);
            }
            EditorGUILayout.EndScrollView();

            int foundCount = repairCandidates.Count(c => c.FoundSprite != null);
            EditorGUILayout.LabelField($"{foundCount} of {repairCandidates.Count} missing reference(s) can be repaired.");
        }

        private void ApplyRepair()
        {
            int count = repairCandidates.Count(c => c.FoundSprite != null);
            target = ReferenceRepairService.ApplyRepairs(target, repairCandidates);
            InspectorRefresher.RefreshOpenInspectors();
            RefreshModel();
            ToolStatus.Set($"Repaired {count} reference(s).");
        }

        // ----- Library Diff -----

        private void DrawDiff()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above as Library A.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Compare With (Library B)", EditorStyles.boldLabel);
            diffLibraryB = LibraryPickerField.Draw("Library B", diffLibraryB, ref diffLibraryBDropRect);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(diffLibraryB == null))
            {
                if (GUILayout.Button("Compare"))
                    diffResults = LibraryDiffService.Compare(target, diffLibraryB);
            }

            DrawDiffResults();
        }

        private void DrawDiffResults()
        {
            if (diffResults == null)
            {
                EditorGUILayout.HelpBox("Click Compare to see differences. Duplicate sprites and missing references for a single library are covered by the Library Inspector's Validation tab.", MessageType.Info);
                return;
            }

            diffScroll = EditorGUILayout.BeginScrollView(diffScroll, GUILayout.Height(200));
            foreach (DiffEntry entry in diffResults)
            {
                if (entry.Type == DiffChangeType.Unchanged)
                    continue;

                string line = entry.Type switch
                {
                    DiffChangeType.Added => $"+ {entry.Category}/{entry.Label} (added in B)",
                    DiffChangeType.Removed => $"- {entry.Category}/{entry.Label} (missing from B)",
                    DiffChangeType.Modified => $"~ {entry.Category}/{entry.Label}: {SpriteLabel(entry.SpriteA)} -> {SpriteLabel(entry.SpriteB)}",
                    _ => string.Empty
                };
                EditorGUILayout.LabelField(line);
            }
            EditorGUILayout.EndScrollView();

            int added = diffResults.Count(e => e.Type == DiffChangeType.Added);
            int removed = diffResults.Count(e => e.Type == DiffChangeType.Removed);
            int modified = diffResults.Count(e => e.Type == DiffChangeType.Modified);
            int unchanged = diffResults.Count(e => e.Type == DiffChangeType.Unchanged);
            EditorGUILayout.LabelField($"{added} added, {removed} removed, {modified} modified, {unchanged} unchanged.");
        }

        private static string SpriteLabel(Sprite sprite)
        {
            return sprite != null ? sprite.name : "(none)";
        }

        // ----- Dependency Viewer -----

        private void DrawDependencyViewer()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Select a Sprite Library above to inspect.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            dependencySprite = (Sprite)EditorGUILayout.ObjectField("Sprite", dependencySprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
                dependencyResults = null;

            if (dependencySprite == null)
            {
                EditorGUILayout.HelpBox("Pick a Sprite to see which categories/labels in this library reference it.", MessageType.Info);
                return;
            }

            if (dependencyResults == null)
                dependencyResults = DependencyViewerService.FindReferences(cachedModel, dependencySprite);

            EditorGUILayout.Space();
            if (dependencyResults.Count == 0)
            {
                EditorGUILayout.HelpBox($"'{dependencySprite.name}' is not referenced by this library.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Referenced by {dependencyResults.Count} label(s):", EditorStyles.boldLabel);
            foreach ((string category, string label) reference in dependencyResults)
                EditorGUILayout.LabelField($"{reference.category}/{reference.label}");
        }
    }
}
