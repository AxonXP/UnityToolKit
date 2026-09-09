using UnityEditor;
using UnityEngine;

public enum PivotPreset
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    BottomCenter,
    Custom
}

public enum TrimMode
{
    Disabled,
    TrimSprites,
    TrimEntireTexture
}

public enum SlicingMode
{
    UniformGrid,
    AutoDetectGrid,
    DetectIndividualSprites
}

[CreateAssetMenu(fileName = "SpriteBatchSlicerProfile", menuName = "ScriptableObjects/Sprite Batch Slicer Profile")]
public class SpriteBatchSlicerProfile : ScriptableObject
{
    [Header("Slicing Mode")]
    public SlicingMode slicingMode = SlicingMode.UniformGrid;

    [Header("Slice Settings")]
    public int cellWidth = 16;
    public int cellHeight = 16;
    public int paddingX = 0;
    public int paddingY = 0;
    public int offsetX = 0;
    public int offsetY = 0;

    [Header("Import Settings")]
    public float pixelsPerUnit = 16f;
    public FilterMode filterMode = FilterMode.Point;
    public TextureImporterCompression compression = TextureImporterCompression.Uncompressed;

    [Header("Pivot")]
    public PivotPreset pivotPreset = PivotPreset.BottomCenter;
    [Range(0f, 1f)] public float customPivotX = 0.5f;
    [Range(0f, 1f)] public float customPivotY = 0.5f;

    [Header("Naming")]
    public string namingPattern = "{name}_{index}";

    [Header("Trimming")]
    public TrimMode trimMode = TrimMode.Disabled;
    [Range(1, 254)] public byte alphaThreshold = 8;

    public Vector2 GetPivot()
    {
        switch (pivotPreset)
        {
            case PivotPreset.Center: return new Vector2(0.5f, 0.5f);
            case PivotPreset.Top: return new Vector2(0.5f, 1f);
            case PivotPreset.Bottom: return new Vector2(0.5f, 0f);
            case PivotPreset.Left: return new Vector2(0f, 0.5f);
            case PivotPreset.Right: return new Vector2(1f, 0.5f);
            case PivotPreset.TopLeft: return new Vector2(0f, 1f);
            case PivotPreset.TopRight: return new Vector2(1f, 1f);
            case PivotPreset.BottomLeft: return new Vector2(0f, 0f);
            case PivotPreset.BottomRight: return new Vector2(1f, 0f);
            case PivotPreset.BottomCenter: return new Vector2(0.5f, 0f);
            case PivotPreset.Custom: return new Vector2(customPivotX, customPivotY);
            default: return new Vector2(0.5f, 0.5f);
        }
    }
}
