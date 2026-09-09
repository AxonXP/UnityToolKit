using System;
using System.Collections.Generic;

namespace SpriteLibraryTools
{
    // Plain [Serializable] data classes so JsonUtility can (de)serialize the nested
    // category/label structure directly.
    [Serializable]
    public sealed class ExportLabel
    {
        public string name;
        // spriteName disambiguates sub-sprites that share one texture's GUID (multi-sprite
        // mode) - the GUID/path alone can't tell two sprites on the same sheet apart.
        public string spriteName;
        public string spriteGuid;
        public string spritePath;
    }

    [Serializable]
    public sealed class ExportCategory
    {
        public string name;
        public List<ExportLabel> labels = new List<ExportLabel>();
    }

    [Serializable]
    public sealed class ExportLibrary
    {
        public string libraryName;
        public List<ExportCategory> categories = new List<ExportCategory>();
    }
}
