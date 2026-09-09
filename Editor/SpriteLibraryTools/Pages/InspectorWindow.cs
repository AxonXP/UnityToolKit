using UnityEditor;

namespace SpriteLibraryTools
{
    public sealed class InspectorWindow : SingleToolPageWindow
    {
        [MenuItem("Tools/Sprite Library Tools/Library Inspector", priority = 104)]
        public static void Open()
        {
            OpenWindow<InspectorWindow>();
        }

        protected override IToolPage CreatePage() => new InspectorPage();
    }
}
