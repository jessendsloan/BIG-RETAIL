using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    internal enum NpcPreviewFocus
    {
        FullBody = 0,
        UpperBody = 1,
        Head = 2
    }


    internal enum NpcRigOverlayFocus
    {
        FullSkeleton = 0,
        BodyAndHead = 1,
        SourceCameraLeftArm = 2,
        SourceCameraRightArm = 3,
        SourceCameraLeftLeg = 4,
        SourceCameraRightLeg = 5
    }


    /// <summary>
    /// Owns one hidden Person instance for Editor-only appearance previews.
    /// It never opens, dirties, or saves a scene or prefab.
    /// </summary>
    internal sealed class NpcPersonPreviewCanvas : IDisposable
    {
        private const string PersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        private PreviewRenderUtility previewUtility;
        private GameObject previewPerson;
        private NpcCutoutRig previewRig;
        private NpcAppearanceProfile previewProfile;
        private Texture previewTexture;
        private float zoom = 1f;
        private readonly Dictionary<NpcRigBoneId, Quaternion>
            bindLocalRotations =
                new Dictionary<NpcRigBoneId, Quaternion>();


        public string ErrorMessage { get; private set; }


        public bool IsReady => previewRig != null;


        public void Apply(
            NpcAppearanceSelection selection,
            NpcFacing facing)
        {
            EnsureCreated();

            if (previewRig == null || previewProfile == null)
            {
                return;
            }

            RestoreBindPose();
            previewProfile.Configure(
                "Appearance Creator Preview",
                selection);

            previewRig.SetAppearancePreview(previewProfile);
            previewRig.SetFacing(facing);
            CaptureBindPose();
        }


        public void SetFacing(
            NpcFacing facing)
        {
            EnsureCreated();
            RestoreBindPose();
            previewRig?.SetFacing(facing);
            CaptureBindPose();
        }


        /// <summary>
        /// Applies temporary Editor-only rotations to the real preview bones.
        /// The supplied angles are offsets from the current facing's bind pose;
        /// no appearance asset, prefab, animation clip, or scene is modified.
        /// </summary>
        public void SetTestPose(
            IReadOnlyDictionary<NpcRigBoneId, float> angleOffsets)
        {
            EnsureCreated();

            if (previewRig == null)
            {
                return;
            }

            RestoreBindPose();

            if (angleOffsets == null)
            {
                return;
            }

            foreach (KeyValuePair<NpcRigBoneId, float> entry
                     in angleOffsets)
            {
                if (Mathf.Approximately(entry.Value, 0f)
                    || !bindLocalRotations.TryGetValue(
                        entry.Key,
                        out Quaternion bindRotation)
                    || !previewRig.TryGetBone(
                        entry.Key,
                        out Transform bone))
                {
                    continue;
                }

                bone.localRotation =
                    bindRotation
                    * Quaternion.Euler(0f, 0f, entry.Value);
            }
        }


        public void ResetTestPose()
        {
            RestoreBindPose();
        }


        public void Draw(
            Rect rect,
            NpcPreviewFocus focus,
            bool showRigAnatomy = false,
            NpcRigOverlayFocus overlayFocus =
                NpcRigOverlayFocus.FullSkeleton)
        {
            EnsureCreated();

            EditorGUI.DrawRect(
                rect,
                new Color(0.075f, 0.095f, 0.12f, 1f));

            HandleZoom(rect);

            if (previewRig == null)
            {
                GUI.Label(
                    rect,
                    string.IsNullOrWhiteSpace(ErrorMessage)
                        ? "Person preview unavailable."
                        : ErrorMessage,
                    CreateCenteredStyle());
                return;
            }

            Render(rect, focus);

            if (previewTexture != null)
            {
                GUI.DrawTexture(
                    rect,
                    previewTexture,
                    ScaleMode.StretchToFill,
                    false);
            }

            if (showRigAnatomy)
            {
                DrawRigAnatomy(rect, overlayFocus);
            }

            Rect hint = rect;
            hint.height = 20f;
            hint.y = rect.yMax - 26f;
            hint.xMin += 8f;
            hint.xMax -= 8f;

            GUI.Label(
                hint,
                "Mouse wheel: zoom",
                EditorStyles.centeredGreyMiniLabel);
        }


        public void ResetZoom()
        {
            zoom = 1f;
        }


        public void Dispose()
        {
            bindLocalRotations.Clear();
            previewRig = null;

            if (previewPerson != null)
            {
                UnityEngine.Object.DestroyImmediate(previewPerson);
                previewPerson = null;
            }

            if (previewProfile != null)
            {
                UnityEngine.Object.DestroyImmediate(previewProfile);
                previewProfile = null;
            }

            DestroyPreviewTexture();

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }
        }


        private void EnsureCreated()
        {
            if (previewUtility != null && previewRig != null)
            {
                return;
            }

            Dispose();

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PersonPrefabPath);

            if (prefab == null)
            {
                ErrorMessage =
                    "The shared Person prefab could not be found.";
                return;
            }

            previewUtility = new PreviewRenderUtility();
            previewUtility.camera.orthographic = true;
            previewUtility.camera.allowHDR = false;
            previewUtility.camera.allowMSAA = true;
            previewUtility.camera.clearFlags =
                CameraClearFlags.SolidColor;
            previewUtility.camera.backgroundColor =
                new Color(0.075f, 0.095f, 0.12f, 1f);
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = 50f;

            previewPerson =
                previewUtility.InstantiatePrefabInScene(prefab);

            if (previewPerson == null)
            {
                ErrorMessage =
                    "Unity could not create the hidden Person preview.";
                Dispose();
                return;
            }

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

            previewRig =
                previewPerson.GetComponentInChildren<NpcCutoutRig>(true);

            if (previewRig == null)
            {
                ErrorMessage =
                    "The shared Person prefab has no NpcCutoutRig.";
                Dispose();
                return;
            }

            previewProfile =
                ScriptableObject.CreateInstance<NpcAppearanceProfile>();
            previewProfile.name = "Appearance Creator Preview";
            previewProfile.hideFlags = HideFlags.HideAndDontSave;
            ErrorMessage = string.Empty;
        }


        private void CaptureBindPose()
        {
            bindLocalRotations.Clear();

            if (previewRig == null)
            {
                return;
            }

            IReadOnlyList<NpcRigBoneDefinition> definitions =
                NpcRigDefinition.BoneDefinitions;

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneId id = definitions[index].Id;

                if (previewRig.TryGetBone(id, out Transform bone))
                {
                    bindLocalRotations[id] = bone.localRotation;
                }
            }
        }


        private void RestoreBindPose()
        {
            if (previewRig == null)
            {
                return;
            }

            foreach (KeyValuePair<NpcRigBoneId, Quaternion> entry
                     in bindLocalRotations)
            {
                if (previewRig.TryGetBone(
                        entry.Key,
                        out Transform bone))
                {
                    bone.localRotation = entry.Value;
                }
            }
        }


        private void Render(
            Rect rect,
            NpcPreviewFocus focus)
        {
            if (Event.current.type != EventType.Repaint
                || previewUtility == null
                || previewPerson == null)
            {
                return;
            }

            DestroyPreviewTexture();
            PositionCamera(rect, focus);

            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.Render(true);
            previewTexture = previewUtility.EndPreview();
        }


        private void PositionCamera(
            Rect rect,
            NpcPreviewFocus focus)
        {
            Bounds bounds = CalculateBounds(focus);
            float aspect = Mathf.Max(
                0.1f,
                rect.width / Mathf.Max(1f, rect.height));

            float verticalExtent = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / aspect);

            float orthographicSize =
                Mathf.Max(0.25f, verticalExtent * 1.35f * zoom);
            previewUtility.camera.orthographicSize = orthographicSize;

            Vector3 cameraCenter = bounds.center;

            if (focus == NpcPreviewFocus.FullBody
                && previewRig != null
                && previewRig.TryGetBone(
                    NpcRigBoneId.Root,
                    out Transform root))
            {
                // Keep the world/ground root visually stable while body
                // proportions change. The person moves relative to this
                // point instead of the camera continuously re-centering it.
                cameraCenter.x = root.position.x;
                cameraCenter.y = root.position.y
                                 + orthographicSize * 0.72f;
            }

            previewUtility.camera.transform.position =
                cameraCenter + Vector3.back * 10f;
            previewUtility.camera.transform.rotation =
                Quaternion.identity;
        }


        private void DrawRigAnatomy(
            Rect rect,
            NpcRigOverlayFocus focus)
        {
            if (Event.current.type != EventType.Repaint
                || previewRig == null
                || previewUtility == null)
            {
                return;
            }

            HashSet<NpcRigBoneId> visibleBones =
                CreateVisibleBoneSet(focus);

            Handles.BeginGUI();

            DrawPartLinksAndBounds(rect, visibleBones);

            IReadOnlyList<NpcRigBoneDefinition> definitions =
                NpcRigDefinition.BoneDefinitions;

            Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneDefinition definition = definitions[index];

                if (!visibleBones.Contains(definition.Id)
                    || !definition.HasParent
                    || !visibleBones.Contains(definition.ParentId)
                    || !previewRig.TryGetBone(
                        definition.Id,
                        out Transform bone)
                    || !previewRig.TryGetBone(
                        definition.ParentId,
                        out Transform parent))
                {
                    continue;
                }

                Handles.DrawAAPolyLine(
                    2.5f,
                    ToGuiPoint(rect, parent.position),
                    ToGuiPoint(rect, bone.position));
            }

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneDefinition definition = definitions[index];

                if (!visibleBones.Contains(definition.Id)
                    || !previewRig.TryGetBone(
                        definition.Id,
                        out Transform bone))
                {
                    continue;
                }

                Vector2 point = ToGuiPoint(rect, bone.position);
                Handles.DrawSolidDisc(
                    point,
                    Vector3.forward,
                    definition.Id == NpcRigBoneId.Root ? 5f : 3.5f);

                if (focus != NpcRigOverlayFocus.FullSkeleton)
                {
                    GUI.Label(
                        new Rect(
                            point.x + 6f,
                            point.y - 9f,
                            170f,
                            18f),
                        GetBoneLabel(definition.Id),
                        EditorStyles.whiteMiniLabel);
                }
            }

            Handles.EndGUI();

            GUI.Label(
                new Rect(
                    rect.x + 10f,
                    rect.y + 8f,
                    360f,
                    20f),
                "Cyan: bones and joints    Gold: visual sprite bounds",
                EditorStyles.whiteMiniLabel);
        }


        private void DrawPartLinksAndBounds(
            Rect rect,
            HashSet<NpcRigBoneId> visibleBones)
        {
            IReadOnlyList<NpcRigPartDefinition> definitions =
                NpcRigDefinition.PartDefinitions;
            Handles.color = new Color(1f, 0.72f, 0.18f, 0.72f);

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigPartDefinition definition = definitions[index];

                if (!visibleBones.Contains(definition.BoneId)
                    || !previewRig.TryGetBone(
                        definition.BoneId,
                        out Transform bone)
                    || !previewRig.TryGetPartRenderer(
                        definition.Id,
                        out SpriteRenderer renderer)
                    || renderer == null
                    || !renderer.enabled
                    || renderer.sprite == null)
                {
                    continue;
                }

                Vector2 bonePoint = ToGuiPoint(rect, bone.position);
                Vector2 visualCenter =
                    ToGuiPoint(rect, renderer.bounds.center);

                Handles.DrawDottedLine(
                    bonePoint,
                    visualCenter,
                    3f);

                Bounds spriteBounds = renderer.sprite.bounds;
                Vector3 center = spriteBounds.center;
                Vector3 extents = spriteBounds.extents;
                Vector3[] localCorners =
                {
                    center + new Vector3(-extents.x, -extents.y),
                    center + new Vector3(-extents.x, extents.y),
                    center + new Vector3(extents.x, extents.y),
                    center + new Vector3(extents.x, -extents.y),
                    center + new Vector3(-extents.x, -extents.y)
                };
                Vector3[] guiCorners = new Vector3[localCorners.Length];

                for (int corner = 0;
                     corner < localCorners.Length;
                     corner++)
                {
                    guiCorners[corner] = ToGuiPoint(
                        rect,
                        renderer.transform.TransformPoint(
                            localCorners[corner]));
                }

                Handles.DrawAAPolyLine(1.5f, guiCorners);
            }
        }


        private Vector2 ToGuiPoint(
            Rect rect,
            Vector3 worldPosition)
        {
            Vector3 viewport = previewUtility.camera
                .WorldToViewportPoint(worldPosition);

            return new Vector2(
                rect.x + viewport.x * rect.width,
                rect.y + (1f - viewport.y) * rect.height);
        }


        private static HashSet<NpcRigBoneId> CreateVisibleBoneSet(
            NpcRigOverlayFocus focus)
        {
            HashSet<NpcRigBoneId> result =
                new HashSet<NpcRigBoneId>();

            switch (focus)
            {
                case NpcRigOverlayFocus.BodyAndHead:
                    AddBones(
                        result,
                        NpcRigBoneId.Root,
                        NpcRigBoneId.Pelvis,
                        NpcRigBoneId.SpineLower,
                        NpcRigBoneId.Chest,
                        NpcRigBoneId.Neck,
                        NpcRigBoneId.Head);
                    break;

                case NpcRigOverlayFocus.SourceCameraLeftArm:
                    AddBones(
                        result,
                        NpcRigBoneId.Chest,
                        NpcRigBoneId.ShoulderSourceCameraLeft,
                        NpcRigBoneId.UpperArmSourceCameraLeft,
                        NpcRigBoneId.ForearmSourceCameraLeft,
                        NpcRigBoneId.HandSourceCameraLeft);
                    break;

                case NpcRigOverlayFocus.SourceCameraRightArm:
                    AddBones(
                        result,
                        NpcRigBoneId.Chest,
                        NpcRigBoneId.ShoulderSourceCameraRight,
                        NpcRigBoneId.UpperArmSourceCameraRight,
                        NpcRigBoneId.ForearmSourceCameraRight,
                        NpcRigBoneId.HandSourceCameraRight);
                    break;

                case NpcRigOverlayFocus.SourceCameraLeftLeg:
                    AddBones(
                        result,
                        NpcRigBoneId.Pelvis,
                        NpcRigBoneId.ThighSourceCameraLeft,
                        NpcRigBoneId.ShinSourceCameraLeft,
                        NpcRigBoneId.FootSourceCameraLeft);
                    break;

                case NpcRigOverlayFocus.SourceCameraRightLeg:
                    AddBones(
                        result,
                        NpcRigBoneId.Pelvis,
                        NpcRigBoneId.ThighSourceCameraRight,
                        NpcRigBoneId.ShinSourceCameraRight,
                        NpcRigBoneId.FootSourceCameraRight);
                    break;

                default:
                    IReadOnlyList<NpcRigBoneDefinition> definitions =
                        NpcRigDefinition.BoneDefinitions;

                    for (int index = 0;
                         index < definitions.Count;
                         index++)
                    {
                        result.Add(definitions[index].Id);
                    }
                    break;
            }

            return result;
        }


        private static void AddBones(
            HashSet<NpcRigBoneId> result,
            params NpcRigBoneId[] bones)
        {
            for (int index = 0; index < bones.Length; index++)
            {
                result.Add(bones[index]);
            }
        }


        private static string GetBoneLabel(
            NpcRigBoneId id)
        {
            switch (id)
            {
                case NpcRigBoneId.Root:
                    return "Ground Root";

                case NpcRigBoneId.Pelvis:
                    return "Pelvis / Body Anchor";

                case NpcRigBoneId.SpineLower:
                    return "Lower Spine";

                case NpcRigBoneId.Chest:
                    return "Chest / Shoulder Base";

                case NpcRigBoneId.Neck:
                    return "Neck Base / Neck Pivot";

                case NpcRigBoneId.Head:
                    return "Head / Head Pivot";

                case NpcRigBoneId.ShoulderSourceCameraLeft:
                    return "Camera-Left Shoulder Spacing Anchor";

                case NpcRigBoneId.UpperArmSourceCameraLeft:
                    return "Camera-Left Shoulder / Upper-Arm Pivot";

                case NpcRigBoneId.ForearmSourceCameraLeft:
                    return "Camera-Left Elbow / Forearm Pivot";

                case NpcRigBoneId.HandSourceCameraLeft:
                    return "Camera-Left Wrist / Hand Pivot";

                case NpcRigBoneId.ShoulderSourceCameraRight:
                    return "Camera-Right Shoulder Spacing Anchor";

                case NpcRigBoneId.UpperArmSourceCameraRight:
                    return "Camera-Right Shoulder / Upper-Arm Pivot";

                case NpcRigBoneId.ForearmSourceCameraRight:
                    return "Camera-Right Elbow / Forearm Pivot";

                case NpcRigBoneId.HandSourceCameraRight:
                    return "Camera-Right Wrist / Hand Pivot";

                case NpcRigBoneId.ThighSourceCameraLeft:
                    return "Camera-Left Hip / Thigh Pivot";

                case NpcRigBoneId.ShinSourceCameraLeft:
                    return "Camera-Left Knee / Shin Pivot";

                case NpcRigBoneId.FootSourceCameraLeft:
                    return "Camera-Left Ankle / Foot Pivot";

                case NpcRigBoneId.ThighSourceCameraRight:
                    return "Camera-Right Hip / Thigh Pivot";

                case NpcRigBoneId.ShinSourceCameraRight:
                    return "Camera-Right Knee / Shin Pivot";

                case NpcRigBoneId.FootSourceCameraRight:
                    return "Camera-Right Ankle / Foot Pivot";

                default:
                    return id.ToString();
            }
        }


        private Bounds CalculateBounds(
            NpcPreviewFocus focus)
        {
            if (focus == NpcPreviewFocus.Head)
            {
                return CalculatePartBounds(
                    NpcRigPartId.Head,
                    NpcRigPartId.Neck,
                    NpcRigPartId.HairRear,
                    NpcRigPartId.HairFront);
            }

            if (focus == NpcPreviewFocus.UpperBody)
            {
                return CalculatePartBounds(
                    NpcRigPartId.Head,
                    NpcRigPartId.Neck,
                    NpcRigPartId.HairRear,
                    NpcRigPartId.HairFront,
                    NpcRigPartId.Torso,
                    NpcRigPartId.UpperArmSourceCameraLeft,
                    NpcRigPartId.UpperArmSourceCameraRight,
                    NpcRigPartId.ForearmSourceCameraLeft,
                    NpcRigPartId.ForearmSourceCameraRight,
                    NpcRigPartId.HandSourceCameraLeft,
                    NpcRigPartId.HandSourceCameraRight);
            }

            Renderer[] renderers =
                previewPerson.GetComponentsInChildren<Renderer>(true);

            return EncapsulateRenderers(renderers);
        }


        private Bounds CalculatePartBounds(
            params NpcRigPartId[] parts)
        {
            Bounds bounds = default;
            bool found = false;

            for (int index = 0; index < parts.Length; index++)
            {
                if (!previewRig.TryGetPartRenderer(
                        parts[index],
                        out SpriteRenderer renderer)
                    || renderer == null
                    || !renderer.enabled)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            IReadOnlyList<SpriteRenderer> hairDetails =
                previewRig.HairDetailRenderers;

            if (hairDetails != null)
            {
                for (int index = 0;
                     index < hairDetails.Count;
                     index++)
                {
                    SpriteRenderer renderer = hairDetails[index];

                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        bounds = renderer.bounds;
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return found
                ? bounds
                : new Bounds(
                    new Vector3(0f, 1.4f, 0f),
                    Vector3.one);
        }


        private static Bounds EncapsulateRenderers(
            Renderer[] renderers)
        {
            Bounds bounds = new Bounds(
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 2f, 0.1f));
            bool found = false;

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];

                if (!renderer.enabled)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }


        private void HandleZoom(
            Rect rect)
        {
            Event current = Event.current;

            if (current.type != EventType.ScrollWheel
                || !rect.Contains(current.mousePosition))
            {
                return;
            }

            zoom = Mathf.Clamp(
                zoom + current.delta.y * 0.08f,
                0.55f,
                2.25f);
            current.Use();
        }


        private void DestroyPreviewTexture()
        {
            if (previewTexture == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(previewTexture);
            previewTexture = null;
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


        private static GUIStyle CreateCenteredStyle()
        {
            return new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
    }
}
