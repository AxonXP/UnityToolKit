using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public static class LabelMoverService
    {
        // Returns the (possibly reloaded) library, or the original unchanged if moving isn't
        // supported for this asset type. Only .spriteLib source assets support renaming a
        // label's category in place; the plain SpriteLibraryAsset public API has no rename,
        // only add/remove, so a "move" there would have to be a remove+add with no atomicity.
        public static SpriteLibraryAsset Move(SpriteLibraryAsset library, IEnumerable<(string fromCategory, string toCategory, string label)> moves)
        {
            string path = AssetDatabase.GetAssetPath(library);
            if (!SpriteLibrarySourceIO.IsSourceAsset(path))
            {
                Debug.LogWarning("Sprite Library Tools: moving labels between categories is only supported for .spriteLib assets.");
                return library;
            }

            SpriteLibrarySourceIO.MoveLabels(path, moves);
            return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
        }
    }
}
