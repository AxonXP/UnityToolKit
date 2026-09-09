using System.Text;
using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    public static class LibraryExportService
    {
        public static string ToJson(SpriteLibraryModel model, string libraryName)
        {
            var export = new ExportLibrary { libraryName = libraryName };

            foreach (CategoryModel category in model.Categories)
            {
                var exportCategory = new ExportCategory { name = category.Name };
                foreach (LabelModel label in category.Labels)
                    exportCategory.labels.Add(ToExportLabel(label));

                export.categories.Add(exportCategory);
            }

            return JsonUtility.ToJson(export, true);
        }

        public static string ToCsv(SpriteLibraryModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Category,Label,SpriteName,SpritePath,SpriteGuid");

            foreach (CategoryModel category in model.Categories)
            {
                foreach (LabelModel label in category.Labels)
                {
                    ExportLabel exportLabel = ToExportLabel(label);
                    sb.AppendLine(string.Join(",",
                        CsvEscape(category.Name),
                        CsvEscape(exportLabel.name),
                        CsvEscape(exportLabel.spriteName),
                        CsvEscape(exportLabel.spritePath),
                        CsvEscape(exportLabel.spriteGuid)));
                }
            }

            return sb.ToString();
        }

        private static ExportLabel ToExportLabel(LabelModel label)
        {
            string path = label.Sprite != null ? AssetDatabase.GetAssetPath(label.Sprite) : string.Empty;
            string guid = !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : string.Empty;

            return new ExportLabel
            {
                name = label.Name,
                spriteName = label.Sprite != null ? label.Sprite.name : string.Empty,
                spriteGuid = guid,
                spritePath = path
            };
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }
    }
}
