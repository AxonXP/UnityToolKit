using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public static class LibraryImportService
    {
        public static List<ImportEntry> ParseJson(string json, out string error)
        {
            error = null;
            var entries = new List<ImportEntry>();

            ExportLibrary export;
            try
            {
                export = JsonUtility.FromJson<ExportLibrary>(json);
            }
            catch (System.Exception ex)
            {
                error = ex.Message;
                return entries;
            }

            if (export?.categories == null)
            {
                error = "No categories found in file.";
                return entries;
            }

            foreach (ExportCategory category in export.categories)
            {
                foreach (ExportLabel label in category.labels ?? new List<ExportLabel>())
                {
                    entries.Add(new ImportEntry
                    {
                        Category = category.name,
                        Label = label.name,
                        SpriteName = label.spriteName,
                        SpriteGuid = label.spriteGuid,
                        SpritePath = label.spritePath
                    });
                }
            }

            return ResolveSprites(entries);
        }

        public static List<ImportEntry> ParseCsv(string csv, out string error)
        {
            error = null;
            var entries = new List<ImportEntry>();
            string[] lines = csv.Replace("\r\n", "\n").Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> fields = ParseCsvLine(line);
                if (fields.Count < 2)
                    continue;

                entries.Add(new ImportEntry
                {
                    Category = fields[0],
                    Label = fields[1],
                    SpriteName = fields.Count > 2 ? fields[2] : string.Empty,
                    SpritePath = fields.Count > 3 ? fields[3] : string.Empty,
                    SpriteGuid = fields.Count > 4 ? fields[4] : string.Empty
                });
            }

            return ResolveSprites(entries);
        }

        private static List<ImportEntry> ResolveSprites(List<ImportEntry> entries)
        {
            foreach (ImportEntry entry in entries)
            {
                string path = null;
                if (!string.IsNullOrEmpty(entry.SpriteGuid))
                    path = AssetDatabase.GUIDToAssetPath(entry.SpriteGuid);

                if (string.IsNullOrEmpty(path))
                    path = entry.SpritePath;

                entry.ResolvedSprite = LoadSprite(path, entry.SpriteName);
            }

            return entries;
        }

        // Sub-sprites of a multi-sprite texture share one GUID/path, so the exported sprite
        // name is used to pick the right one; falls back to the first sprite found if the
        // name doesn't match (e.g. the sheet was re-sliced since export).
        private static Sprite LoadSprite(string path, string expectedName)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            Sprite fallback = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    if (sprite.name == expectedName)
                        return sprite;
                    fallback ??= sprite;
                }
            }

            return fallback;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }

        public static SpriteLibraryAsset Apply(SpriteLibraryAsset library, IEnumerable<ImportEntry> entries, string undoName)
        {
            IEnumerable<SpriteLibrarySourceIO.SourceLabel> writes = entries
                .Where(e => e.ResolvedSprite != null)
                .Select(e => new SpriteLibrarySourceIO.SourceLabel(e.Category, e.Label, e.ResolvedSprite));

            return SpriteLibrarySourceIO.ApplyWrites(library, writes, undoName);
        }
    }
}
