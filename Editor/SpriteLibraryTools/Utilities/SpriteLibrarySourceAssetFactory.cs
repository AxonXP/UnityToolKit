using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SpriteLibraryTools
{
    // SpriteLibrarySourceAsset (the .spriteLib backing type) is internal to the 2D Animation
    // package, so there is no public API to create one from outside it. Unity's own
    // "Assets/Create/2D/Sprite Library Asset" menu (CreateSpriteLibrarySourceAsset.Action in
    // SpriteLibrarySourceAssetImporterInspector.cs) does: CreateInstance, SetPrimaryLibraryGUID(""),
    // SaveToSerializedFileAndForget, then a forced reimport. Reflection is the only way to
    // reproduce that from a separate assembly; this is scoped narrowly and fails soft.
    public static class SpriteLibrarySourceAssetFactory
    {
        private const string TypeName = "UnityEngine.U2D.Animation.SpriteLibrarySourceAsset, Unity.2D.Animation.Runtime";

        public static bool TryCreate(string savePath, out string failureReason)
        {
            failureReason = null;
            try
            {
                System.Type sourceType = System.Type.GetType(TypeName);
                if (sourceType == null)
                {
                    failureReason = "SpriteLibrarySourceAsset type not found (2D Animation package layout may have changed).";
                    return false;
                }

                var instance = ScriptableObject.CreateInstance(sourceType);
                try
                {
                    MethodInfo setGuid = sourceType.GetMethod("SetPrimaryLibraryGUID", BindingFlags.Public | BindingFlags.Instance);
                    setGuid?.Invoke(instance, new object[] { string.Empty });

                    InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { instance }, savePath, true);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }

                AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
                return true;
            }
            catch (System.Exception ex)
            {
                failureReason = ex.Message;
                return false;
            }
        }
    }
}
