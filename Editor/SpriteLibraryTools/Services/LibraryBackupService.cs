using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // .spriteLib files are plain YAML, so a raw file copy is a complete, reliable backup -
    // no need to snapshot SerializedProperty state. Wired into
    // SpriteLibrarySourceIO.SaveAndReimport, so every write this suite makes (Builder, Batch
    // Replace, every Manager tool) is backed up first. That matters more than usual here since
    // .spriteLib writes bypass Unity's Undo system entirely (see SpriteLibrarySourceIO).
    // Backups live outside Assets/ so they never clutter the Project window or get imported.
    public static class LibraryBackupService
    {
        private const string EnabledPrefKey = "BackupsEnabled";
        private const string MaxBackupsPrefKey = "MaxBackupsPerLibrary";
        private const int DefaultMaxBackups = 10;
        private const string BackupExtension = ".spriteLib.bak";

        public static bool Enabled
        {
            get => EditorPrefsState.GetBool(EnabledPrefKey, true);
            set => EditorPrefsState.SetBool(EnabledPrefKey, value);
        }

        public static int MaxBackupsPerLibrary
        {
            get => EditorPrefsState.GetInt(MaxBackupsPrefKey, DefaultMaxBackups);
            set => EditorPrefsState.SetInt(MaxBackupsPrefKey, value);
        }

        public static void CreateBackupIfEnabled(string projectRelativePath)
        {
            if (Enabled)
                CreateBackup(projectRelativePath);
        }

        public static string CreateBackup(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath))
                return null;

            string absoluteSourcePath = ToAbsolutePath(projectRelativePath);
            if (!File.Exists(absoluteSourcePath))
                return null;

            string backupDir = GetBackupFolder();
            Directory.CreateDirectory(backupDir);

            string baseName = Path.GetFileNameWithoutExtension(projectRelativePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string backupPath = Path.Combine(backupDir, $"{baseName}_{timestamp}{BackupExtension}");

            File.Copy(absoluteSourcePath, backupPath, true);
            PruneOldBackups(baseName);
            return backupPath;
        }

        public static List<string> GetBackups(SpriteLibraryAsset library)
        {
            string path = AssetDatabase.GetAssetPath(library);
            return GetBackupsForBaseName(Path.GetFileNameWithoutExtension(path));
        }

        public static void Restore(SpriteLibraryAsset library, string backupPath)
        {
            string path = AssetDatabase.GetAssetPath(library);
            string absoluteTargetPath = ToAbsolutePath(path);
            File.Copy(backupPath, absoluteTargetPath, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static List<string> GetBackupsForBaseName(string baseName)
        {
            string backupDir = GetBackupFolder();
            if (!Directory.Exists(backupDir))
                return new List<string>();

            return Directory.GetFiles(backupDir, $"{baseName}_*{BackupExtension}")
                .OrderByDescending(f => f)
                .ToList();
        }

        private static void PruneOldBackups(string baseName)
        {
            List<string> backups = GetBackupsForBaseName(baseName);
            int max = Mathf.Max(1, MaxBackupsPerLibrary);
            for (int i = max; i < backups.Count; i++)
            {
                try
                {
                    File.Delete(backups[i]);
                }
                catch (IOException)
                {
                }
            }
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, projectRelativePath);
        }

        private static string GetBackupFolder()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, "SpriteLibraryToolsBackups");
        }
    }
}
