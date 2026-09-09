using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class TextureImporterSnapshot
{
    public string assetPath;
    public TextureImporterType textureType;
    public SpriteImportMode spriteImportMode;
    public float spritePixelsPerUnit;
    public FilterMode filterMode;
    public TextureImporterCompression textureCompression;
    public SpriteRect[] spriteRects;
}

public static class SpriteImportApplier
{
    public static TextureImporterSnapshot CaptureSnapshot(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        var snapshot = new TextureImporterSnapshot
        {
            assetPath = assetPath,
            textureType = importer.textureType,
            spriteImportMode = importer.spriteImportMode,
            spritePixelsPerUnit = importer.spritePixelsPerUnit,
            filterMode = importer.filterMode,
            textureCompression = importer.textureCompression
        };

        try
        {
            if (importer.textureType == TextureImporterType.Sprite)
            {
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();
                snapshot.spriteRects = dataProvider.GetSpriteRects();
            }
        }
        catch
        {
            snapshot.spriteRects = null;
        }

        return snapshot;
    }

    public static bool RestoreSnapshot(TextureImporterSnapshot snapshot, out string error)
    {
        error = null;
        if (snapshot == null)
        {
            error = "No snapshot available.";
            return false;
        }

        try
        {
            var importer = AssetImporter.GetAtPath(snapshot.assetPath) as TextureImporter;
            if (importer == null)
            {
                error = "Texture no longer exists at " + snapshot.assetPath;
                return false;
            }

            importer.textureType = snapshot.textureType;
            importer.spriteImportMode = snapshot.spriteImportMode;
            importer.spritePixelsPerUnit = snapshot.spritePixelsPerUnit;
            importer.filterMode = snapshot.filterMode;
            importer.textureCompression = snapshot.textureCompression;

            if (snapshot.spriteRects != null && snapshot.textureType == TextureImporterType.Sprite)
            {
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();
                dataProvider.SetSpriteRects(snapshot.spriteRects);
                dataProvider.Apply();
            }

            importer.SaveAndReimport();
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    public static bool TryApply(string assetPath, List<SpriteRectData> rects, SpriteBatchSlicerProfile settings, out string error)
    {
        error = null;

        if (rects == null || rects.Count == 0)
        {
            error = "No sprites generated - check cell size/offset against texture dimensions.";
            return false;
        }

        try
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                error = "Not a texture asset (no TextureImporter found).";
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = settings.pixelsPerUnit;
            importer.filterMode = settings.filterMode;
            importer.textureCompression = settings.compression;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new SpriteRect[rects.Count];
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                spriteRects[i] = new SpriteRect
                {
                    name = r.name,
                    rect = r.rect,
                    pivot = r.pivot,
                    alignment = SpriteAlignment.Custom,
                    spriteID = GUID.Generate()
                };
            }

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();

            importer.SaveAndReimport();
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }
}
