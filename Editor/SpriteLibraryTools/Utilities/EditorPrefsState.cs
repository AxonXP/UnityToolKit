using UnityEditor;

namespace SpriteLibraryTools
{
    // Thin, namespaced wrapper over EditorPrefs so every page shares one key convention
    // for remembering the last selected page, folders, and options between sessions.
    public static class EditorPrefsState
    {
        private const string KeyPrefix = "SpriteLibraryTools.";

        public static string GetString(string key, string defaultValue) => EditorPrefs.GetString(KeyPrefix + key, defaultValue);
        public static void SetString(string key, string value) => EditorPrefs.SetString(KeyPrefix + key, value);

        public static int GetInt(string key, int defaultValue) => EditorPrefs.GetInt(KeyPrefix + key, defaultValue);
        public static void SetInt(string key, int value) => EditorPrefs.SetInt(KeyPrefix + key, value);

        public static float GetFloat(string key, float defaultValue) => EditorPrefs.GetFloat(KeyPrefix + key, defaultValue);
        public static void SetFloat(string key, float value) => EditorPrefs.SetFloat(KeyPrefix + key, value);

        public static bool GetBool(string key, bool defaultValue) => EditorPrefs.GetBool(KeyPrefix + key, defaultValue);
        public static void SetBool(string key, bool value) => EditorPrefs.SetBool(KeyPrefix + key, value);
    }
}
