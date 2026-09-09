using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    public enum RepairMethod
    {
        Filename,
        FolderSearch
    }

    // Reconnects labels with a missing Sprite by matching the label's name against sprite
    // names found in the project (Filename) or a specific folder (FolderSearch). A true
    // GUID-based repair isn't meaningfully possible here: if the reference is already null,
    // the stored GUID either points at nothing anymore or was never set, so there's nothing
    // left to resolve by GUID alone.
    public static class ReferenceRepairService
    {
        public static List<RepairCandidate> FindMissing(SpriteLibraryModel model)
        {
            var missing = new List<RepairCandidate>();
            if (model?.Categories == null)
                return missing;

            foreach (CategoryModel category in model.Categories)
                foreach (LabelModel label in category.Labels)
                    if (label.Sprite == null)
                        missing.Add(new RepairCandidate { Category = category.Name, Label = label.Name });

            return missing;
        }

        public static List<RepairCandidate> ResolveCandidates(List<RepairCandidate> missing, RepairMethod method, string folderPath)
        {
            string searchRoot = method == RepairMethod.FolderSearch && !string.IsNullOrEmpty(folderPath) ? folderPath : "Assets";
            List<ScannedSprite> sprites = SpriteAssetScanner.ScanPaths(new[] { searchRoot });

            var byName = new Dictionary<string, Sprite>();
            foreach (ScannedSprite scanned in sprites)
            {
                if (!byName.ContainsKey(scanned.Sprite.name))
                    byName[scanned.Sprite.name] = scanned.Sprite;
            }

            foreach (RepairCandidate candidate in missing)
            {
                if (byName.TryGetValue(candidate.Label, out Sprite sprite))
                    candidate.FoundSprite = sprite;
            }

            return missing;
        }

        public static SpriteLibraryAsset ApplyRepairs(SpriteLibraryAsset library, IEnumerable<RepairCandidate> candidates)
        {
            IEnumerable<SpriteLibrarySourceIO.SourceLabel> writes = candidates
                .Where(c => c.FoundSprite != null)
                .Select(c => new SpriteLibrarySourceIO.SourceLabel(c.Category, c.Label, c.FoundSprite));

            return SpriteLibrarySourceIO.ApplyWrites(library, writes, "Repair References");
        }
    }
}
