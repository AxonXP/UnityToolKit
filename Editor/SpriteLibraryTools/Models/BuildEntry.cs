using UnityEngine;

namespace SpriteLibraryTools
{
    public enum BuildEntryStatus
    {
        New,
        WillReplaceDuplicate,
        WillSkipDuplicate,
        RenamedDuplicate,
        UnmatchedCategory
    }

    public sealed class BuildEntry
    {
        public string Category;
        public string Label;
        public Sprite Sprite;
        public BuildEntryStatus Status;

        public bool WillApply =>
            Status == BuildEntryStatus.New ||
            Status == BuildEntryStatus.WillReplaceDuplicate ||
            Status == BuildEntryStatus.RenamedDuplicate;
    }
}
