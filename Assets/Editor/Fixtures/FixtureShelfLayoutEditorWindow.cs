using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.View;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Editor.Fixtures
{
    [CustomEditor(typeof(FixtureDefinitionAsset))]
    public sealed class FixtureDefinitionAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Open Shelf Layout Editor", GUILayout.Height(30f)))
            {
                FixtureShelfLayoutEditorWindow.Open(
                    (FixtureDefinitionAsset)target);
            }
        }
    }


    /// <summary>
    /// Visual authoring surface for the product anchors stored on a fixture
    /// definition. The editor works in the fixture sprite's local coordinate
    /// space, which is the same space used by its shelf masks and runtime
    /// product renderers.
    /// </summary>
    public sealed class FixtureShelfLayoutEditorWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Fixtures/Shelf Layout Editor";
        private const string SeedMenuPath =
            "Big Retail/Fixtures/Seed Missing Merchandise Slot Layouts";
        private const float ToolbarHeight = 206f;
        private const float MinimumPreviewHeight = 280f;
        private const float AnchorRadius = 9f;

        private static readonly Color[] ShelfColors =
        {
            new Color(1f, 0.72f, 0.18f, 1f),
            new Color(0.25f, 0.85f, 1f, 1f),
            new Color(0.42f, 1f, 0.48f, 1f),
            new Color(1f, 0.45f, 0.75f, 1f)
        };

        [SerializeField]
        private FixtureDefinitionAsset fixture;

        [SerializeField]
        private FixtureSide localDisplaySide = FixtureSide.South;

        [SerializeField]
        private FixtureArtworkDirection artworkDirection =
            FixtureArtworkDirection.North;

        private int selectedAnchorIndex = -1;
        private int dragUndoGroup = -1;


        private enum FixtureArtworkDirection
        {
            North = 0,
            East = 1,
            South = 2,
            West = 3
        }


        [MenuItem(MenuPath)]
        public static void Open()
        {
            Open(Selection.activeObject as FixtureDefinitionAsset);
        }


        public static void Open(
            FixtureDefinitionAsset definition)
        {
            FixtureShelfLayoutEditorWindow window =
                GetWindow<FixtureShelfLayoutEditorWindow>();
            window.titleContent = new GUIContent("Shelf Layout");
            window.minSize = new Vector2(620f, 620f);

            if (definition != null)
            {
                window.fixture = definition;
                window.ResolveFirstAuthoredView();
            }

            window.Show();
            window.Focus();
        }


        [MenuItem(SeedMenuPath)]
        public static void SeedMissingLayouts()
        {
            string[] assetGuids =
                AssetDatabase.FindAssets("t:FixtureDefinitionAsset");
            int seededViewCount = 0;

            for (int index = 0; index < assetGuids.Length; index++)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(assetGuids[index]);
                FixtureDefinitionAsset definition =
                    AssetDatabase.LoadAssetAtPath<FixtureDefinitionAsset>(
                        assetPath);

                if (definition != null)
                {
                    seededViewCount +=
                        SeedAllMissingViews(
                            definition,
                            recordUndo: true);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                seededViewCount > 0
                    ? $"Seeded {seededViewCount} fixture shelf-layout view(s)."
                    : "All visible fixture shelf layouts were already authored.");
        }


        private void OnGUI()
        {
            DrawToolbar();

            if (fixture == null)
            {
                EditorGUILayout.HelpBox(
                    "Choose a Fixture Definition asset to begin.",
                    MessageType.Info);
                return;
            }

            if (!TryGetDisplayFace(
                    fixture,
                    localDisplaySide,
                    out FixtureDisplayFaceDefinition displayFace,
                    out string configurationError))
            {
                EditorGUILayout.HelpBox(
                    configurationError,
                    MessageType.Warning);
                return;
            }

            ResolveCanonicalView(
                artworkDirection,
                out FixtureOrientation worldOrientation,
                out IsometricViewOrientation viewOrientation);
            Sprite fixtureSprite =
                fixture.GetSprite(
                    worldOrientation,
                    viewOrientation);
            IReadOnlyList<Sprite> shelfMasks =
                fixture.GetMerchandisingShelfMasks(
                    localDisplaySide,
                    worldOrientation,
                    viewOrientation);
            IReadOnlyList<Vector2> anchors =
                fixture.GetMerchandisingProductAnchors(
                    localDisplaySide,
                    worldOrientation,
                    viewOrientation);
            int expectedAnchorCount =
                displayFace.ShelfRunCount
                * displayFace.FrontageUnitsPerRun;

            DrawStatus(
                fixtureSprite,
                shelfMasks.Count,
                anchors.Count,
                expectedAnchorCount);

            if (fixtureSprite == null)
            {
                EditorGUILayout.HelpBox(
                    "The selected artwork direction has no fixture sprite.",
                    MessageType.Warning);
                return;
            }

            Rect previewArea =
                GUILayoutUtility.GetRect(
                    320f,
                    Mathf.Max(
                        MinimumPreviewHeight,
                        position.height - ToolbarHeight),
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true));
            DrawPreview(
                previewArea,
                fixtureSprite,
                shelfMasks,
                anchors,
                displayFace.FrontageUnitsPerRun);

            DrawSelectedAnchorField(
                anchors,
                expectedAnchorCount,
                displayFace.FrontageUnitsPerRun);
        }


        private void DrawToolbar()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Merchandise Shelf Layout",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drag the numbered anchors over the fixture. Rows are "
                + "stored top-to-bottom and slots visually left-to-right. "
                + "Runtime inventory still decides whether each anchor "
                + "shows the 1, 2, or 3-unit sprite.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            FixtureDefinitionAsset nextFixture =
                (FixtureDefinitionAsset)EditorGUILayout.ObjectField(
                    "Fixture",
                    fixture,
                    typeof(FixtureDefinitionAsset),
                    allowSceneObjects: false);
            FixtureSide nextSide =
                (FixtureSide)EditorGUILayout.EnumPopup(
                    "Display Face",
                    localDisplaySide);
            FixtureArtworkDirection nextDirection =
                (FixtureArtworkDirection)EditorGUILayout.EnumPopup(
                    "Artwork View",
                    artworkDirection);

            if (EditorGUI.EndChangeCheck())
            {
                bool fixtureChanged = nextFixture != fixture;
                fixture = nextFixture;
                localDisplaySide = nextSide;
                artworkDirection = nextDirection;
                selectedAnchorIndex = -1;

                if (fixtureChanged && fixture != null)
                {
                    ResolveFirstAuthoredView();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(fixture == null))
                {
                    if (GUILayout.Button("Seed This View From Shelf Masks"))
                    {
                        SeedSelectedView();
                    }

                    if (GUILayout.Button("Seed Every Missing View"))
                    {
                        int count =
                            SeedAllMissingViews(
                                fixture,
                                recordUndo: true);
                        ShowNotification(
                            new GUIContent(
                                count > 0
                                    ? $"Seeded {count} view(s)"
                                    : "Nothing needed seeding"));
                    }

                    if (GUILayout.Button("Clear This View"))
                    {
                        ClearSelectedView();
                    }

                    if (GUILayout.Button("Save"))
                    {
                        AssetDatabase.SaveAssets();
                        ShowNotification(new GUIContent("Fixture layout saved"));
                    }
                }
            }
        }


        private void DrawStatus(
            Sprite fixtureSprite,
            int shelfMaskCount,
            int anchorCount,
            int expectedAnchorCount)
        {
            string spriteName =
                fixtureSprite != null
                    ? fixtureSprite.name
                    : "<missing>";
            MessageType statusType =
                anchorCount == expectedAnchorCount
                    ? MessageType.Info
                    : MessageType.Warning;

            EditorGUILayout.HelpBox(
                $"Sprite: {spriteName}  |  Shelf masks: {shelfMaskCount}  |  "
                + $"Anchors: {anchorCount}/{expectedAnchorCount}",
                statusType);
        }


        private void DrawPreview(
            Rect availableArea,
            Sprite fixtureSprite,
            IReadOnlyList<Sprite> shelfMasks,
            IReadOnlyList<Vector2> anchors,
            int frontageUnitsPerShelf)
        {
            EditorGUI.DrawRect(
                availableArea,
                new Color(0.075f, 0.082f, 0.09f, 1f));

            Rect previewRect =
                FitSpriteRect(
                    availableArea,
                    fixtureSprite);
            DrawSprite(fixtureSprite, previewRect);
            DrawShelfMasks(
                fixtureSprite,
                previewRect,
                shelfMasks);

            if (anchors == null || anchors.Count == 0)
            {
                GUI.Label(
                    previewRect,
                    "Seed this view to create draggable anchors.",
                    CenteredPreviewLabelStyle());
                return;
            }

            for (int index = 0; index < anchors.Count; index++)
            {
                DrawAnchor(
                    fixtureSprite,
                    previewRect,
                    anchors[index],
                    index,
                    frontageUnitsPerShelf);
            }
        }


        private void DrawAnchor(
            Sprite fixtureSprite,
            Rect previewRect,
            Vector2 localPosition,
            int anchorIndex,
            int frontageUnitsPerShelf)
        {
            Vector2 guiPosition =
                LocalToGui(
                    fixtureSprite,
                    previewRect,
                    localPosition);
            Rect hitRect =
                new Rect(
                    guiPosition.x - AnchorRadius,
                    guiPosition.y - AnchorRadius,
                    AnchorRadius * 2f,
                    AnchorRadius * 2f);
            int controlId =
                GUIUtility.GetControlID(
                    "FixtureShelfAnchor".GetHashCode()
                        + anchorIndex,
                    FocusType.Passive,
                    hitRect);
            Event currentEvent = Event.current;

            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0
                        && hitRect.Contains(currentEvent.mousePosition))
                    {
                        selectedAnchorIndex = anchorIndex;
                        GUIUtility.hotControl = controlId;
                        dragUndoGroup = Undo.GetCurrentGroup();
                        Undo.SetCurrentGroupName(
                            "Move Merchandise Shelf Anchor");
                        Undo.RecordObject(
                            fixture,
                            "Move Merchandise Shelf Anchor");
                        currentEvent.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        Vector2 clampedMouse =
                            new Vector2(
                                Mathf.Clamp(
                                    currentEvent.mousePosition.x,
                                    previewRect.xMin,
                                    previewRect.xMax),
                                Mathf.Clamp(
                                    currentEvent.mousePosition.y,
                                    previewRect.yMin,
                                    previewRect.yMax));
                        SetAnchor(
                            anchorIndex,
                            GuiToLocal(
                                fixtureSprite,
                                previewRect,
                                clampedMouse),
                            recordUndo: false);
                        currentEvent.Use();
                        Repaint();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;

                        if (dragUndoGroup >= 0)
                        {
                            Undo.CollapseUndoOperations(dragUndoGroup);
                            dragUndoGroup = -1;
                        }

                        currentEvent.Use();
                    }

                    break;
            }

            int shelfIndex =
                frontageUnitsPerShelf > 0
                    ? anchorIndex / frontageUnitsPerShelf
                    : 0;
            int slotIndex =
                frontageUnitsPerShelf > 0
                    ? anchorIndex % frontageUnitsPerShelf
                    : anchorIndex;
            Color anchorColor =
                ShelfColors[shelfIndex % ShelfColors.Length];

            Handles.BeginGUI();
            Handles.color = new Color(0f, 0f, 0f, 0.75f);
            Handles.DrawSolidDisc(
                guiPosition,
                Vector3.forward,
                AnchorRadius + 2f);
            Handles.color =
                anchorIndex == selectedAnchorIndex
                    ? Color.white
                    : anchorColor;
            Handles.DrawSolidDisc(
                guiPosition,
                Vector3.forward,
                AnchorRadius);
            Handles.EndGUI();

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };
            GUI.Label(
                hitRect,
                $"{shelfIndex + 1}.{slotIndex + 1}",
                labelStyle);
        }


        private void DrawSelectedAnchorField(
            IReadOnlyList<Vector2> anchors,
            int expectedAnchorCount,
            int frontageUnitsPerShelf)
        {
            if (selectedAnchorIndex < 0
                || selectedAnchorIndex >= anchors.Count
                || anchors.Count != expectedAnchorCount)
            {
                return;
            }

            int shelfIndex =
                selectedAnchorIndex / frontageUnitsPerShelf;
            int slotIndex =
                selectedAnchorIndex % frontageUnitsPerShelf;
            EditorGUI.BeginChangeCheck();
            Vector2 nextPosition =
                EditorGUILayout.Vector2Field(
                    $"Selected: Shelf {shelfIndex + 1}, Slot {slotIndex + 1}",
                    anchors[selectedAnchorIndex]);

            if (EditorGUI.EndChangeCheck())
            {
                SetAnchor(
                    selectedAnchorIndex,
                    nextPosition,
                    recordUndo: true);
            }
        }


        private void SeedSelectedView()
        {
            if (fixture == null)
            {
                return;
            }

            bool seeded =
                SeedView(
                    fixture,
                    localDisplaySide,
                    artworkDirection,
                    overwriteExisting: true,
                    recordUndo: true);

            if (!seeded)
            {
                ShowNotification(
                    new GUIContent("This view has no complete shelf masks"));
            }
            else
            {
                selectedAnchorIndex = -1;
            }
        }


        private void ClearSelectedView()
        {
            if (fixture == null)
            {
                return;
            }

            SerializedObject serializedFixture =
                new SerializedObject(fixture);
            SerializedProperty anchors =
                FindAnchorArray(
                    serializedFixture,
                    localDisplaySide,
                    artworkDirection,
                    createLayoutSet: false);

            if (anchors == null || anchors.arraySize == 0)
            {
                return;
            }

            Undo.RecordObject(fixture, "Clear Merchandise Shelf Anchors");
            anchors.arraySize = 0;
            serializedFixture.ApplyModifiedProperties();
            EditorUtility.SetDirty(fixture);
            selectedAnchorIndex = -1;
        }


        private void SetAnchor(
            int anchorIndex,
            Vector2 positionValue,
            bool recordUndo)
        {
            SerializedObject serializedFixture =
                new SerializedObject(fixture);
            SerializedProperty anchors =
                FindAnchorArray(
                    serializedFixture,
                    localDisplaySide,
                    artworkDirection,
                    createLayoutSet: false);

            if (anchors == null
                || anchorIndex < 0
                || anchorIndex >= anchors.arraySize)
            {
                return;
            }

            if (recordUndo)
            {
                Undo.RecordObject(
                    fixture,
                    "Edit Merchandise Shelf Anchor");
            }

            anchors.GetArrayElementAtIndex(anchorIndex).vector2Value =
                positionValue;
            serializedFixture.ApplyModifiedProperties();
            EditorUtility.SetDirty(fixture);
        }


        private void ResolveFirstAuthoredView()
        {
            if (fixture == null)
            {
                return;
            }

            FixtureDefinition definition;

            try
            {
                definition = fixture.CreateDomainDefinition();
            }
            catch (Exception)
            {
                return;
            }

            if (definition.MerchandisingProfile.DisplayFaceCount > 0)
            {
                localDisplaySide =
                    definition.MerchandisingProfile
                        .GetDisplayFace(0)
                        .LocalSide;
            }

            for (FixtureArtworkDirection direction =
                     FixtureArtworkDirection.North;
                 direction <= FixtureArtworkDirection.West;
                 direction++)
            {
                ResolveCanonicalView(
                    direction,
                    out FixtureOrientation worldOrientation,
                    out IsometricViewOrientation viewOrientation);

                if (fixture.GetMerchandisingShelfMasks(
                        localDisplaySide,
                        worldOrientation,
                        viewOrientation).Count > 0)
                {
                    artworkDirection = direction;
                    return;
                }
            }
        }


        private static int SeedAllMissingViews(
            FixtureDefinitionAsset definition,
            bool recordUndo)
        {
            if (definition == null)
            {
                return 0;
            }

            FixtureDefinition domainDefinition;

            try
            {
                domainDefinition = definition.CreateDomainDefinition();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Could not seed shelf anchors for '{definition.name}': "
                    + exception.Message,
                    definition);
                return 0;
            }

            int seededViewCount = 0;
            FixtureMerchandisingProfile profile =
                domainDefinition.MerchandisingProfile;

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureSide side =
                    profile.GetDisplayFace(faceIndex).LocalSide;

                for (FixtureArtworkDirection direction =
                         FixtureArtworkDirection.North;
                     direction <= FixtureArtworkDirection.West;
                     direction++)
                {
                    if (SeedView(
                            definition,
                            side,
                            direction,
                            overwriteExisting: false,
                            recordUndo))
                    {
                        seededViewCount++;
                    }
                }
            }

            return seededViewCount;
        }


        private static bool SeedView(
            FixtureDefinitionAsset definition,
            FixtureSide displaySide,
            FixtureArtworkDirection direction,
            bool overwriteExisting,
            bool recordUndo)
        {
            if (!TryGetDisplayFace(
                    definition,
                    displaySide,
                    out FixtureDisplayFaceDefinition displayFace,
                    out _))
            {
                return false;
            }

            ResolveCanonicalView(
                direction,
                out FixtureOrientation worldOrientation,
                out IsometricViewOrientation viewOrientation);
            IReadOnlyList<Sprite> shelfMasks =
                definition.GetMerchandisingShelfMasks(
                    displaySide,
                    worldOrientation,
                    viewOrientation);

            if (shelfMasks.Count != displayFace.ShelfRunCount)
            {
                return false;
            }

            SerializedObject serializedFixture =
                new SerializedObject(definition);
            SerializedProperty anchors =
                FindAnchorArray(
                    serializedFixture,
                    displaySide,
                    direction,
                    createLayoutSet: true);
            int expectedCount =
                displayFace.ShelfRunCount
                * displayFace.FrontageUnitsPerRun;

            if (anchors == null
                || (!overwriteExisting
                    && anchors.arraySize == expectedCount))
            {
                return false;
            }

            Vector2[] seededAnchors = new Vector2[expectedCount];

            for (int shelfIndex = 0;
                 shelfIndex < shelfMasks.Count;
                 shelfIndex++)
            {
                if (!FixtureShelfMaskGeometry.TryCreate(
                        shelfMasks[shelfIndex],
                        out FixtureShelfMaskGeometry geometry))
                {
                    return false;
                }

                for (int slotIndex = 0;
                     slotIndex < displayFace.FrontageUnitsPerRun;
                     slotIndex++)
                {
                    seededAnchors[
                            shelfIndex
                            * displayFace.FrontageUnitsPerRun
                            + slotIndex] =
                        FixtureViewSystem
                            .ResolveAuthoredDisplayProductCenter(
                                geometry,
                                slotIndex,
                                displayFace.FrontageUnitsPerRun);
                }
            }

            if (recordUndo)
            {
                Undo.RecordObject(
                    definition,
                    "Seed Merchandise Shelf Anchors");
            }

            anchors.arraySize = seededAnchors.Length;

            for (int index = 0; index < seededAnchors.Length; index++)
            {
                anchors.GetArrayElementAtIndex(index).vector2Value =
                    seededAnchors[index];
            }

            serializedFixture.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            return true;
        }


        private static SerializedProperty FindAnchorArray(
            SerializedObject serializedFixture,
            FixtureSide displaySide,
            FixtureArtworkDirection direction,
            bool createLayoutSet)
        {
            SerializedProperty layoutSets =
                serializedFixture.FindProperty(
                    "merchandisingSlotLayoutSets");

            if (layoutSets == null)
            {
                return null;
            }

            SerializedProperty layoutSet = null;

            for (int index = 0;
                 index < layoutSets.arraySize;
                 index++)
            {
                SerializedProperty candidate =
                    layoutSets.GetArrayElementAtIndex(index);

                if (candidate.FindPropertyRelative(
                        "localDisplaySide").intValue
                    == (int)displaySide)
                {
                    layoutSet = candidate;
                    break;
                }
            }

            if (layoutSet == null && createLayoutSet)
            {
                int addedIndex = layoutSets.arraySize;
                layoutSets.arraySize++;
                layoutSet =
                    layoutSets.GetArrayElementAtIndex(addedIndex);
                layoutSet.FindPropertyRelative(
                        "localDisplaySide").intValue =
                    (int)displaySide;
                layoutSet.FindPropertyRelative(
                        "northProductAnchors").arraySize = 0;
                layoutSet.FindPropertyRelative(
                        "eastProductAnchors").arraySize = 0;
                layoutSet.FindPropertyRelative(
                        "southProductAnchors").arraySize = 0;
                layoutSet.FindPropertyRelative(
                        "westProductAnchors").arraySize = 0;
            }

            return layoutSet?.FindPropertyRelative(
                GetAnchorPropertyName(direction));
        }


        private static string GetAnchorPropertyName(
            FixtureArtworkDirection direction)
        {
            return direction switch
            {
                FixtureArtworkDirection.North => "northProductAnchors",
                FixtureArtworkDirection.East => "eastProductAnchors",
                FixtureArtworkDirection.South => "southProductAnchors",
                FixtureArtworkDirection.West => "westProductAnchors",
                _ => "northProductAnchors"
            };
        }


        private static bool TryGetDisplayFace(
            FixtureDefinitionAsset definition,
            FixtureSide side,
            out FixtureDisplayFaceDefinition displayFace,
            out string error)
        {
            displayFace = null;
            error = string.Empty;

            if (definition == null)
            {
                error = "Choose a Fixture Definition asset.";
                return false;
            }

            try
            {
                FixtureDefinition domainDefinition =
                    definition.CreateDomainDefinition();

                if (!domainDefinition.MerchandisingProfile.TryGetDisplayFace(
                        side,
                        out displayFace))
                {
                    error =
                        $"{definition.DisplayName} does not merchandise from "
                        + $"its {side} side.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }


        private static void ResolveCanonicalView(
            FixtureArtworkDirection direction,
            out FixtureOrientation worldOrientation,
            out IsometricViewOrientation viewOrientation)
        {
            worldOrientation = FixtureOrientation.North;
            viewOrientation =
                direction switch
                {
                    FixtureArtworkDirection.North =>
                        IsometricViewOrientation.North,
                    FixtureArtworkDirection.East =>
                        IsometricViewOrientation.West,
                    FixtureArtworkDirection.South =>
                        IsometricViewOrientation.South,
                    FixtureArtworkDirection.West =>
                        IsometricViewOrientation.East,
                    _ => IsometricViewOrientation.North
                };
        }


        private static Rect FitSpriteRect(
            Rect availableArea,
            Sprite sprite)
        {
            float aspect =
                sprite.rect.width
                / Mathf.Max(sprite.rect.height, 1f);
            Rect padded =
                new Rect(
                    availableArea.x + 20f,
                    availableArea.y + 20f,
                    Mathf.Max(1f, availableArea.width - 40f),
                    Mathf.Max(1f, availableArea.height - 40f));
            float width = padded.width;
            float height = width / aspect;

            if (height > padded.height)
            {
                height = padded.height;
                width = height * aspect;
            }

            return new Rect(
                padded.center.x - width * 0.5f,
                padded.center.y - height * 0.5f,
                width,
                height);
        }


        private static void DrawSprite(
            Sprite sprite,
            Rect previewRect)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect textureRect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            Rect uv =
                new Rect(
                    textureRect.x / texture.width,
                    textureRect.y / texture.height,
                    textureRect.width / texture.width,
                    textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(
                previewRect,
                texture,
                uv,
                alphaBlend: true);
        }


        private static void DrawShelfMasks(
            Sprite fixtureSprite,
            Rect previewRect,
            IReadOnlyList<Sprite> shelfMasks)
        {
            if (shelfMasks == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Handles.BeginGUI();

            for (int shelfIndex = 0;
                 shelfIndex < shelfMasks.Count;
                 shelfIndex++)
            {
                Sprite mask = shelfMasks[shelfIndex];

                if (mask == null)
                {
                    continue;
                }

                Vector2[] vertices = mask.vertices;
                ushort[] triangles = mask.triangles;
                Color shelfColor =
                    ShelfColors[shelfIndex % ShelfColors.Length];
                Handles.color =
                    new Color(
                        shelfColor.r,
                        shelfColor.g,
                        shelfColor.b,
                        0.13f);

                for (int index = 0;
                     index <= triangles.Length - 3;
                     index += 3)
                {
                    Handles.DrawAAConvexPolygon(
                        LocalToGui(
                            fixtureSprite,
                            previewRect,
                            vertices[triangles[index]]),
                        LocalToGui(
                            fixtureSprite,
                            previewRect,
                            vertices[triangles[index + 1]]),
                        LocalToGui(
                            fixtureSprite,
                            previewRect,
                            vertices[triangles[index + 2]]));
                }
            }

            Handles.EndGUI();
        }


        private static Vector2 LocalToGui(
            Sprite sprite,
            Rect previewRect,
            Vector2 localPosition)
        {
            Rect localRect = GetSpriteLocalRect(sprite);
            float normalizedX =
                Mathf.InverseLerp(
                    localRect.xMin,
                    localRect.xMax,
                    localPosition.x);
            float normalizedY =
                Mathf.InverseLerp(
                    localRect.yMin,
                    localRect.yMax,
                    localPosition.y);

            return new Vector2(
                Mathf.Lerp(previewRect.xMin, previewRect.xMax, normalizedX),
                Mathf.Lerp(previewRect.yMax, previewRect.yMin, normalizedY));
        }


        private static Vector2 GuiToLocal(
            Sprite sprite,
            Rect previewRect,
            Vector2 guiPosition)
        {
            Rect localRect = GetSpriteLocalRect(sprite);
            float normalizedX =
                Mathf.InverseLerp(
                    previewRect.xMin,
                    previewRect.xMax,
                    guiPosition.x);
            float normalizedY =
                Mathf.InverseLerp(
                    previewRect.yMax,
                    previewRect.yMin,
                    guiPosition.y);

            return new Vector2(
                Mathf.Lerp(localRect.xMin, localRect.xMax, normalizedX),
                Mathf.Lerp(localRect.yMin, localRect.yMax, normalizedY));
        }


        private static Rect GetSpriteLocalRect(
            Sprite sprite)
        {
            float pixelsPerUnit =
                Mathf.Max(sprite.pixelsPerUnit, 0.0001f);
            float minimumX = -sprite.pivot.x / pixelsPerUnit;
            float minimumY = -sprite.pivot.y / pixelsPerUnit;

            return new Rect(
                minimumX,
                minimumY,
                sprite.rect.width / pixelsPerUnit,
                sprite.rect.height / pixelsPerUnit);
        }


        private static GUIStyle CenteredPreviewLabelStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
            };
        }
    }
}
