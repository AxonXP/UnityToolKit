using System.Collections.Generic;

namespace SpriteLibraryTools
{
    public static class UniqueNameGenerator
    {
        public static string MakeUnique(string baseName, ICollection<string> existingNames)
        {
            int suffix = 1;
            string candidate = $"{baseName}_{suffix}";
            while (existingNames.Contains(candidate))
            {
                suffix++;
                candidate = $"{baseName}_{suffix}";
            }
            return candidate;
        }
    }
}
