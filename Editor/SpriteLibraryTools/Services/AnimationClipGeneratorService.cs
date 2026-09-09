using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SpriteLibraryTools
{
    // Converts Sprite Library categories into sprite-swap AnimationClips by keying a single
    // m_SpriteHash float curve on the SpriteResolver component, exactly as Unity's own Sprite
    // Resolver inspector does. SpriteLibrary.GetHashForCategoryAndEntry and
    // SpriteLibraryUtility.GetStringHash are both internal to the 2D Animation package, so the
    // hash (Animator.StringToHash("Category_Label") masked to 30 bits) is reimplemented here -
    // confirmed against the package's own SpriteLibraryUtility.cs source, not guessed.
    public static class AnimationClipGeneratorService
    {
        private const float ClipSampleRate = 60f;

        public static List<AnimationGenerationResult> GenerateBatch(SpriteLibraryAsset library, IEnumerable<CategoryModel> categories, AnimationGeneratorOptions options)
        {
            string libraryPath = AssetDatabase.GetAssetPath(library);
            List<CategoryModel> categoryList = categories.ToList();
            var results = new List<AnimationGenerationResult>();

            try
            {
                for (int i = 0; i < categoryList.Count; i++)
                {
                    CategoryModel category = categoryList[i];
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Generating Animation Clips",
                        $"{category.Name} ({i + 1}/{categoryList.Count})",
                        (float)i / Mathf.Max(1, categoryList.Count));

                    if (cancelled)
                    {
                        results.Add(new AnimationGenerationResult
                        {
                            Category = category.Name,
                            Status = AnimationGenerationStatus.Skipped,
                            Message = "Cancelled."
                        });
                        break;
                    }

                    try
                    {
                        results.Add(GenerateSingle(libraryPath, category, options));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new AnimationGenerationResult
                        {
                            Category = category.Name,
                            Status = AnimationGenerationStatus.Error,
                            Message = ex.Message
                        });
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            return results;
        }

        private static AnimationGenerationResult GenerateSingle(string libraryPath, CategoryModel category, AnimationGeneratorOptions options)
        {
            List<string> labels = category.Labels
                .Select(l => l.Name)
                .OrderBy(n => n, NaturalStringComparer.Instance)
                .ToList();

            if (labels.Count == 0)
            {
                return new AnimationGenerationResult
                {
                    Category = category.Name,
                    Status = AnimationGenerationStatus.Skipped,
                    Message = "Category has no labels."
                };
            }

            if (options.Reverse)
                labels.Reverse();

            if (options.PingPong && labels.Count > 1)
            {
                var pingPong = new List<string>(labels);
                for (int i = labels.Count - 2; i >= 0; i--)
                    pingPong.Add(labels[i]);
                labels = pingPong;
            }

            string folder = string.IsNullOrEmpty(options.OutputFolder)
                ? System.IO.Path.GetDirectoryName(libraryPath)?.Replace('\\', '/') ?? "Assets"
                : options.OutputFolder;
            string clipName = string.IsNullOrEmpty(options.NameFormat)
                ? category.Name
                : options.NameFormat.Replace("{category}", category.Name);
            string clipPath = $"{folder}/{clipName}.anim";

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip != null && !options.OverwriteExisting)
            {
                return new AnimationGenerationResult
                {
                    Category = category.Name,
                    Status = AnimationGenerationStatus.Skipped,
                    ClipPath = clipPath,
                    Message = $"'{clipPath}' already exists (Overwrite Existing is off)."
                };
            }

            bool isNew = clip == null;
            if (isNew)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = ClipSampleRate;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = options.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var binding = EditorCurveBinding.FloatCurve(options.ResolverPath, typeof(SpriteResolver), "m_SpriteHash");
            AnimationUtility.SetEditorCurve(clip, binding, null);

            var curve = new AnimationCurve();
            float frameDuration = 1f / Mathf.Max(options.FrameRate, 0.01f);

            for (int i = 0; i < labels.Count; i++)
                AddSpriteKey(curve, category.Name, labels[i], i * frameDuration);

            // Looping clips need a closing key repeating frame 0 one frameDuration after the
            // last real frame - otherwise the last frame's hold is cut short (or skipped
            // entirely) the instant the clip wraps back to time 0.
            if (options.Loop)
                AddSpriteKey(curve, category.Name, labels[0], labels.Count * frameDuration);

            AnimationUtility.SetEditorCurve(clip, binding, curve);
            EditorUtility.SetDirty(clip);

            return new AnimationGenerationResult
            {
                Category = category.Name,
                Status = isNew ? AnimationGenerationStatus.Created : AnimationGenerationStatus.Updated,
                ClipPath = clipPath,
                Clip = clip
            };
        }

        private static void AddSpriteKey(AnimationCurve curve, string categoryName, string labelName, float time)
        {
            int hash = Animator.StringToHash($"{categoryName}_{labelName}") & 0x3FFFFFFF;
            float hashAsFloat = BitConverter.Int32BitsToSingle(hash);
            int keyIndex = curve.AddKey(new Keyframe(time, hashAsFloat));
            AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Constant);
        }
    }
}
