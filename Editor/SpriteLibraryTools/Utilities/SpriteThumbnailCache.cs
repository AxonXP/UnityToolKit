using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    // Wraps AssetPreview so pages don't each re-request the same thumbnail every OnGUI pass.
    public static class SpriteThumbnailCache
    {
        private static readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();

        public static Texture2D GetThumbnail(Sprite sprite)
        {
            if (sprite == null)
                return null;

            int id = sprite.GetInstanceID();
            if (cache.TryGetValue(id, out Texture2D cached) && cached != null)
                return cached;

            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null)
                return AssetPreview.GetMiniThumbnail(sprite);

            cache[id] = preview;
            return preview;
        }

        public static bool IsLoading(Sprite sprite)
        {
            return sprite != null && AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID());
        }

        public static void Clear()
        {
            cache.Clear();
        }
    }
}
