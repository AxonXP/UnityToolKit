using UnityEditor;
using UnityEngine;

namespace SpriteLibraryTools
{
    // Component inspectors like SpriteResolver's cache their Category/Label dropdown on
    // OnEnable and don't detect a SpriteLibraryAsset being edited externally. Clearing and
    // restoring the selection forces Unity to tear down and rebuild any open inspector.
    public static class InspectorRefresher
    {
        public static void RefreshOpenInspectors()
        {
            Object[] previousSelection = Selection.objects;
            if (previousSelection.Length == 0)
                return;

            Selection.objects = new Object[0];
            EditorApplication.delayCall += () => Selection.objects = previousSelection;
        }
    }
}
