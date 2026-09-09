using UnityEngine;

public static class TextureUtils
{
    public static Color32[] ReadPixels(Texture2D source)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        Graphics.Blit(source, temp);
        RenderTexture.active = temp;

        var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temp);

        Color32[] pixels = readable.GetPixels32();
        Object.DestroyImmediate(readable);
        return pixels;
    }
}
