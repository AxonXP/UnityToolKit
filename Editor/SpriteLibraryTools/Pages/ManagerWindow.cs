using UnityEditor;

namespace SpriteLibraryTools
{
    public sealed class ManagerWindow : SingleToolPageWindow
    {
        [MenuItem("Tools/Sprite Library Tools/Library Manager", priority = 103)]
        public static void Open()
        {
            OpenWindow<ManagerWindow>();
        }

        protected override IToolPage CreatePage() => new ManagerPage();
    }
}
