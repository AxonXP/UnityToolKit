using System.Collections.Generic;
using UnityEngine;

public struct SpriteRectData
{
    public string name;
    public Rect rect;
    public Vector2 pivot;
}

public struct DetectedGrid
{
    public bool success;
    public int cellWidth;
    public int cellHeight;
    public int paddingX;
    public int paddingY;
    public int offsetX;
    public int offsetY;
}

public static class SpriteSlicerCore
{
    public static List<SpriteRectData> Slice(int textureWidth, int textureHeight, string baseName, SpriteBatchSlicerProfile settings, Color32[] pixels = null)
    {
        if (settings.slicingMode == SlicingMode.DetectIndividualSprites)
        {
            return DetectIndividualSprites(textureWidth, textureHeight, baseName, settings, pixels);
        }

        return SliceUniformGrid(textureWidth, textureHeight, baseName, settings, pixels);
    }
    public static DetectedGrid DetectGrid(int textureWidth, int textureHeight, Color32[] pixels, byte alphaThreshold)
    {
        if (pixels == null)
        {
            return default;
        }

        var colHasContent = new bool[textureWidth];
        var rowHasContent = new bool[textureHeight];

        for (int y = 0; y < textureHeight; y++)
        {
            int rowStart = y * textureWidth;
            for (int x = 0; x < textureWidth; x++)
            {
                if (pixels[rowStart + x].a > alphaThreshold)
                {
                    colHasContent[x] = true;
                    rowHasContent[y] = true;
                }
            }
        }

        List<(int start, int length)> colRuns = FindRuns(colHasContent);
        List<(int start, int length)> rowRuns = FindRuns(rowHasContent);

        if (colRuns.Count == 0 || rowRuns.Count == 0)
        {
            return default;
        }

        (int start, int length) topRowRun = rowRuns[rowRuns.Count - 1];

        return new DetectedGrid
        {
            success = true,
            cellWidth = Mathf.Max(1, Median(colRuns.ConvertAll(r => r.length))),
            cellHeight = Mathf.Max(1, Median(rowRuns.ConvertAll(r => r.length))),
            paddingX = Mathf.Max(0, MedianGap(colRuns)),
            paddingY = Mathf.Max(0, MedianGap(rowRuns)),
            offsetX = Mathf.Max(0, colRuns[0].start),
            offsetY = Mathf.Max(0, textureHeight - (topRowRun.start + topRowRun.length))
        };
    }
    public static List<SpriteRectData> DetectIndividualSprites(int textureWidth, int textureHeight, string baseName, SpriteBatchSlicerProfile settings, Color32[] pixels)
    {
        var result = new List<SpriteRectData>();
        if (pixels == null)
        {
            return result;
        }

        byte alphaThreshold = settings.alphaThreshold;
        var visited = new bool[textureWidth * textureHeight];
        var boxes = new List<RectInt>();
        var stack = new Stack<int>();

        for (int y = 0; y < textureHeight; y++)
        {
            int rowStart = y * textureWidth;
            for (int x = 0; x < textureWidth; x++)
            {
                int idx = rowStart + x;
                if (visited[idx] || pixels[idx].a <= alphaThreshold)
                {
                    continue;
                }

                visited[idx] = true;
                stack.Push(idx);

                int minX = x, maxX = x, minY = y, maxY = y;
                while (stack.Count > 0)
                {
                    int cur = stack.Pop();
                    int cy = cur / textureWidth;
                    int cx = cur - cy * textureWidth;

                    if (cx < minX) minX = cx;
                    if (cx > maxX) maxX = cx;
                    if (cy < minY) minY = cy;
                    if (cy > maxY) maxY = cy;

                    TryPushOpaque(stack, visited, pixels, textureWidth, textureHeight, cx - 1, cy, alphaThreshold);
                    TryPushOpaque(stack, visited, pixels, textureWidth, textureHeight, cx + 1, cy, alphaThreshold);
                    TryPushOpaque(stack, visited, pixels, textureWidth, textureHeight, cx, cy - 1, alphaThreshold);
                    TryPushOpaque(stack, visited, pixels, textureWidth, textureHeight, cx, cy + 1, alphaThreshold);
                }

                int w = maxX - minX + 1;
                int h = maxY - minY + 1;
                if (w * h > 1)
                {
                    boxes.Add(new RectInt(minX, minY, w, h));
                }
            }
        }

        boxes.Sort((a, b) =>
        {
            int rowA = textureHeight - (a.y + a.height);
            int rowB = textureHeight - (b.y + b.height);
            int rowCompare = rowA.CompareTo(rowB);
            return rowCompare != 0 ? rowCompare : a.x.CompareTo(b.x);
        });

        Vector2 pivot = settings.GetPivot();
        string pattern = string.IsNullOrEmpty(settings.namingPattern) ? "{name}_{index}" : settings.namingPattern;

        for (int i = 0; i < boxes.Count; i++)
        {
            RectInt box = boxes[i];
            result.Add(new SpriteRectData
            {
                name = BuildName(pattern, baseName, i, 0, i),
                rect = new Rect(box.x, box.y, box.width, box.height),
                pivot = pivot
            });
        }

        return result;
    }

    private static void TryPushOpaque(Stack<int> stack, bool[] visited, Color32[] pixels, int width, int height, int x, int y, byte alphaThreshold)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int idx = y * width + x;
        if (visited[idx] || pixels[idx].a <= alphaThreshold)
        {
            return;
        }

        visited[idx] = true;
        stack.Push(idx);
    }

    private static List<(int start, int length)> FindRuns(bool[] flags)
    {
        var runs = new List<(int, int)>();
        int i = 0;
        while (i < flags.Length)
        {
            if (flags[i])
            {
                int start = i;
                while (i < flags.Length && flags[i])
                {
                    i++;
                }
                runs.Add((start, i - start));
            }
            else
            {
                i++;
            }
        }
        return runs;
    }

    private static int MedianGap(List<(int start, int length)> runs)
    {
        var gaps = new List<int>();
        for (int k = 0; k < runs.Count - 1; k++)
        {
            int gap = runs[k + 1].start - (runs[k].start + runs[k].length);
            if (gap > 0)
            {
                gaps.Add(gap);
            }
        }
        return Median(gaps);
    }

    private static int Median(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }
        var sorted = new List<int>(values);
        sorted.Sort();
        return sorted[sorted.Count / 2];
    }

    public static List<SpriteRectData> SliceUniformGrid(int textureWidth, int textureHeight, string baseName, SpriteBatchSlicerProfile settings, Color32[] pixels = null)
    {
        var result = new List<SpriteRectData>();

        int cellWidth = Mathf.Max(1, settings.cellWidth);
        int cellHeight = Mathf.Max(1, settings.cellHeight);
        int strideX = cellWidth + Mathf.Max(0, settings.paddingX);
        int strideY = cellHeight + Mathf.Max(0, settings.paddingY);
        int offsetX = Mathf.Max(0, settings.offsetX);
        int offsetY = Mathf.Max(0, settings.offsetY);
        Vector2 pivot = settings.GetPivot();
        string pattern = string.IsNullOrEmpty(settings.namingPattern) ? "{name}_{index}" : settings.namingPattern;

        Rect? overallBounds = null;
        if (settings.trimMode == TrimMode.TrimEntireTexture && pixels != null)
        {
            if (TryGetOpaqueBounds(pixels, textureWidth, new Rect(0, 0, textureWidth, textureHeight), settings.alphaThreshold, out Rect bounds))
            {
                overallBounds = bounds;
            }
        }

        int index = 0;
        int row = 0;
        for (int topY = offsetY; topY + cellHeight <= textureHeight; topY += strideY, row++)
        {
            int col = 0;
            for (int x = offsetX; x + cellWidth <= textureWidth; x += strideX, col++)
            {
                float y = textureHeight - topY - cellHeight;
                Rect rect = new Rect(x, y, cellWidth, cellHeight);

                if (settings.trimMode == TrimMode.TrimSprites && pixels != null)
                {
                    if (!TryGetOpaqueBounds(pixels, textureWidth, rect, settings.alphaThreshold, out rect))
                    {
                        continue;
                    }
                }
                else if (overallBounds.HasValue)
                {
                    if (!rect.Overlaps(overallBounds.Value))
                    {
                        continue;
                    }
                    rect = ClipRect(rect, overallBounds.Value);
                }

                result.Add(new SpriteRectData
                {
                    name = BuildName(pattern, baseName, index, row, col),
                    rect = rect,
                    pivot = pivot
                });
                index++;
            }
        }

        return result;
    }

    public static string BuildName(string pattern, string baseName, int index, int row, int col)
    {
        return pattern
            .Replace("{name}", baseName)
            .Replace("{index}", index.ToString())
            .Replace("{row}", row.ToString())
            .Replace("{col}", col.ToString());
    }

    private static Rect ClipRect(Rect rect, Rect bounds)
    {
        float x = Mathf.Max(rect.x, bounds.x);
        float y = Mathf.Max(rect.y, bounds.y);
        float xMax = Mathf.Min(rect.xMax, bounds.xMax);
        float yMax = Mathf.Min(rect.yMax, bounds.yMax);
        return new Rect(x, y, xMax - x, yMax - y);
    }

    private static bool TryGetOpaqueBounds(Color32[] pixels, int textureWidth, Rect cell, byte alphaThreshold, out Rect bounds)
    {
        int startX = Mathf.RoundToInt(cell.x);
        int startY = Mathf.RoundToInt(cell.y);
        int w = Mathf.RoundToInt(cell.width);
        int h = Mathf.RoundToInt(cell.height);

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

        for (int dy = 0; dy < h; dy++)
        {
            int py = startY + dy;
            int rowStart = py * textureWidth;
            for (int dx = 0; dx < w; dx++)
            {
                int px = startX + dx;
                if (pixels[rowStart + px].a > alphaThreshold)
                {
                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;
                }
            }
        }

        if (maxX < minX)
        {
            bounds = default;
            return false;
        }

        bounds = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }
}
