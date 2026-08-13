using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    internal enum NpcPopulationAuditMotion
    {
        BindPose = 0,
        WalkInPlace = 1,
        DiamondWalkTest = 2
    }


    /// <summary>
    /// Owns a hidden group of Person prefab instances for population testing.
    /// All people, profiles, animation sampling, and path motion exist only in
    /// PreviewRenderUtility's temporary scene.
    /// </summary>
    internal sealed class NpcPopulationLineupCanvas : IDisposable
    {
        private const string PersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        private const string SouthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_SouthFacing.anim";

        private const string NorthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_NorthFacing.anim";

        private const float HorizontalSpacing = 1.25f;
        private const float VerticalSpacing = 2.35f;
        private const float DiamondRadiusX = 0.2f;
        private const float DiamondRadiusY = 0.13f;
        private const float DiamondSegmentDuration = 0.55f;

        private static readonly Vector3[] DiamondPoints =
        {
            new Vector3(0f, DiamondRadiusY, 0f),
            new Vector3(DiamondRadiusX, 0f, 0f),
            new Vector3(0f, -DiamondRadiusY, 0f),
            new Vector3(-DiamondRadiusX, 0f, 0f)
        };

        private static readonly NpcFacing[] DiamondFacings =
        {
            NpcFacing.SouthEast,
            NpcFacing.SouthWest,
            NpcFacing.NorthWest,
            NpcFacing.NorthEast
        };

        private readonly List<PreviewPerson> people =
            new List<PreviewPerson>();

        private PreviewRenderUtility previewUtility;
        private AnimationClip southFacingWalkClip;
        private AnimationClip northFacingWalkClip;
        private Texture previewTexture;
        private Bounds lineupBounds;
        private bool hasLineupBounds;
        private float zoom = 1f;


        public string ErrorMessage { get; private set; }

        public bool IsReady => previewUtility != null
                               && people.Count > 0;


        public void SetSamples(
            IReadOnlyList<NpcPopulationAuditSample> samples,
            NpcFacing facing)
        {
            Dispose();

            GameObject personPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PersonPrefabPath);

            if (personPrefab == null)
            {
                ErrorMessage =
                    "The shared Person prefab could not be found.";
                return;
            }

            southFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);
            northFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);
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

            List<NpcPopulationAuditSample> validSamples =
                new List<NpcPopulationAuditSample>();

            if (samples != null)
            {
                for (int index = 0; index < samples.Count; index++)
                {
                    if (samples[index] != null
                        && samples[index].IsValid)
                    {
                        validSamples.Add(samples[index]);
                    }
                }
            }

            if (validSamples.Count == 0)
            {
                ErrorMessage =
                    "This population did not generate any valid people.";
                return;
            }

            int columns = Mathf.Clamp(
                Mathf.CeilToInt(
                    Mathf.Sqrt(validSamples.Count * 1.35f)),
                2,
                6);
            int rows = Mathf.CeilToInt(
                validSamples.Count / (float)columns);
            float width = (columns - 1) * HorizontalSpacing;
            float height = (rows - 1) * VerticalSpacing;

            for (int index = 0; index < validSamples.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                Vector3 basePosition = new Vector3(
                    column * HorizontalSpacing - width * 0.5f,
                    height * 0.5f - row * VerticalSpacing,
                    0f);

                if (!TryCreatePerson(
                        personPrefab,
                        validSamples[index],
                        index,
                        basePosition,
                        facing,
                        out PreviewPerson person))
                {
                    Dispose();
                    return;
                }

                people.Add(person);
            }

            CalculateLineupBounds();
            ErrorMessage = string.Empty;
        }


        public void Evaluate(
            float elapsedTime,
            NpcPopulationAuditMotion motion,
            NpcFacing fixedFacing)
        {
            if (people.Count == 0)
            {
                return;
            }

            for (int index = 0; index < people.Count; index++)
            {
                PreviewPerson person = people[index];
                NpcFacing facing = fixedFacing;
                Vector3 pathOffset = Vector3.zero;
                float sampleTime = elapsedTime;

                if (motion
                    == NpcPopulationAuditMotion.DiamondWalkTest)
                {
                    float pathDuration =
                        DiamondSegmentDuration * DiamondPoints.Length;
                    float phaseOffset =
                        people.Count > 0
                            ? index / (float)people.Count * pathDuration
                            : 0f;
                    float pathTime = Mathf.Repeat(
                        elapsedTime + phaseOffset,
                        pathDuration);
                    float segmentProgress =
                        pathTime / DiamondSegmentDuration;
                    int segment = Mathf.FloorToInt(segmentProgress)
                                  % DiamondPoints.Length;
                    float segmentT = segmentProgress
                                     - Mathf.Floor(segmentProgress);
                    int nextPoint = (segment + 1)
                                    % DiamondPoints.Length;

                    pathOffset = Vector3.Lerp(
                        DiamondPoints[segment],
                        DiamondPoints[nextPoint],
                        segmentT);
                    facing = DiamondFacings[segment];
                    sampleTime = pathTime;
                }

                EnsureFacing(person, facing);
                RestoreBindPose(person);

                AnimationClip activeWalkClip =
                    NpcFacingUtility.UsesNorthFacingAnimation(facing)
                        ? northFacingWalkClip
                        : southFacingWalkClip;

                if (motion != NpcPopulationAuditMotion.BindPose
                    && activeWalkClip != null
                    && activeWalkClip.length > 0f)
                {
                    float animationPhase =
                        people.Count > 0
                            ? index / (float)people.Count
                              * activeWalkClip.length
                            : 0f;
                    activeWalkClip.SampleAnimation(
                        person.Root,
                        Mathf.Repeat(
                            sampleTime + animationPhase,
                            activeWalkClip.length));
                }

                person.Root.transform.position =
                    person.BasePosition + pathOffset;
            }
        }


        public void Draw(
            Rect rect,
            bool showLabels)
        {
            EditorGUI.DrawRect(
                rect,
                new Color(0.075f, 0.095f, 0.12f, 1f));
            HandleZoom(rect);

            if (!IsReady)
            {
                GUI.Label(
                    rect,
                    string.IsNullOrWhiteSpace(ErrorMessage)
                        ? "Population lineup unavailable."
                        : ErrorMessage,
                    CreateCenteredStyle());
                return;
            }

            Render(rect);

            if (previewTexture != null)
            {
                GUI.DrawTexture(
                    rect,
                    previewTexture,
                    ScaleMode.StretchToFill,
                    false);
            }

            if (showLabels)
            {
                DrawLabels(rect);
            }

            Rect hint = new Rect(
                rect.x + 8f,
                rect.yMax - 24f,
                rect.width - 16f,
                18f);
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
            for (int index = 0; index < people.Count; index++)
            {
                people[index].Dispose();
            }

            people.Clear();
            hasLineupBounds = false;
            southFacingWalkClip = null;
            northFacingWalkClip = null;
            DestroyPreviewTexture();

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }
        }


        private bool TryCreatePerson(
            GameObject personPrefab,
            NpcPopulationAuditSample sample,
            int displayIndex,
            Vector3 basePosition,
            NpcFacing facing,
            out PreviewPerson result)
        {
            result = null;
            GameObject root =
                previewUtility.InstantiatePrefabInScene(personPrefab);

            if (root == null)
            {
                ErrorMessage =
                    "Unity could not create a hidden Person preview.";
                return false;
            }

            SetHideFlagsRecursively(root);
            root.transform.SetPositionAndRotation(
                basePosition,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;

            Behaviour[] behaviours =
                root.GetComponentsInChildren<Behaviour>(true);

            for (int index = 0; index < behaviours.Length; index++)
            {
                behaviours[index].enabled = false;
            }

            NpcCutoutRig rig =
                root.GetComponentInChildren<NpcCutoutRig>(true);

            if (rig == null)
            {
                ErrorMessage =
                    "The shared Person prefab has no NpcCutoutRig.";
                UnityEngine.Object.DestroyImmediate(root);
                return false;
            }

            NpcAppearanceProfile profile =
                ScriptableObject.CreateInstance<NpcAppearanceProfile>();
            profile.name = "Population Audit Preview";
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.Configure(
                "Population Audit Preview",
                sample.Selection);

            rig.SetAppearancePreview(profile);
            rig.SetFacing(facing);

            result = new PreviewPerson(
                root,
                rig,
                profile,
                sample,
                displayIndex,
                basePosition,
                facing);
            CaptureBindPose(result);
            return true;
        }


        private void EnsureFacing(
            PreviewPerson person,
            NpcFacing facing)
        {
            if (person.Facing == facing)
            {
                return;
            }

            RestoreBindPose(person);
            person.Rig.SetFacing(facing);
            person.Facing = facing;
            CaptureBindPose(person);
        }


        private static void CaptureBindPose(
            PreviewPerson person)
        {
            person.BindPose.Clear();
            Transform[] transforms =
                person.Root.GetComponentsInChildren<Transform>(true);

            for (int index = 0; index < transforms.Length; index++)
            {
                person.BindPose.Add(
                    new PreviewTransformPose(transforms[index]));
            }
        }


        private static void RestoreBindPose(
            PreviewPerson person)
        {
            for (int index = 0; index < person.BindPose.Count; index++)
            {
                PreviewTransformPose pose = person.BindPose[index];

                if (pose.Transform == null)
                {
                    continue;
                }

                pose.Transform.localPosition = pose.LocalPosition;
                pose.Transform.localRotation = pose.LocalRotation;
                pose.Transform.localScale = pose.LocalScale;
            }
        }


        private void Render(
            Rect rect)
        {
            if (Event.current.type != EventType.Repaint
                || previewUtility == null)
            {
                return;
            }

            DestroyPreviewTexture();
            PositionCamera(rect);
            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.Render(true);
            previewTexture = previewUtility.EndPreview();
        }


        private void PositionCamera(
            Rect rect)
        {
            Bounds bounds = hasLineupBounds
                ? lineupBounds
                : new Bounds(Vector3.zero, new Vector3(4f, 4f, 0.1f));
            float aspect = Mathf.Max(
                0.1f,
                rect.width / Mathf.Max(1f, rect.height));
            float verticalExtent = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / aspect);

            previewUtility.camera.orthographicSize =
                Mathf.Max(1f, verticalExtent * 1.12f * zoom);
            previewUtility.camera.transform.position =
                bounds.center + Vector3.back * 10f;
            previewUtility.camera.transform.rotation = Quaternion.identity;
        }


        private void CalculateLineupBounds()
        {
            bool found = false;
            Bounds bounds = default;

            for (int personIndex = 0;
                 personIndex < people.Count;
                 personIndex++)
            {
                Renderer[] renderers = people[personIndex].Root
                    .GetComponentsInChildren<Renderer>(true);

                for (int rendererIndex = 0;
                     rendererIndex < renderers.Length;
                     rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];

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

            if (!found)
            {
                bounds = new Bounds(
                    Vector3.zero,
                    new Vector3(4f, 4f, 0.1f));
            }

            bounds.Expand(
                new Vector3(
                    DiamondRadiusX * 2f + 0.25f,
                    DiamondRadiusY * 2f + 0.4f,
                    0.1f));
            lineupBounds = bounds;
            hasLineupBounds = true;
        }


        private void DrawLabels(
            Rect rect)
        {
            if (previewUtility == null)
            {
                return;
            }

            for (int index = 0; index < people.Count; index++)
            {
                PreviewPerson person = people[index];
                Vector3 viewport = previewUtility.camera
                    .WorldToViewportPoint(
                        person.Root.transform.position
                        + Vector3.up * 2.05f);
                Vector2 point = new Vector2(
                    rect.x + viewport.x * rect.width,
                    rect.y + (1f - viewport.y) * rect.height);
                Rect labelRect = new Rect(
                    point.x - 35f,
                    point.y - 9f,
                    70f,
                    18f);
                string gender = person.Sample.Selection.Gender
                                == NpcPersonGender.Woman
                    ? "W"
                    : "M";

                GUI.Label(
                    labelRect,
                    $"{person.DisplayIndex + 1}  {gender}",
                    EditorStyles.centeredGreyMiniLabel);
            }
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


        private sealed class PreviewPerson : IDisposable
        {
            public PreviewPerson(
                GameObject root,
                NpcCutoutRig rig,
                NpcAppearanceProfile profile,
                NpcPopulationAuditSample sample,
                int displayIndex,
                Vector3 basePosition,
                NpcFacing facing)
            {
                Root = root;
                Rig = rig;
                Profile = profile;
                Sample = sample;
                DisplayIndex = displayIndex;
                BasePosition = basePosition;
                Facing = facing;
            }


            public GameObject Root { get; private set; }

            public NpcCutoutRig Rig { get; }

            public NpcAppearanceProfile Profile { get; private set; }

            public NpcPopulationAuditSample Sample { get; }

            public int DisplayIndex { get; }

            public Vector3 BasePosition { get; }

            public NpcFacing Facing { get; set; }

            public List<PreviewTransformPose> BindPose { get; } =
                new List<PreviewTransformPose>();


            public void Dispose()
            {
                BindPose.Clear();

                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                    Root = null;
                }

                if (Profile != null)
                {
                    UnityEngine.Object.DestroyImmediate(Profile);
                    Profile = null;
                }
            }
        }


        private readonly struct PreviewTransformPose
        {
            public PreviewTransformPose(
                Transform transform)
            {
                Transform = transform;
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
                LocalScale = transform.localScale;
            }


            public Transform Transform { get; }

            public Vector3 LocalPosition { get; }

            public Quaternion LocalRotation { get; }

            public Vector3 LocalScale { get; }
        }
    }
}
