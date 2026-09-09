using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpriteBatchSlicerWindow : EditorWindow
{
    private enum WindowTab
    {
        Slicer,
        Scanner
    }

    private class AssetListEntry
    {
        public Texture2D texture;
        public bool selected = true;
    }

    private class ScanEntry
    {
        public Texture2D texture;
        public bool selected = true;
        public bool wrongPpu;
        public bool wrongFilter;
        public bool wrongCompression;
        public bool notSliced;
    }

    private const string LastProfilePathPrefKey = "SpriteBatchSlicer.LastProfilePath";
    private const string PrefKeyAssetListWidth = "SpriteBatchSlicer.AssetListWidth";
    private const string PrefKeyPreviewHeight = "SpriteBatchSlicer.PreviewHeight";
    private const string PrefKeyPreviewMinimized = "SpriteBatchSlicer.PreviewMinimized";
    private const string PrefKeyPreviewZoom = "SpriteBatchSlicer.PreviewZoom";
    private const string PrefKeyShowOverlay = "SpriteBatchSlicer.ShowOverlay";
    private const string PrefKeyShowPivotMarkers = "SpriteBatchSlicer.ShowPivotMarkers";
    private const string PrefKeyShowNumbers = "SpriteBatchSlicer.ShowNumbers";

    private readonly List<AssetListEntry> assets = new List<AssetListEntry>();
    private readonly List<string> scanFolderPaths = new List<string>();
    private readonly List<ScanEntry> scanResults = new List<ScanEntry>();
    private readonly List<TextureImporterSnapshot> lastBatchSnapshots = new List<TextureImporterSnapshot>();

    private SpriteBatchSlicerProfile profile;
    private SerializedObject profileSerialized;
    private SpriteBatchSlicerProfile loadedProfileAsset;

    private WindowTab activeTab = WindowTab.Slicer;

    private Vector2 assetListScroll;
    private Vector2 settingsScroll;
    private Vector2 previewScroll;
    private Vector2 scanFolderScroll;
    private Vector2 scanResultsScroll;
    private Rect dropAreaRect;
    private Rect previewAreaRect;
    private Rect scanDropAreaRect;

    private const float SplitterThickness = 4f;
    private const float MinPanelWidth = 160f;
    private const float MinPreviewHeight = 120f;
    private const float MinimizedPreviewHeight = 22f;
    private static readonly Color SplitterColor = new Color(0f, 0f, 0f, 0.3f);

    private float assetListWidth = 280f;
    private float previewHeight = 300f;
    private bool draggingAssetSplitter;
    private bool draggingPreviewSplitter;

    private int previewIndex = -1;
    private float previewZoom = 1f;
    private bool showOverlay = true;
    private bool showPivotMarkers = true;
    private bool showNumbers = true;
    private bool previewMinimized;

    private Color32[] previewPixelsCache;
    private Texture2D previewPixelsCacheSource;

    [MenuItem("Tools/Sprite Batch Slicer")]
    public static void ShowWindow()
    {
        var window = GetWindow<SpriteBatchSlicerWindow>();
        window.titleContent = new GUIContent("Sprite Batch Slicer");
        window.minSize = new Vector2(760, 460);
    }

    private void OnEnable()
    {
        profile = ScriptableObject.CreateInstance<SpriteBatchSlicerProfile>();
        profile.hideFlags = HideFlags.DontSave;
        profileSerialized = new SerializedObject(profile);

        string lastPath = EditorPrefs.GetString(LastProfilePathPrefKey, string.Empty);
        if (!string.IsNullOrEmpty(lastPath))
        {
            SpriteBatchSlicerProfile lastAsset = AssetDatabase.LoadAssetAtPath<SpriteBatchSlicerProfile>(lastPath);
            if (lastAsset != null)
            {
                CopyProfileValues(lastAsset, profile);
                profileSerialized.Update();
                loadedProfileAsset = lastAsset;
            }
        }

        LoadWindowPrefs();
    }

    private void OnDisable()
    {
        SaveWindowPrefs();

        if (profile != null)
        {
            DestroyImmediate(profile);
        }
    }

    private void LoadWindowPrefs()
    {
        assetListWidth = EditorPrefs.GetFloat(PrefKeyAssetListWidth, assetListWidth);
        previewHeight = EditorPrefs.GetFloat(PrefKeyPreviewHeight, previewHeight);
        previewMinimized = EditorPrefs.GetBool(PrefKeyPreviewMinimized, previewMinimized);
        previewZoom = EditorPrefs.GetFloat(PrefKeyPreviewZoom, previewZoom);
        showOverlay = EditorPrefs.GetBool(PrefKeyShowOverlay, showOverlay);
        showPivotMarkers = EditorPrefs.GetBool(PrefKeyShowPivotMarkers, showPivotMarkers);
        showNumbers = EditorPrefs.GetBool(PrefKeyShowNumbers, showNumbers);
    }

    private void SaveWindowPrefs()
    {
        EditorPrefs.SetFloat(PrefKeyAssetListWidth, assetListWidth);
        EditorPrefs.SetFloat(PrefKeyPreviewHeight, previewHeight);
        EditorPrefs.SetBool(PrefKeyPreviewMinimized, previewMinimized);
        EditorPrefs.SetFloat(PrefKeyPreviewZoom, previewZoom);
        EditorPrefs.SetBool(PrefKeyShowOverlay, showOverlay);
        EditorPrefs.SetBool(PrefKeyShowPivotMarkers, showPivotMarkers);
        EditorPrefs.SetBool(PrefKeyShowNumbers, showNumbers);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Sprite Batch Slicer", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        activeTab = (WindowTab)GUILayout.Toolbar((int)activeTab, new[] { "Slicer", "Scanner" }, EditorStyles.toolbarButton, GUILayout.Width(160));
        EditorGUILayout.EndHorizontal();

        DrawProfileBar();

        if (activeTab == WindowTab.Scanner)
        {
            DrawScannerPanel();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawAssetListPanel();
        DrawVerticalSplitter(ref assetListWidth, ref draggingAssetSplitter, false);
        DrawSettingsPanel();
        EditorGUILayout.EndHorizontal();

        if (!previewMinimized)
        {
            DrawHorizontalSplitter(ref previewHeight, ref draggingPreviewSplitter);
        }
        DrawPreviewPanel();
    }

    private void DrawProfileBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Profile", GUILayout.Width(46));

        EditorGUI.BeginChangeCheck();
        var picked = (SpriteBatchSlicerProfile)EditorGUILayout.ObjectField(loadedProfileAsset, typeof(SpriteBatchSlicerProfile), false, GUILayout.Width(180));
        if (EditorGUI.EndChangeCheck() && picked != null)
        {
            LoadProfileAsset(picked);
        }

        if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            NewProfile();
        }
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            SaveProfile(false);
        }
        if (GUILayout.Button("Save As", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SaveProfile(true);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void LoadProfileAsset(SpriteBatchSlicerProfile asset)
    {
        CopyProfileValues(asset, profile);
        profileSerialized.Update();
        loadedProfileAsset = asset;
        EditorPrefs.SetString(LastProfilePathPrefKey, AssetDatabase.GetAssetPath(asset));
    }

    private void NewProfile()
    {
        SpriteBatchSlicerProfile defaults = ScriptableObject.CreateInstance<SpriteBatchSlicerProfile>();
        CopyProfileValues(defaults, profile);
        DestroyImmediate(defaults);
        profileSerialized.Update();
        loadedProfileAsset = null;
        EditorPrefs.DeleteKey(LastProfilePathPrefKey);
    }

    private void SaveProfile(bool saveAs)
    {
        string path;
        if (!saveAs && loadedProfileAsset != null)
        {
            path = AssetDatabase.GetAssetPath(loadedProfileAsset);
        }
        else
        {
            string defaultName = loadedProfileAsset != null ? loadedProfileAsset.name : "New Profile";
            path = EditorUtility.SaveFilePanelInProject("Save Sprite Batch Slicer Profile", defaultName, "asset", "Choose a location for the profile asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
        }

        SpriteBatchSlicerProfile targetAsset = AssetDatabase.LoadAssetAtPath<SpriteBatchSlicerProfile>(path);
        if (targetAsset == null)
        {
            targetAsset = ScriptableObject.CreateInstance<SpriteBatchSlicerProfile>();
            CopyProfileValues(profile, targetAsset);
            AssetDatabase.CreateAsset(targetAsset, path);
        }
        else
        {
            CopyProfileValues(profile, targetAsset);
            EditorUtility.SetDirty(targetAsset);
        }

        AssetDatabase.SaveAssets();
        loadedProfileAsset = targetAsset;
        EditorPrefs.SetString(LastProfilePathPrefKey, path);
    }

    private static void CopyProfileValues(SpriteBatchSlicerProfile source, SpriteBatchSlicerProfile destination)
    {
        destination.slicingMode = source.slicingMode;
        destination.cellWidth = source.cellWidth;
        destination.cellHeight = source.cellHeight;
        destination.paddingX = source.paddingX;
        destination.paddingY = source.paddingY;
        destination.offsetX = source.offsetX;
        destination.offsetY = source.offsetY;
        destination.pixelsPerUnit = source.pixelsPerUnit;
        destination.filterMode = source.filterMode;
        destination.compression = source.compression;
        destination.pivotPreset = source.pivotPreset;
        destination.customPivotX = source.customPivotX;
        destination.customPivotY = source.customPivotY;
        destination.namingPattern = source.namingPattern;
        destination.trimMode = source.trimMode;
        destination.alphaThreshold = source.alphaThreshold;
    }

    private void DrawVerticalSplitter(ref float width, ref bool dragging, bool invert)
    {
        Rect splitterRect = GUILayoutUtility.GetRect(SplitterThickness, 0f, GUILayout.Width(SplitterThickness), GUILayout.ExpandHeight(true));
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(splitterRect, SplitterColor);

        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && splitterRect.Contains(evt.mousePosition))
        {
            dragging = true;
            evt.Use();
        }
        else if (evt.type == EventType.MouseDrag && dragging)
        {
            float delta = invert ? -evt.delta.x : evt.delta.x;
            float maxWidth = Mathf.Max(MinPanelWidth, position.width * 0.6f);
            width = Mathf.Clamp(width + delta, MinPanelWidth, maxWidth);
            evt.Use();
            Repaint();
        }
        else if (evt.type == EventType.MouseUp && dragging)
        {
            dragging = false;
            evt.Use();
        }
    }

    private void DrawHorizontalSplitter(ref float height, ref bool dragging)
    {
        Rect splitterRect = GUILayoutUtility.GetRect(0f, SplitterThickness, GUILayout.ExpandWidth(true), GUILayout.Height(SplitterThickness));
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);
        EditorGUI.DrawRect(splitterRect, SplitterColor);

        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && splitterRect.Contains(evt.mousePosition))
        {
            dragging = true;
            evt.Use();
        }
        else if (evt.type == EventType.MouseDrag && dragging)
        {
            float maxHeight = Mathf.Max(MinPreviewHeight, position.height * 0.6f);
            height = Mathf.Clamp(height - evt.delta.y, MinPreviewHeight, maxHeight);
            evt.Use();
            Repaint();
        }
        else if (evt.type == EventType.MouseUp && dragging)
        {
            dragging = false;
            evt.Use();
        }
    }

    private void DrawAssetListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(assetListWidth));
        EditorGUILayout.LabelField("Asset List", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        assetListScroll = EditorGUILayout.BeginScrollView(assetListScroll);

        if (assets.Count == 0)
        {
            EditorGUILayout.HelpBox("Drag textures or folders here, or use Add Selected.", MessageType.Info);
        }

        for (int i = 0; i < assets.Count; i++)
        {
            AssetListEntry entry = assets[i];
            if (entry.texture == null)
            {
                continue;
            }

            Rect rowRect = EditorGUILayout.BeginHorizontal();
            if (i == previewIndex)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.9f, 0.35f));
            }

            entry.selected = EditorGUILayout.ToggleLeft(GUIContent.none, entry.selected, GUILayout.Width(18));
            if (GUILayout.Button($"{entry.texture.name} ({entry.texture.width}x{entry.texture.height})", EditorStyles.label))
            {
                previewIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.Repaint)
        {
            dropAreaRect = GUILayoutUtility.GetLastRect();
        }
        HandleDragAndDrop(dropAreaRect);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected"))
        {
            AddFromSelection();
        }
        if (GUILayout.Button("Remove Checked"))
        {
            assets.RemoveAll(e => e.selected);
            if (previewIndex >= assets.Count)
            {
                previewIndex = assets.Count - 1;
            }
        }
        if (GUILayout.Button("Clear All"))
        {
            assets.Clear();
            previewIndex = -1;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(assets.TrueForAll(e => !e.selected));
        if (GUILayout.Button("Apply to Selected", GUILayout.Height(30)))
        {
            ApplyToSelected();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(lastBatchSnapshots.Count == 0);
        if (GUILayout.Button("Revert Last Apply"))
        {
            RevertLastApply();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
        {
            return;
        }

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (string path in DragAndDrop.paths)
            {
                AddFromPath(path);
            }
            evt.Use();
            Repaint();
        }
    }

    private void AddFromSelection()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                AddFromPath(path);
            }
        }
        Repaint();
    }

    private void AddFromPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
            foreach (string guid in guids)
            {
                AddTextureAtPath(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
        else
        {
            AddTextureAtPath(path);
        }
    }

    private void AddTextureAtPath(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            return;
        }
        if (assets.Exists(e => e.texture == tex))
        {
            return;
        }
        assets.Add(new AssetListEntry { texture = tex, selected = true });
        if (previewIndex == -1)
        {
            previewIndex = assets.Count - 1;
        }
    }

    private void DrawPreviewPanel()
    {
        if (previewMinimized)
        {
            DrawMinimizedPreviewPanel();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Minimize", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            previewMinimized = true;
            previewAreaRect = default;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Zoom", GUILayout.Width(36));
        previewZoom = GUILayout.HorizontalSlider(previewZoom, 0.25f, 8f, GUILayout.Width(100));
        GUILayout.Label(previewZoom.ToString("0.00") + "x", GUILayout.Width(38));
        if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(32)))
        {
            FitPreviewZoom();
        }
        showOverlay = GUILayout.Toggle(showOverlay, "Overlay", EditorStyles.toolbarButton);
        showPivotMarkers = GUILayout.Toggle(showPivotMarkers, "Pivots", EditorStyles.toolbarButton);
        showNumbers = GUILayout.Toggle(showNumbers, "Numbers", EditorStyles.toolbarButton);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        Texture2D tex = (previewIndex >= 0 && previewIndex < assets.Count) ? assets[previewIndex].texture : null;

        HandlePreviewZoomScroll(previewAreaRect);

        previewScroll = EditorGUILayout.BeginScrollView(previewScroll, "box", GUILayout.Height(previewHeight), GUILayout.ExpandWidth(true));

        if (tex == null)
        {
            EditorGUILayout.HelpBox("Select a texture in the Asset List to preview slicing.", MessageType.Info);
        }
        else
        {
            float w = tex.width * previewZoom;
            float h = tex.height * previewZoom;
            Rect imageRect = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(imageRect, tex, ScaleMode.StretchToFill);

            if (showOverlay)
            {
                DrawSliceOverlay(imageRect, tex);
            }
        }

        EditorGUILayout.EndScrollView();

        if (Event.current.type == EventType.Repaint)
        {
            previewAreaRect = GUILayoutUtility.GetLastRect();
        }
    }

    private void DrawMinimizedPreviewPanel()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(MinimizedPreviewHeight));
        if (GUILayout.Button("▲ Preview", GUILayout.ExpandWidth(true), GUILayout.Height(MinimizedPreviewHeight)))
        {
            previewMinimized = false;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void HandlePreviewZoomScroll(Rect area)
    {
        Event evt = Event.current;
        if (evt.type != EventType.ScrollWheel || !area.Contains(evt.mousePosition))
        {
            return;
        }

        float zoomStep = 1f - evt.delta.y * 0.05f;
        previewZoom = Mathf.Clamp(previewZoom * zoomStep, 0.25f, 8f);
        evt.Use();
        Repaint();
    }

    private void FitPreviewZoom()
    {
        Texture2D tex = (previewIndex >= 0 && previewIndex < assets.Count) ? assets[previewIndex].texture : null;
        if (tex == null)
        {
            return;
        }
        previewZoom = Mathf.Clamp(320f / Mathf.Max(tex.width, tex.height), 0.25f, 8f);
    }

    private void DrawSliceOverlay(Rect imageRect, Texture2D tex)
    {
        bool needsPixels = profile.trimMode != TrimMode.Disabled || profile.slicingMode == SlicingMode.DetectIndividualSprites;
        Color32[] pixels = needsPixels ? GetPreviewPixels(tex) : null;
        List<SpriteRectData> rects = SpriteSlicerCore.Slice(tex.width, tex.height, tex.name, profile, pixels);

        for (int i = 0; i < rects.Count; i++)
        {
            SpriteRectData r = rects[i];
            Rect screenRect = TextureRectToScreenRect(r.rect, imageRect, tex.height, previewZoom);
            DrawRectOutline(screenRect, new Color(0.1f, 1f, 0.3f));

            if (showNumbers)
            {
                DrawNumberLabel(screenRect, i.ToString());
            }

            if (showPivotMarkers)
            {
                float px = screenRect.x + r.pivot.x * screenRect.width;
                float py = screenRect.y + (1f - r.pivot.y) * screenRect.height;
                DrawCross(new Vector2(px, py), 4f, Color.red);
            }
        }
    }

    private Color32[] GetPreviewPixels(Texture2D tex)
    {
        if (previewPixelsCacheSource != tex)
        {
            try
            {
                previewPixelsCache = TextureUtils.ReadPixels(tex);
            }
            catch (System.Exception)
            {
                previewPixelsCache = null;
            }
            previewPixelsCacheSource = tex;
        }
        return previewPixelsCache;
    }

    private static Rect TextureRectToScreenRect(Rect texRect, Rect imageRect, int textureHeight, float zoom)
    {
        float x = imageRect.x + texRect.x * zoom;
        float yTop = imageRect.y + (textureHeight - texRect.y - texRect.height) * zoom;
        return new Rect(x, yTop, texRect.width * zoom, texRect.height * zoom);
    }

    private static void DrawRectOutline(Rect r, Color color, float thickness = 1.5f)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, thickness), color);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), color);
        EditorGUI.DrawRect(new Rect(r.x, r.y, thickness, r.height), color);
        EditorGUI.DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), color);
    }

    private static void DrawCross(Vector2 center, float size, Color color)
    {
        EditorGUI.DrawRect(new Rect(center.x - size, center.y - 1f, size * 2f, 2f), color);
        EditorGUI.DrawRect(new Rect(center.x - 1f, center.y - size, 2f, size * 2f), color);
    }
    private static void DrawNumberLabel(Rect screenRect, string text)
    {
        var style = new GUIStyle
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = Color.white;

        Rect labelRect = new Rect(screenRect.x + 1f, screenRect.y, screenRect.width, 14f);
        float textWidth = Mathf.Min(screenRect.width, style.CalcSize(new GUIContent(text)).x + 4f);
        EditorGUI.DrawRect(new Rect(labelRect.x, labelRect.y, textWidth, 14f), new Color(0f, 0f, 0f, 0.65f));

        Color previousColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(labelRect, text, style);
        GUI.color = previousColor;
    }

    private void DrawSettingsPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
        profileSerialized.Update();

        //EditorGUILayout.LabelField("Slicing Mode", EditorStyles.boldLabel);
        SerializedProperty slicingModeProp = profileSerialized.FindProperty("slicingMode");
        EditorGUILayout.PropertyField(slicingModeProp);
        SlicingMode slicingMode = (SlicingMode)slicingModeProp.enumValueIndex;

        if (slicingMode == SlicingMode.AutoDetectGrid)
        {
            EditorGUILayout.HelpBox("Detects cell size, spacing and offset from the previewed texture's transparent gutters. Adjust the results below if needed.", MessageType.None);
            bool hasPreview = previewIndex >= 0 && previewIndex < assets.Count;
            EditorGUI.BeginDisabledGroup(!hasPreview);
            if (GUILayout.Button("Detect Grid From Preview"))
            {
                RunAutoDetectGrid();
            }
            EditorGUI.EndDisabledGroup();
        }
        else if (slicingMode == SlicingMode.DetectIndividualSprites)
        {
            EditorGUILayout.HelpBox("Sprites are detected automatically from separate opaque regions. Use the alpha threshold below if regions merge or split incorrectly.", MessageType.None);
        }

        EditorGUILayout.Space();

        if (slicingMode != SlicingMode.DetectIndividualSprites)
        {
            //EditorGUILayout.LabelField("Slice Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("cellWidth"));
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("cellHeight"));
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("paddingX"));
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("paddingY"));
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("offsetX"));
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("offsetY"));

            EditorGUILayout.Space();
        }
        //EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(profileSerialized.FindProperty("pixelsPerUnit"));
        EditorGUILayout.PropertyField(profileSerialized.FindProperty("filterMode"));
        EditorGUILayout.PropertyField(profileSerialized.FindProperty("compression"));

        EditorGUILayout.Space();
        //EditorGUILayout.LabelField("Pivot", EditorStyles.boldLabel);
        SerializedProperty pivotPresetProp = profileSerialized.FindProperty("pivotPreset");
        EditorGUILayout.PropertyField(pivotPresetProp);
        if ((PivotPreset)pivotPresetProp.enumValueIndex == PivotPreset.Custom)
        {
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("customPivotX"));
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("customPivotY"));
        }

        EditorGUILayout.Space();
        //EditorGUILayout.LabelField("Naming", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(profileSerialized.FindProperty("namingPattern"));
        EditorGUILayout.HelpBox("Tokens: {name} {index} {row} {col}", MessageType.None);

        EditorGUILayout.Space();
        //EditorGUILayout.LabelField("Trimming", EditorStyles.boldLabel);
        SerializedProperty trimModeProp = profileSerialized.FindProperty("trimMode");
        bool trimApplicable = slicingMode != SlicingMode.DetectIndividualSprites;
        if (trimApplicable)
        {
            EditorGUILayout.PropertyField(trimModeProp);
        }
        bool needsAlphaThreshold = slicingMode == SlicingMode.DetectIndividualSprites
            || (trimApplicable && (TrimMode)trimModeProp.enumValueIndex != TrimMode.Disabled);
        if (needsAlphaThreshold)
        {
            EditorGUILayout.PropertyField(profileSerialized.FindProperty("alphaThreshold"));
        }

        profileSerialized.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void RunAutoDetectGrid()
    {
        Texture2D tex = (previewIndex >= 0 && previewIndex < assets.Count) ? assets[previewIndex].texture : null;
        if (tex == null)
        {
            return;
        }

        Color32[] pixels = GetPreviewPixels(tex);
        DetectedGrid detected = SpriteSlicerCore.DetectGrid(tex.width, tex.height, pixels, profile.alphaThreshold);
        if (!detected.success)
        {
            EditorUtility.DisplayDialog("Sprite Batch Slicer", "Could not detect a grid - the texture may be fully transparent or have no visible gutters.", "OK");
            return;
        }

        profile.cellWidth = detected.cellWidth;
        profile.cellHeight = detected.cellHeight;
        profile.paddingX = detected.paddingX;
        profile.paddingY = detected.paddingY;
        profile.offsetX = detected.offsetX;
        profile.offsetY = detected.offsetY;
        profileSerialized.Update();
    }

    private void DrawScannerPanel()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(assetListWidth));
        EditorGUILayout.LabelField("Folders To Scan", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        scanFolderScroll = EditorGUILayout.BeginScrollView(scanFolderScroll);

        if (scanFolderPaths.Count == 0)
        {
            EditorGUILayout.HelpBox("Drag folders here, or use Add Selected Folders.", MessageType.Info);
        }

        int removeIndex = -1;
        for (int i = 0; i < scanFolderPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(scanFolderPaths[i]);
            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0)
        {
            scanFolderPaths.RemoveAt(removeIndex);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.Repaint)
        {
            scanDropAreaRect = GUILayoutUtility.GetLastRect();
        }
        HandleScanFolderDragAndDrop(scanDropAreaRect);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected Folders"))
        {
            AddSelectedFolders();
        }
        if (GUILayout.Button("Clear"))
        {
            scanFolderPaths.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(scanFolderPaths.Count == 0);
        if (GUILayout.Button("Scan", GUILayout.Height(28)))
        {
            RunScan();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();

        DrawVerticalSplitter(ref assetListWidth, ref draggingAssetSplitter, false);

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField($"Results ({scanResults.Count} texture(s) with issues)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Compares scanned textures against the profile above. Fixing re-slices textures using the current profile's slice settings.", MessageType.None);

        scanResultsScroll = EditorGUILayout.BeginScrollView(scanResultsScroll, "box", GUILayout.ExpandHeight(true));
        for (int i = 0; i < scanResults.Count; i++)
        {
            ScanEntry entry = scanResults[i];
            if (entry.texture == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            entry.selected = EditorGUILayout.ToggleLeft(GUIContent.none, entry.selected, GUILayout.Width(18));
            EditorGUILayout.LabelField(entry.texture.name, GUILayout.Width(160));
            EditorGUILayout.LabelField(BuildIssueSummary(entry));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(scanResults.Count == 0);
        if (GUILayout.Button("Fix Selected"))
        {
            FixScanResults(true);
        }
        if (GUILayout.Button("Fix All"))
        {
            FixScanResults(false);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void HandleScanFolderDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
        {
            return;
        }

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (string path in DragAndDrop.paths)
            {
                if (AssetDatabase.IsValidFolder(path) && !scanFolderPaths.Contains(path))
                {
                    scanFolderPaths.Add(path);
                }
            }
            evt.Use();
            Repaint();
        }
    }

    private void AddSelectedFolders()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) && !scanFolderPaths.Contains(path))
            {
                scanFolderPaths.Add(path);
            }
        }
        Repaint();
    }

    private void RunScan()
    {
        scanResults.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", scanFolderPaths.ToArray());
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool notSliced = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple;
            bool wrongPpu = !Mathf.Approximately(importer.spritePixelsPerUnit, profile.pixelsPerUnit);
            bool wrongFilter = importer.filterMode != profile.filterMode;
            bool wrongCompression = importer.textureCompression != profile.compression;

            if (!notSliced && !wrongPpu && !wrongFilter && !wrongCompression)
            {
                continue;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                continue;
            }

            scanResults.Add(new ScanEntry
            {
                texture = tex,
                selected = true,
                wrongPpu = wrongPpu,
                wrongFilter = wrongFilter,
                wrongCompression = wrongCompression,
                notSliced = notSliced
            });
        }

        Repaint();
    }

    private static string BuildIssueSummary(ScanEntry entry)
    {
        var parts = new List<string>();
        if (entry.notSliced)
        {
            parts.Add("Not Sliced");
        }
        if (entry.wrongPpu)
        {
            parts.Add("Wrong PPU");
        }
        if (entry.wrongFilter)
        {
            parts.Add("Wrong Filter Mode");
        }
        if (entry.wrongCompression)
        {
            parts.Add("Wrong Compression");
        }
        return string.Join(", ", parts);
    }

    private void FixScanResults(bool onlySelected)
    {
        List<ScanEntry> targets = scanResults.FindAll(e => !onlySelected || e.selected);
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Sprite Batch Slicer", "No scan results to fix.", "OK");
            return;
        }

        int successCount = 0;
        int failCount = 0;
        lastBatchSnapshots.Clear();

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                ScanEntry entry = targets[i];
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Sprite Batch Slicer",
                    $"Fixing {entry.texture.name} ({i + 1}/{targets.Count})",
                    (float)i / targets.Count);
                if (cancel)
                {
                    break;
                }

                bool ok = ApplyProfileToTextureWithSnapshot(entry.texture, out int spriteCount, out string error);
                if (ok)
                {
                    successCount++;
                    Log($"✔ {entry.texture.name} ({spriteCount} sprites)");
                }
                else
                {
                    failCount++;
                    Log($"⚠ {entry.texture.name}: {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"Sprite Batch Slicer: Fixed {successCount}, failed {failCount}.");

        RunScan();
    }

    private bool ApplyProfileToTexture(Texture2D texture, out int spriteCount, out string error)
    {
        spriteCount = 0;
        try
        {
            string path = AssetDatabase.GetAssetPath(texture);
            bool needsPixels = profile.trimMode != TrimMode.Disabled || profile.slicingMode == SlicingMode.DetectIndividualSprites;
            Color32[] pixels = needsPixels ? TextureUtils.ReadPixels(texture) : null;
            List<SpriteRectData> rects = SpriteSlicerCore.Slice(texture.width, texture.height, texture.name, profile, pixels);
            spriteCount = rects.Count;
            return SpriteImportApplier.TryApply(path, rects, profile, out error);
        }
        catch (System.Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    // Wraps ApplyProfileToTexture with a pre-apply snapshot so a batch can be undone via
    // Revert Last Apply, since TextureImporter reimports bypass Unity's normal Undo stack.
    private bool ApplyProfileToTextureWithSnapshot(Texture2D texture, out int spriteCount, out string error)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporterSnapshot snapshot = SpriteImportApplier.CaptureSnapshot(path);

        bool ok = ApplyProfileToTexture(texture, out spriteCount, out error);
        if (ok && snapshot != null)
        {
            lastBatchSnapshots.Add(snapshot);
        }
        return ok;
    }

    private void RevertLastApply()
    {
        if (lastBatchSnapshots.Count == 0)
        {
            EditorUtility.DisplayDialog("Sprite Batch Slicer", "There is nothing to revert.", "OK");
            return;
        }

        int successCount = 0;
        int failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < lastBatchSnapshots.Count; i++)
            {
                TextureImporterSnapshot snapshot = lastBatchSnapshots[i];
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Sprite Batch Slicer",
                    $"Reverting {i + 1}/{lastBatchSnapshots.Count}",
                    (float)i / lastBatchSnapshots.Count);
                if (cancel)
                {
                    break;
                }

                bool ok = SpriteImportApplier.RestoreSnapshot(snapshot, out string error);
                if (ok)
                {
                    successCount++;
                    Log($"✔ Reverted {snapshot.assetPath}");
                }
                else
                {
                    failCount++;
                    Log($"⚠ Revert failed for {snapshot.assetPath}: {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        lastBatchSnapshots.Clear();
        Debug.Log($"Sprite Batch Slicer: Reverted {successCount}, failed {failCount}.");
    }

    private void ApplyToSelected()
    {
        List<AssetListEntry> targets = assets.FindAll(e => e.selected && e.texture != null);
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Sprite Batch Slicer", "No textures checked in the asset list.", "OK");
            return;
        }

        int successCount = 0;
        int failCount = 0;
        lastBatchSnapshots.Clear();

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                AssetListEntry entry = targets[i];
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Sprite Batch Slicer",
                    $"Processing {entry.texture.name} ({i + 1}/{targets.Count})",
                    (float)i / targets.Count);
                if (cancel)
                {
                    break;
                }

                bool ok = ApplyProfileToTextureWithSnapshot(entry.texture, out int spriteCount, out string error);

                if (ok)
                {
                    successCount++;
                    Log($"✔ {entry.texture.name} ({spriteCount} sprites)");
                }
                else
                {
                    failCount++;
                    Log($"⚠ {entry.texture.name}: {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"Sprite Batch Slicer: {successCount} succeeded, {failCount} failed.");
    }

    private void Log(string message)
    {
        if (message.StartsWith("✔"))
        {
            Debug.Log("Sprite Batch Slicer: " + message);
        }
        else
        {
            Debug.LogWarning("Sprite Batch Slicer: " + message);
        }
    }
}
