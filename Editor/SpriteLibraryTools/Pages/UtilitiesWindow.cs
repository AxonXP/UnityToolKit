using UnityEditor;

namespace SpriteLibraryTools
{
    public sealed class UtilitiesWindow : SingleToolPageWindow
    {
        [MenuItem("Tools/Sprite Library Tools/Utilities", priority = 105)]
        public static void Open()
        {
            OpenWindow<UtilitiesWindow>();
        }

        protected override IToolPage CreatePage() => new UtilitiesPage();
    }
}
