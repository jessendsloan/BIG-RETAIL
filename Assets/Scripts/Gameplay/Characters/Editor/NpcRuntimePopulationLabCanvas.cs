using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// A hidden runtime-like world made from the production Person prefab.
    /// Identity generation and path following use the same MonoBehaviours a
    /// future simulation spawner will call. Nothing is written to a scene.
    /// </summary>
    internal sealed class NpcRuntimePopulationLabCanvas : IDisposable
    {
        private const string PersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        private const float HorizontalSpacing = 1.35f;
        private const float VerticalSpacing = 2.25f;
        private const float RouteRadiusX = 0.5f;
        private const float RouteRadiusY = 0.32f;

        private readonly List<RuntimePerson> people =
            new List<RuntimePerson>();
        private readonly List<NpcRuntimePopulationSnapshot> snapshots =
            new List<NpcRuntimePopulationSnapshot>();

        private PreviewRenderUtility previewUtility;
        private Texture previewTexture;
        private Bounds populationBounds;
        private float zoom = 1f;


        public string ErrorMessage { get; private set; } = string.Empty;

        public int LiveCount => people.Count;

        public int InitializedCount { get; private set; }

        public int UniqueRecipeCount { get; private set; }

        public int RepeatedRecipeCount =>
            Mathf.Max(0, InitializedCount - UniqueRecipeCount);

        public IReadOnlyList<NpcRuntimePopulationSnapshot> Snapshots =>
            snapshots;

        public bool IsReady => previewUtility != null
                               && people.Count > 0;


        public void CreatePopulation(
            NpcPopulationDefinition definition,
            IReadOnlyList<NpcRuntimePopulationPlanEntry> plan)
        {
            Dispose();
            ErrorMessage = string.Empty;

            if (definition == null)
            {
                ErrorMessage = "Choose a Population Definition.";
                return;
            }

            if (!definition.TryValidate(out string validationFailure))
            {
                ErrorMessage = validationFailure;
                return;
            }

            GameObject personPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPath);

            if (personPrefab == null)
            {
                ErrorMessage =
                    "The shared Person prefab could not be found.";
                return;
            }

            if (plan == null || plan.Count == 0)
            {
                ErrorMessage = "The runtime population plan is empty.";
                return;
            }

            ConfigurePreviewUtility();
            int columns = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Sqrt(plan.Count * 1.35f)),
                1,
                12);
            int rows = Mathf.CeilToInt(plan.Count / (float)columns);
            float width = (columns - 1) * HorizontalSpacing;
            float height = (rows - 1) * VerticalSpacing;

            for (int index = 0; index < plan.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                Vector3 basePosition = new Vector3(
                    column * HorizontalSpacing - width * 0.5f,
                    height * 0.5f - row * VerticalSpacing,
                    0f);

                TryCreatePerson(
                    personPrefab,
                    definition,
                    plan[index],
                    basePosition);
            }

            CalculatePopulationBounds(width, height);
            CalculateRecipeCounts();

            if (people.Count == 0
                && string.IsNullOrWhiteSpace(ErrorMessage))
            {
                ErrorMessage =
                    "No Person instances could be initialized.";
            }
        }


        public void Tick(
            float deltaTime,
            float playbackSpeed,
            bool movementEnabled)
        {
            if (!IsReady || deltaTime <= 0f)
            {
                return;
            }

            float scaledDelta = Mathf.Min(
                0.1f,
                deltaTime * Mathf.Max(0f, playbackSpeed));

            for (int index = 0; index < people.Count; index++)
            {
                RuntimePerson person = people[index];

                if (movementEnabled)
                {
                    if (!person.PathFollower.IsMoving)
                    {
                        person.Root.transform.position = person.Route[0];
                        person.PathFollower.SetPath(person.Route, true);
                    }

                    person.PathFollower.Tick(scaledDelta);
                }
                else
                {
                    person.PathFollower.Stop();
                }

                if (person.Animator != null
                    && person.Animator.enabled
                    && person.Animator.runtimeAnimatorController != null)
                {
                    person.Animator.Update(scaledDelta);
                }
            }
        }


        public void Draw(
            Rect rect,
            bool showLabels)
        {
            Color background = new Color(0.07f, 0.085f, 0.11f, 1f);
            EditorGUI.DrawRect(rect, background);
            HandleZoom(rect);

            if (!IsReady)
            {
                GUI.Label(
                    rect,
                    string.IsNullOrWhiteSpace(ErrorMessage)
                        ? "Create a runtime population to begin."
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

            if (showLabels && people.Count <= 24)
            {
                DrawLabels(rect);
            }

            GUI.Label(
                new Rect(
                    rect.x + 8f,
                    rect.yMax - 24f,
                    rect.width - 16f,
                    18f),
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
            snapshots.Clear();
            InitializedCount = 0;
            UniqueRecipeCount = 0;
            DestroyPreviewTexture();

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }
        }


        private void ConfigurePreviewUtility()
        {
            previewUtility = new PreviewRenderUtility();
            previewUtility.camera.orthographic = true;
            previewUtility.camera.allowHDR = false;
            previewUtility.camera.allowMSAA = true;
            previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewUtility.camera.backgroundColor =
                new Color(0.07f, 0.085f, 0.11f, 1f);
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = 50f;
        }


        private void TryCreatePerson(
            GameObject personPrefab,
            NpcPopulationDefinition definition,
            NpcRuntimePopulationPlanEntry planEntry,
            Vector3 basePosition)
        {
            GameObject root =
                previewUtility.InstantiatePrefabInScene(personPrefab);

            if (root == null)
            {
                ErrorMessage =
                    "Unity could not create a hidden Person instance.";
                return;
            }

            SetHideFlagsRecursively(root);
            root.name = string.IsNullOrWhiteSpace(planEntry.PersistentId)
                ? $"Runtime Customer {planEntry.Index + 1}"
                : planEntry.PersistentId;
            root.transform.SetPositionAndRotation(
                basePosition,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;

            NpcPersonIdentity identity =
                root.GetComponentInChildren<NpcPersonIdentity>(true);
            NpcPathFollower pathFollower =
                root.GetComponentInChildren<NpcPathFollower>(true);
            Animator animator =
                root.GetComponentInChildren<Animator>(true);

            if (identity == null || pathFollower == null)
            {
                ErrorMessage =
                    "The shared Person prefab must contain both " +
                    "NpcPersonIdentity and NpcPathFollower.";
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }

            bool initialized = identity.TryInitialize(
                definition,
                planEntry.AppearanceSeed,
                planEntry.PersistentId,
                out string failureReason);
            NpcAppearanceSelection appearance = initialized
                ? identity.CurrentAppearance
                : null;
            NpcRuntimePopulationSnapshot snapshot =
                new NpcRuntimePopulationSnapshot(
                    planEntry,
                    appearance,
                    initialized ? string.Empty : failureReason);
            snapshots.Add(snapshot);

            if (!initialized)
            {
                ErrorMessage = failureReason;
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }

            Vector3[] route = CreateRoute(basePosition);
            root.transform.position = route[0];
            pathFollower.Configure(0.52f, 0.006f, 1.2f);
            pathFollower.SetPath(route, true);

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
            }

            people.Add(
                new RuntimePerson(
                    root,
                    identity,
                    pathFollower,
                    animator,
                    route,
                    planEntry));
            InitializedCount++;
        }


        private static Vector3[] CreateRoute(
            Vector3 center)
        {
            return new[]
            {
                center + new Vector3(-RouteRadiusX, 0f, 0f),
                center + new Vector3(0f, -RouteRadiusY, 0f),
                center + new Vector3(RouteRadiusX, 0f, 0f),
                center + new Vector3(0f, RouteRadiusY, 0f),
                center + new Vector3(-RouteRadiusX, 0f, 0f)
            };
        }


        private void CalculateRecipeCounts()
        {
            HashSet<string> recipes = new HashSet<string>();

            for (int index = 0; index < snapshots.Count; index++)
            {
                if (snapshots[index].IsValid)
                {
                    recipes.Add(snapshots[index].RecipeKey);
                }
            }

            UniqueRecipeCount = recipes.Count;
        }


        private void CalculatePopulationBounds(
            float width,
            float height)
        {
            populationBounds = new Bounds(
                Vector3.zero,
                new Vector3(
                    Mathf.Max(2f, width + 1.4f),
                    Mathf.Max(2.4f, height + 2.5f),
                    0.1f));
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
            float aspect = Mathf.Max(
                0.1f,
                rect.width / Mathf.Max(1f, rect.height));
            float verticalExtent = Mathf.Max(
                populationBounds.extents.y,
                populationBounds.extents.x / aspect);

            previewUtility.camera.orthographicSize =
                Mathf.Max(1f, verticalExtent * 1.05f * zoom);
            previewUtility.camera.transform.position =
                populationBounds.center + Vector3.back * 10f;
            previewUtility.camera.transform.rotation = Quaternion.identity;
        }


        private void DrawLabels(
            Rect rect)
        {
            for (int index = 0; index < people.Count; index++)
            {
                RuntimePerson person = people[index];
                Vector3 viewport = previewUtility.camera
                    .WorldToViewportPoint(
                        person.Root.transform.position + Vector3.up * 2.05f);
                Vector2 point = new Vector2(
                    rect.x + viewport.x * rect.width,
                    rect.y + (1f - viewport.y) * rect.height);
                Rect labelRect = new Rect(
                    point.x - 45f,
                    point.y - 9f,
                    90f,
                    18f);

                GUI.Label(
                    labelRect,
                    string.IsNullOrWhiteSpace(person.PlanEntry.PersistentId)
                        ? $"Visitor {person.PlanEntry.Index + 1}"
                        : $"Employee {person.PlanEntry.Index + 1}",
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
                0.5f,
                2.5f);
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


        private sealed class RuntimePerson : IDisposable
        {
            public RuntimePerson(
                GameObject root,
                NpcPersonIdentity identity,
                NpcPathFollower pathFollower,
                Animator animator,
                Vector3[] route,
                NpcRuntimePopulationPlanEntry planEntry)
            {
                Root = root;
                Identity = identity;
                PathFollower = pathFollower;
                Animator = animator;
                Route = route;
                PlanEntry = planEntry;
            }


            public GameObject Root { get; private set; }

            public NpcPersonIdentity Identity { get; }

            public NpcPathFollower PathFollower { get; }

            public Animator Animator { get; }

            public Vector3[] Route { get; }

            public NpcRuntimePopulationPlanEntry PlanEntry { get; }


            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                    Root = null;
                }
            }
        }
    }
}
