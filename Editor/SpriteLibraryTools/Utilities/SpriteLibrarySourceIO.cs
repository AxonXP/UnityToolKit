using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // .spriteLib assets (SpriteLibrarySourceAsset) are ScriptedImporter sources: the
    // SpriteLibraryAsset you get back from AssetDatabase.LoadAssetAtPath is a generated
    // runtime copy, rebuilt from this source file on every reimport. Editing that generated
    // copy through the public SpriteLibraryAsset API (AddCategoryLabel etc.) is silently
    // discarded the next time Unity reimports. Categories/labels must be written into the
    // source YAML itself via SerializedObject, then the asset force-reimported so the
    // generated copy picks up the change. Mirrors the technique proven by this project's
    // SpriteLibraryFiller/SpriteLibToAnimClip prototypes.
    public static class SpriteLibrarySourceIO
    {
        public readonly struct SourceLabel
        {
            public readonly string Category;
            public readonly string Label;
            public readonly Sprite Sprite;

            public SourceLabel(string category, string label, Sprite sprite)
            {
                Category = category;
                Label = label;
                Sprite = sprite;
            }
        }

        public static bool IsSourceAsset(string path)
        {
            return !string.IsNullOrEmpty(path) && path.EndsWith(".spriteLib", System.StringComparison.OrdinalIgnoreCase);
        }

        // Writes categories/labels for either asset type, returning the (possibly reloaded)
        // target. Shared by every service that only ever needs to add or overwrite entries -
        // Builder, Batch Replace, Merge, Duplicate Categories.
        public static SpriteLibraryAsset ApplyWrites(SpriteLibraryAsset target, IEnumerable<SourceLabel> writes, string undoName)
        {
            List<SourceLabel> writeList = writes.ToList();
            if (writeList.Count == 0)
                return target;

            string path = AssetDatabase.GetAssetPath(target);
            if (IsSourceAsset(path))
            {
                WriteLabels(path, writeList);
                return AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path);
            }

            Undo.RecordObject(target, undoName);
            foreach (SourceLabel write in writeList)
                target.AddCategoryLabel(write.Sprite, write.Category, write.Label);

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            return target;
        }

        public static List<SourceLabel> ReadLabels(string path)
        {
            var results = new List<SourceLabel>();
            if (!TryLoad(path, out _, out _, out SerializedProperty library))
                return results;

            for (int i = 0; i < library.arraySize; i++)
            {
                SerializedProperty category = library.GetArrayElementAtIndex(i);
                string categoryName = category.FindPropertyRelative("m_Name").stringValue;
                SerializedProperty entries = category.FindPropertyRelative("m_OverrideEntries");

                for (int j = 0; j < entries.arraySize; j++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                    string labelName = entry.FindPropertyRelative("m_Name").stringValue;
                    var sprite = entry.FindPropertyRelative("m_SpriteOverride").objectReferenceValue as Sprite;
                    results.Add(new SourceLabel(categoryName, labelName, sprite));
                }
            }

            return results;
        }

        public static void WriteLabels(string path, IEnumerable<SourceLabel> writes)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            foreach (SourceLabel write in writes)
            {
                SerializedProperty category = FindOrCreateCategory(library, write.Category);
                SerializedProperty entries = category.FindPropertyRelative("m_OverrideEntries");
                SerializedProperty entry = FindOrCreateEntry(entries, write.Label);

                entry.FindPropertyRelative("m_Name").stringValue = write.Label;
                entry.FindPropertyRelative("m_Hash").intValue = Animator.StringToHash(write.Label);
                entry.FindPropertyRelative("m_Sprite").objectReferenceValue = write.Sprite;
                entry.FindPropertyRelative("m_FromMain").intValue = 0;
                entry.FindPropertyRelative("m_SpriteOverride").objectReferenceValue = write.Sprite;

                category.FindPropertyRelative("m_EntryOverrideCount").intValue = entries.arraySize;
            }

            SaveAndReimport(objects, serializedObject, path);
        }

        public static void RemoveLabels(string path, IEnumerable<(string category, string label)> removals)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            foreach ((string category, string label) removal in removals)
            {
                SerializedProperty entries = FindEntriesProperty(library, removal.category);
                if (entries == null)
                    continue;

                for (int j = 0; j < entries.arraySize; j++)
                {
                    if (entries.GetArrayElementAtIndex(j).FindPropertyRelative("m_Name").stringValue != removal.label)
                        continue;

                    entries.DeleteArrayElementAtIndex(j);
                    break;
                }
            }

            SyncOverrideCounts(library);
            SaveAndReimport(objects, serializedObject, path);
        }

        public static void RemoveCategories(string path, IEnumerable<string> categories)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            var toRemove = new HashSet<string>(categories);
            for (int i = library.arraySize - 1; i >= 0; i--)
            {
                if (toRemove.Contains(library.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue))
                    library.DeleteArrayElementAtIndex(i);
            }

            SaveAndReimport(objects, serializedObject, path);
        }

        public static void RenameCategories(string path, IReadOnlyDictionary<string, string> renames)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            for (int i = 0; i < library.arraySize; i++)
            {
                SerializedProperty category = library.GetArrayElementAtIndex(i);
                string currentName = category.FindPropertyRelative("m_Name").stringValue;
                if (!renames.TryGetValue(currentName, out string newName) || string.IsNullOrEmpty(newName))
                    continue;

                category.FindPropertyRelative("m_Name").stringValue = newName;
                category.FindPropertyRelative("m_Hash").intValue = Animator.StringToHash(newName);
            }

            SaveAndReimport(objects, serializedObject, path);
        }

        public static void RenameLabels(string path, IEnumerable<(string category, string oldLabel, string newLabel)> renames)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            foreach ((string category, string oldLabel, string newLabel) rename in renames)
            {
                if (string.IsNullOrEmpty(rename.newLabel))
                    continue;

                SerializedProperty entries = FindEntriesProperty(library, rename.category);
                if (entries == null)
                    continue;

                for (int j = 0; j < entries.arraySize; j++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                    if (entry.FindPropertyRelative("m_Name").stringValue != rename.oldLabel)
                        continue;

                    entry.FindPropertyRelative("m_Name").stringValue = rename.newLabel;
                    entry.FindPropertyRelative("m_Hash").intValue = Animator.StringToHash(rename.newLabel);
                    break;
                }
            }

            SaveAndReimport(objects, serializedObject, path);
        }

        // Removes each label from its source category and adds it to its destination
        // category (find-or-create), all in a single load/save pass so a failure partway
        // through can't leave a label duplicated in both places.
        public static void MoveLabels(string path, IEnumerable<(string fromCategory, string toCategory, string label)> moves)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            foreach ((string fromCategory, string toCategory, string label) move in moves)
            {
                if (move.fromCategory == move.toCategory)
                    continue;

                SerializedProperty fromEntries = FindEntriesProperty(library, move.fromCategory);
                if (fromEntries == null)
                    continue;

                Sprite sprite = null;
                bool found = false;
                for (int j = 0; j < fromEntries.arraySize; j++)
                {
                    SerializedProperty entry = fromEntries.GetArrayElementAtIndex(j);
                    if (entry.FindPropertyRelative("m_Name").stringValue != move.label)
                        continue;

                    sprite = entry.FindPropertyRelative("m_SpriteOverride").objectReferenceValue as Sprite;
                    fromEntries.DeleteArrayElementAtIndex(j);
                    found = true;
                    break;
                }

                if (!found)
                    continue;

                SerializedProperty toCategory = FindOrCreateCategory(library, move.toCategory);
                SerializedProperty toEntries = toCategory.FindPropertyRelative("m_OverrideEntries");
                SerializedProperty newEntry = FindOrCreateEntry(toEntries, move.label);
                newEntry.FindPropertyRelative("m_Name").stringValue = move.label;
                newEntry.FindPropertyRelative("m_Hash").intValue = Animator.StringToHash(move.label);
                newEntry.FindPropertyRelative("m_Sprite").objectReferenceValue = sprite;
                newEntry.FindPropertyRelative("m_FromMain").intValue = 0;
                newEntry.FindPropertyRelative("m_SpriteOverride").objectReferenceValue = sprite;
                toCategory.FindPropertyRelative("m_EntryOverrideCount").intValue = toEntries.arraySize;
            }

            SyncOverrideCounts(library);
            SaveAndReimport(objects, serializedObject, path);
        }

        // Rewrites the category array to match orderedCategoryNames. Any existing category
        // not present in the list keeps its relative position at the end.
        public static void ReorderCategories(string path, IReadOnlyList<string> orderedCategoryNames)
        {
            if (!TryLoad(path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library))
                return;

            // Selection sort via MoveArrayElement: positions before targetIndex are already
            // final, so search only the remaining tail for the next desired category and move
            // it into place. Categories not named in orderedCategoryNames are left untouched
            // at the end, in their existing relative order.
            for (int targetIndex = 0; targetIndex < orderedCategoryNames.Count && targetIndex < library.arraySize; targetIndex++)
            {
                string desiredName = orderedCategoryNames[targetIndex];
                int currentIndex = -1;
                for (int i = targetIndex; i < library.arraySize; i++)
                {
                    if (library.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == desiredName)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                if (currentIndex < 0)
                    continue;

                if (currentIndex != targetIndex)
                    library.MoveArrayElement(currentIndex, targetIndex);
            }

            SaveAndReimport(objects, serializedObject, path);
        }

        private static SerializedProperty FindEntriesProperty(SerializedProperty library, string categoryName)
        {
            for (int i = 0; i < library.arraySize; i++)
            {
                SerializedProperty category = library.GetArrayElementAtIndex(i);
                if (category.FindPropertyRelative("m_Name").stringValue == categoryName)
                    return category.FindPropertyRelative("m_OverrideEntries");
            }
            return null;
        }

        private static void SyncOverrideCounts(SerializedProperty library)
        {
            for (int i = 0; i < library.arraySize; i++)
            {
                SerializedProperty category = library.GetArrayElementAtIndex(i);
                SerializedProperty entries = category.FindPropertyRelative("m_OverrideEntries");
                category.FindPropertyRelative("m_EntryOverrideCount").intValue = entries.arraySize;
            }
        }

        private static SerializedProperty FindOrCreateCategory(SerializedProperty library, string categoryName)
        {
            for (int i = 0; i < library.arraySize; i++)
            {
                SerializedProperty category = library.GetArrayElementAtIndex(i);
                if (category.FindPropertyRelative("m_Name").stringValue == categoryName)
                    return category;
            }

            int newIndex = library.arraySize;
            library.arraySize = newIndex + 1;
            SerializedProperty created = library.GetArrayElementAtIndex(newIndex);
            created.FindPropertyRelative("m_Name").stringValue = categoryName;
            created.FindPropertyRelative("m_Hash").intValue = Animator.StringToHash(categoryName);
            created.FindPropertyRelative("m_OverrideEntries").arraySize = 0;
            created.FindPropertyRelative("m_EntryOverrideCount").intValue = 0;
            created.FindPropertyRelative("m_FromMain").intValue = 0;
            return created;
        }

        private static SerializedProperty FindOrCreateEntry(SerializedProperty entries, string labelName)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("m_Name").stringValue == labelName)
                    return entry;
            }

            int newIndex = entries.arraySize;
            entries.arraySize = newIndex + 1;
            return entries.GetArrayElementAtIndex(newIndex);
        }

        private static bool TryLoad(string path, out Object[] objects, out SerializedObject serializedObject, out SerializedProperty library)
        {
            objects = null;
            serializedObject = null;
            library = null;

            if (string.IsNullOrEmpty(path))
                return false;

            objects = InternalEditorUtility.LoadSerializedFileAndForget(path);
            if (objects == null || objects.Length == 0 || objects[0] == null)
                return false;

            serializedObject = new SerializedObject(objects[0]);
            library = serializedObject.FindProperty("m_Library");
            return library != null && library.isArray;
        }

        private static void SaveAndReimport(Object[] objects, SerializedObject serializedObject, string path)
        {
            // Backs up the file's current (pre-write) contents. This is the single choke
            // point every .spriteLib mutation in the suite passes through, so hooking it here
            // gives every tool automatic backups without each one calling it separately.
            LibraryBackupService.CreateBackupIfEnabled(path);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            InternalEditorUtility.SaveToSerializedFileAndForget(objects, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
