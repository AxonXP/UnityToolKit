using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    public readonly struct ScannedSprite
    {
        public readonly Sprite Sprite;
        public readonly string AssetPath;
        public readonly string FolderName;

        public ScannedSprite(Sprite sprite, string assetPath)
        {
            Sprite = sprite;
            AssetPath = assetPath;
            FolderName = Path.GetFileName(Path.GetDirectoryName(assetPath));
        }
    }

    // Finds every Sprite sub-asset under a set of folders/files, handling both
    // Single and Multiple sprite import mode via LoadAllAssetsAtPath.
    public static class SpriteAssetScanner
    {
        public static List<ScannedSprite> ScanPaths(IEnumerable<string> paths)
        {
            var results = new List<ScannedSprite>();
            var seenPaths = new HashSet<string>();

            foreach (string rawPath in paths)
            {
                if (string.IsNullOrEmpty(rawPath))
                    continue;

                if (AssetDatabase.IsValidFolder(rawPath))
                {
                    // Search both filters: "t:Sprite" alone can miss sub-sprites on textures
                    // imported in Multiple mode depending on how the asset index was built.
                    var guids = new HashSet<string>(AssetDatabase.FindAssets("t:Sprite", new[] { rawPath }));
                    guids.UnionWith(AssetDatabase.FindAssets("t:Texture2D", new[] { rawPath }));

                    foreach (string guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        CollectSprites(assetPath, seenPaths, results);
                    }
                }
                else
                {
                    CollectSprites(rawPath, seenPaths, results);
                }
            }

            return results;
        }

        private static void CollectSprites(string assetPath, HashSet<string> seenPaths, List<ScannedSprite> results)
        {
            if (!seenPaths.Add(assetPath))
                return;

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite sprite)
                    results.Add(new ScannedSprite(sprite, assetPath));
            }
        }
    }
}
