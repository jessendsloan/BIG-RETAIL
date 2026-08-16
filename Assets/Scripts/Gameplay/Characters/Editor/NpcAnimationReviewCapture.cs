using System;
using System.Collections.Generic;
using System.IO;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Creates a disposable, off-screen review of one NPC animation without
    /// changing the selected scene Person, prefab, or animation clip.
    /// </summary>
    public static class NpcAnimationReviewCapture
    {
        public const int SampleCount = 11;

        private const int FrameWidth = 256;
        private const int FrameHeight = 320;
        private const int LabelHeight = 24;
        private const int SheetColumns = 4;
        private const int SheetPadding = 8;
        private const float CameraPadding = 1.15f;

        private static readonly Color PreviewBackground =
            new Color(0.075f, 0.095f, 0.12f, 1f);


        public static NpcAnimationReviewCaptureResult Capture(
            NpcCutoutRig sourceRig,
            AnimationClip clip)
        {
            if (sourceRig == null)
            {
                throw new ArgumentNullException(nameof(sourceRig));
            }

            return Capture(sourceRig, clip, sourceRig.Facing);
        }


        public static NpcAnimationReviewCaptureResult Capture(
            NpcCutoutRig sourceRig,
            AnimationClip clip,
            NpcFacing facing)
        {
            if (sourceRig == null)
            {
                throw new ArgumentNullException(nameof(sourceRig));
            }

            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (!sourceRig.TryValidate(out string failureReason))
            {
                throw new InvalidOperationException(
                    $"The selected Person rig is incomplete: {failureReason}");
            }

            PreviewRenderUtility previewUtility = null;
            GameObject previewPerson = null;
            Texture2D contactSheet = null;

            try
            {
                previewUtility = CreatePreviewUtility();
                previewPerson = UnityEngine.Object.Instantiate(
                    sourceRig.gameObject);
                previewPerson.name =
                    $"{sourceRig.gameObject.name} Animation Review";
                SetHideFlagsRecursively(previewPerson);
                previewPerson.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                previewPerson.transform.localScale = Vector3.one;

                Animator[] animators =
                    previewPerson.GetComponentsInChildren<Animator>(true);

                for (int index = 0; index < animators.Length; index++)
                {
                    animators[index].enabled = false;
                }

                NpcCutoutRig previewRig =
                    previewPerson.GetComponent<NpcCutoutRig>();

                if (previewRig == null)
                {
                    previewRig =
                        previewPerson.GetComponentInChildren<NpcCutoutRig>(
                            true);
                }

                if (previewRig == null)
                {
                    throw new InvalidOperationException(
                        "The temporary review Person has no NPC cutout rig.");
                }

                previewUtility.AddSingleGO(previewPerson);

                Bounds captureBounds = CalculateCaptureBounds(
                    previewPerson,
                    previewRig,
                    facing,
                    clip);
                PositionCamera(previewUtility, captureBounds);

                contactSheet = CreateContactSheet();
                NpcAnimationReviewManifest manifest =
                    CreateManifest(sourceRig, clip, facing);

                for (int sampleIndex = 0;
                     sampleIndex < SampleCount;
                     sampleIndex++)
                {
                    float sampleTime = GetSampleTime(
                        clip.length,
                        sampleIndex,
                        SampleCount);
                    SamplePose(
                        previewPerson,
                        previewRig,
                        facing,
                        clip,
                        sampleTime);

                    manifest.frames.Add(
                        CaptureFrameData(
                            previewRig,
                            sampleIndex,
                            sampleTime));

                    Texture2D frame = RenderFrame(previewUtility);

                    try
                    {
                        CopyFrameToSheet(
                            contactSheet,
                            frame,
                            sampleIndex);
                    }
                    finally
                    {
                        if (frame != null)
                        {
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                    }
                }

                contactSheet.Apply(false, false);

                string reviewFolder = CreateReviewFolder(
                    clip.name,
                    facing);
                string imagePath = Path.Combine(
                    reviewFolder,
                    "contact-sheet.png");
                string dataPath = Path.Combine(
                    reviewFolder,
                    "pose-data.json");

                File.WriteAllBytes(
                    imagePath,
                    contactSheet.EncodeToPNG());
                File.WriteAllText(
                    dataPath,
                    JsonUtility.ToJson(manifest, true));

                return new NpcAnimationReviewCaptureResult(
                    reviewFolder,
                    imagePath,
                    dataPath);
            }
            finally
            {
                if (contactSheet != null)
                {
                    UnityEngine.Object.DestroyImmediate(contactSheet);
                }

                if (previewUtility != null)
                {
                    previewUtility.Cleanup();
                }

                if (previewPerson != null)
                {
                    UnityEngine.Object.DestroyImmediate(previewPerson);
                }
            }
        }


        public static float GetSampleTime(
            float clipLength,
            int sampleIndex,
            int sampleCount)
        {
            if (clipLength <= 0f
                || sampleCount <= 1)
            {
                return 0f;
            }

            int clampedIndex = Mathf.Clamp(
                sampleIndex,
                0,
                sampleCount - 1);

            return clipLength
                   * clampedIndex
                   / (sampleCount - 1f);
        }


        public static bool IsFacingCompatible(
            AnimationClip clip,
            NpcFacing facing)
        {
            if (clip == null)
            {
                return true;
            }

            string clipName = clip.name.ToLowerInvariant();
            bool namesNorth = clipName.Contains("northfacing");
            bool namesSouth = clipName.Contains("southfacing");

            if (!namesNorth && !namesSouth)
            {
                return true;
            }

            bool facingUsesNorth =
                NpcFacingUtility.UsesNorthFacingAnimation(facing);

            return namesNorth
                ? facingUsesNorth
                : !facingUsesNorth;
        }


        public static string GetReviewRootPath(
            string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException(
                    "A project folder is required.",
                    nameof(projectRoot));
            }

            // Unity clears its project Temp directory during a clean exit.
            // Logs is already Git-ignored and remains available afterward.
            return Path.Combine(
                projectRoot,
                "Logs",
                "CodexAnimationReviews");
        }


        private static PreviewRenderUtility CreatePreviewUtility()
        {
            PreviewRenderUtility previewUtility =
                new PreviewRenderUtility();
            previewUtility.camera.orthographic = true;
            previewUtility.camera.allowHDR = false;
            previewUtility.camera.allowMSAA = true;
            previewUtility.camera.clearFlags =
                CameraClearFlags.SolidColor;
            previewUtility.camera.backgroundColor = PreviewBackground;
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = 50f;
            return previewUtility;
        }


        private static Bounds CalculateCaptureBounds(
            GameObject previewPerson,
            NpcCutoutRig previewRig,
            NpcFacing facing,
            AnimationClip clip)
        {
            Bounds combinedBounds = default;
            bool hasBounds = false;

            for (int sampleIndex = 0;
                 sampleIndex < SampleCount;
                 sampleIndex++)
            {
                float sampleTime = GetSampleTime(
                    clip.length,
                    sampleIndex,
                    SampleCount);
                SamplePose(
                    previewPerson,
                    previewRig,
                    facing,
                    clip,
                    sampleTime);

                if (!TryCalculateRenderableBounds(
                        previewPerson,
                        out Bounds frameBounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = frameBounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(frameBounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException(
                    "The temporary review Person has no visible renderers.");
            }

            return combinedBounds;
        }


        private static void SamplePose(
            GameObject previewPerson,
            NpcCutoutRig previewRig,
            NpcFacing facing,
            AnimationClip clip,
            float sampleTime)
        {
            // Reset the direction first so properties absent from the clip
            // cannot leak forward from the preceding sampled frame.
            previewRig.SetFacing(facing);
            clip.SampleAnimation(previewPerson, sampleTime);
        }


        private static bool TryCalculateRenderableBounds(
            GameObject previewPerson,
            out Bounds bounds)
        {
            Renderer[] renderers =
                previewPerson.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            bool foundRenderer = false;

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];

                if (renderer == null
                    || !renderer.enabled
                    || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (renderer is SpriteRenderer spriteRenderer
                    && spriteRenderer.sprite == null)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = renderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return foundRenderer;
        }


        private static void PositionCamera(
            PreviewRenderUtility previewUtility,
            Bounds bounds)
        {
            float aspect = FrameWidth / (float)FrameHeight;
            float verticalExtent = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / aspect);

            previewUtility.camera.orthographicSize = Mathf.Max(
                0.25f,
                verticalExtent * CameraPadding);
            previewUtility.camera.transform.position =
                bounds.center + Vector3.back * 10f;
            previewUtility.camera.transform.rotation =
                Quaternion.identity;
        }


        private static Texture2D RenderFrame(
            PreviewRenderUtility previewUtility)
        {
            Rect frameRect = new Rect(
                0f,
                0f,
                FrameWidth,
                FrameHeight);
            previewUtility.BeginStaticPreview(frameRect);
            previewUtility.Render(true);
            return previewUtility.EndStaticPreview();
        }


        private static Texture2D CreateContactSheet()
        {
            int rows = Mathf.CeilToInt(
                SampleCount / (float)SheetColumns);
            int sheetWidth =
                SheetPadding
                + SheetColumns * (FrameWidth + SheetPadding);
            int sheetHeight =
                SheetPadding
                + rows * (FrameHeight + LabelHeight + SheetPadding);
            Texture2D sheet = new Texture2D(
                sheetWidth,
                sheetHeight,
                TextureFormat.RGBA32,
                false,
                false);
            Color32 background = new Color32(18, 24, 31, 255);
            Color32[] pixels = new Color32[sheetWidth * sheetHeight];

            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = background;
            }

            sheet.SetPixels32(pixels);
            return sheet;
        }


        private static void CopyFrameToSheet(
            Texture2D sheet,
            Texture2D frame,
            int sampleIndex)
        {
            int column = sampleIndex % SheetColumns;
            int row = sampleIndex / SheetColumns;
            int x = SheetPadding
                    + column * (FrameWidth + SheetPadding);
            int frameY = sheet.height
                         - SheetPadding
                         - row * (FrameHeight + LabelHeight + SheetPadding)
                         - FrameHeight;

            sheet.SetPixels32(
                x,
                frameY,
                FrameWidth,
                FrameHeight,
                frame.GetPixels32());

            DrawFrameLabel(
                sheet,
                x,
                frameY - LabelHeight,
                sampleIndex);
        }


        private static void DrawFrameLabel(
            Texture2D sheet,
            int cellX,
            int labelY,
            int sampleIndex)
        {
            string label = $"F{sampleIndex:00}";
            const int glyphWidth = 3;
            const int glyphScale = 2;
            const int glyphSpacing = 2;
            int labelWidth =
                label.Length * glyphWidth * glyphScale
                + (label.Length - 1) * glyphSpacing;
            int x = cellX + (FrameWidth - labelWidth) / 2;
            int y = labelY + 7;
            Color32 color = new Color32(220, 232, 242, 255);

            for (int index = 0; index < label.Length; index++)
            {
                DrawGlyph(
                    sheet,
                    x,
                    y,
                    label[index],
                    glyphScale,
                    color);
                x += glyphWidth * glyphScale + glyphSpacing;
            }
        }


        private static void DrawGlyph(
            Texture2D sheet,
            int x,
            int y,
            char glyph,
            int scale,
            Color32 color)
        {
            int[] rows = GetGlyphRows(glyph);

            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    int mask = 1 << (2 - column);

                    if ((rows[row] & mask) == 0)
                    {
                        continue;
                    }

                    for (int offsetY = 0;
                         offsetY < scale;
                         offsetY++)
                    {
                        for (int offsetX = 0;
                             offsetX < scale;
                             offsetX++)
                        {
                            sheet.SetPixel(
                                x + column * scale + offsetX,
                                y + (4 - row) * scale + offsetY,
                                color);
                        }
                    }
                }
            }
        }


        private static int[] GetGlyphRows(
            char glyph)
        {
            switch (glyph)
            {
                case 'F':
                    return new[] { 7, 4, 6, 4, 4 };
                case '0':
                    return new[] { 7, 5, 5, 5, 7 };
                case '1':
                    return new[] { 2, 6, 2, 2, 7 };
                case '2':
                    return new[] { 7, 1, 7, 4, 7 };
                case '3':
                    return new[] { 7, 1, 7, 1, 7 };
                case '4':
                    return new[] { 5, 5, 7, 1, 1 };
                case '5':
                    return new[] { 7, 4, 7, 1, 7 };
                case '6':
                    return new[] { 7, 4, 7, 5, 7 };
                case '7':
                    return new[] { 7, 1, 1, 1, 1 };
                case '8':
                    return new[] { 7, 5, 7, 5, 7 };
                case '9':
                    return new[] { 7, 5, 7, 1, 7 };
                default:
                    return new[] { 0, 0, 0, 0, 0 };
            }
        }


        private static NpcAnimationReviewManifest CreateManifest(
            NpcCutoutRig sourceRig,
            AnimationClip clip,
            NpcFacing facing)
        {
            return new NpcAnimationReviewManifest
            {
                clipName = clip.name,
                clipAssetPath = AssetDatabase.GetAssetPath(clip),
                sourcePerson = sourceRig.gameObject.name,
                facing = facing.ToString(),
                clipLengthSeconds = clip.length,
                sourceFrameRate = clip.frameRate,
                sampleCount = SampleCount,
                capturedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                frames = new List<NpcAnimationReviewFrame>(SampleCount)
            };
        }


        private static NpcAnimationReviewFrame CaptureFrameData(
            NpcCutoutRig previewRig,
            int sampleIndex,
            float sampleTime)
        {
            NpcAnimationReviewFrame frame =
                new NpcAnimationReviewFrame
                {
                    label = $"F{sampleIndex:00}",
                    sampleIndex = sampleIndex,
                    timeSeconds = sampleTime,
                    normalizedTime =
                        sampleIndex / (SampleCount - 1f),
                    bones = new List<NpcAnimationReviewBonePose>()
                };
            IReadOnlyList<NpcRigBoneDefinition> definitions =
                NpcRigDefinition.BoneDefinitions;

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneId boneId = definitions[index].Id;

                if (!previewRig.TryGetBone(
                        boneId,
                        out Transform bone))
                {
                    continue;
                }

                Vector3 signedEuler = bone.localEulerAngles;
                signedEuler.x =
                    NpcPoseControlsUtility.NormalizeAngle(signedEuler.x);
                signedEuler.y =
                    NpcPoseControlsUtility.NormalizeAngle(signedEuler.y);
                signedEuler.z =
                    NpcPoseControlsUtility.NormalizeAngle(signedEuler.z);

                frame.bones.Add(
                    new NpcAnimationReviewBonePose
                    {
                        bone = boneId.ToString(),
                        localPosition = bone.localPosition,
                        localEulerAngles = signedEuler,
                        localScale = bone.localScale
                    });
            }

            return frame;
        }


        private static string CreateReviewFolder(
            string clipName,
            NpcFacing facing)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;

            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException(
                    "Unity could not resolve the project folder.");
            }

            string folderName =
                $"{SanitizeFileName(clipName)}_{facing}_"
                + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string reviewFolder = Path.Combine(
                GetReviewRootPath(projectRoot),
                folderName);
            Directory.CreateDirectory(reviewFolder);
            return reviewFolder;
        }


        private static string SanitizeFileName(
            string fileName)
        {
            string sanitized = string.IsNullOrWhiteSpace(fileName)
                ? "Animation"
                : fileName;

            foreach (char invalidCharacter
                     in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidCharacter, '_');
            }

            return sanitized;
        }


        private static void SetHideFlagsRecursively(
            GameObject root)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            for (int index = 0; index < transforms.Length; index++)
            {
                transforms[index].gameObject.hideFlags =
                    HideFlags.HideAndDontSave;
            }
        }


        [Serializable]
        private sealed class NpcAnimationReviewManifest
        {
            public string clipName;
            public string clipAssetPath;
            public string sourcePerson;
            public string facing;
            public float clipLengthSeconds;
            public float sourceFrameRate;
            public int sampleCount;
            public string capturedUtc;
            public string unityVersion;
            public List<NpcAnimationReviewFrame> frames;
        }


        [Serializable]
        private sealed class NpcAnimationReviewFrame
        {
            public string label;
            public int sampleIndex;
            public float timeSeconds;
            public float normalizedTime;
            public List<NpcAnimationReviewBonePose> bones;
        }


        [Serializable]
        private sealed class NpcAnimationReviewBonePose
        {
            public string bone;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale;
        }
    }


    public readonly struct NpcAnimationReviewCaptureResult
    {
        public string ReviewFolder { get; }

        public string ImagePath { get; }

        public string DataPath { get; }


        public NpcAnimationReviewCaptureResult(
            string reviewFolder,
            string imagePath,
            string dataPath)
        {
            ReviewFolder = reviewFolder;
            ImagePath = imagePath;
            DataPath = dataPath;
        }
    }
}
