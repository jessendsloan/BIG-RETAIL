using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Presents the canonical 18-by-2 art intake as a compact table
    /// instead of exposing raw serialized list plumbing.
    /// </summary>
    [CustomEditor(typeof(NpcRigArtKit))]
    public sealed class NpcRigArtKitEditor :
        UnityEditor.Editor
    {
        private const float PartLabelWidth = 118f;

        private SerializedProperty partsProperty;


        private void OnEnable()
        {
            partsProperty =
                serializedObject.FindProperty(
                    "parts");
        }


        public override void OnInspectorGUI()
        {
            NpcRigArtKit artKit =
                (NpcRigArtKit)target;

            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "One row is one intentional body-piece target. " +
                "SouthWest mirrors SouthEast; NorthWest mirrors " +
                "NorthEast.",
                MessageType.Info);

            DrawColumnHeaders();
            DrawPartRows();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            DrawDirectionProgress(
                artKit,
                NpcAuthoredDirection.SouthEast,
                "SouthEast");
            DrawDirectionProgress(
                artKit,
                NpcAuthoredDirection.NorthEast,
                "NorthEast");

            EditorGUILayout.Space(8f);

            if (GUILayout.Button(
                    "Populate From Canonical Folders"))
            {
                NpcRigArtKitTools
                    .PopulateFromCanonicalFolders(
                        artKit);
            }

            if (GUILayout.Button(
                    "Apply To Canonical NPC Rig Prefab"))
            {
                NpcRigArtKitTools
                    .ApplyToCanonicalRig(
                        artKit);
            }

            if (GUILayout.Button(
                    "Validate Art Kit"))
            {
                NpcRigArtKitTools
                    .ValidateArtKit(
                        artKit);
            }

            if (GUILayout.Button(
                    "Normalize Canonical 18 Slots"))
            {
                Undo.RecordObject(
                    artKit,
                    "Normalize NPC Art Kit");

                artKit.NormalizeCanonicalLayout();
                EditorUtility.SetDirty(
                    artKit);
                serializedObject.Update();
            }
        }


        private static void DrawColumnHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    "Body Part",
                    EditorStyles.boldLabel,
                    GUILayout.Width(
                        PartLabelWidth));

                GUILayout.Label(
                    "SouthEast",
                    EditorStyles.boldLabel);

                GUILayout.Label(
                    "NorthEast",
                    EditorStyles.boldLabel);
            }
        }

        private void DrawPartRows()
        {
            if (partsProperty == null
                || !partsProperty.isArray)
            {
                EditorGUILayout.HelpBox(
                    "The serialized part list could not be read.",
                    MessageType.Error);
                return;
            }

            for (int index = 0;
                 index < partsProperty.arraySize;
                 index++)
            {
                SerializedProperty partProperty =
                    partsProperty.GetArrayElementAtIndex(
                        index);

                SerializedProperty idProperty =
                    partProperty.FindPropertyRelative(
                        "id");

                SerializedProperty southEastProperty =
                    partProperty.FindPropertyRelative(
                        "southEastSprite");

                SerializedProperty northEastProperty =
                    partProperty.FindPropertyRelative(
                        "northEastSprite");

                string partName =
                    idProperty != null
                        ? idProperty.enumDisplayNames[
                            idProperty.enumValueIndex]
                        : $"Part {index}";

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        partName,
                        GUILayout.Width(
                            PartLabelWidth));

                    EditorGUILayout.PropertyField(
                        southEastProperty,
                        GUIContent.none);

                    EditorGUILayout.PropertyField(
                        northEastProperty,
                        GUIContent.none);
                }
            }
        }

        private static void DrawDirectionProgress(
            NpcRigArtKit artKit,
            NpcAuthoredDirection direction,
            string label)
        {
            int assignedCount =
                artKit.CountAssignedSprites(
                    direction);

            int expectedCount =
                NpcRigDefinition.ExpectedPartCount;

            Rect progressRect =
                EditorGUILayout.GetControlRect(
                    false,
                    EditorGUIUtility.singleLineHeight);

            EditorGUI.ProgressBar(
                progressRect,
                (float)assignedCount / expectedCount,
                $"{label}: {assignedCount}/{expectedCount}");
        }
    }

    /// <summary>
    /// Creates, populates, validates, and applies the canonical NPC
    /// art-kit asset without relying on handwritten prefab edits.
    /// </summary>
    public static class NpcRigArtKitTools
    {
        public const string CanonicalArtRoot =
            "Assets/Art/Characters/CanonicalEmployee";

        public const string SouthEastFolder =
            CanonicalArtRoot + "/SouthEast";

        public const string NorthEastFolder =
            CanonicalArtRoot + "/NorthEast";

        public const string CanonicalArtKitPath =
            CanonicalArtRoot +
            "/CanonicalEmployeeArtKit.asset";

        public const string CanonicalRigPrefabPath =
            "Assets/Prefabs/Characters/Prototype/" +
            "CanonicalNpcRig.prefab";

        private const string CreateMenuPath =
            "Big Retail/Characters/Art Kit/" +
            "Create Canonical Employee Art Kit";

        private const string PopulateMenuPath =
            "Big Retail/Characters/Art Kit/" +
            "Populate Selected Kit From Folders";

        private const string ApplyMenuPath =
            "Big Retail/Characters/Art Kit/" +
            "Apply Selected Kit To Canonical Rig";

        private const string ValidateMenuPath =
            "Big Retail/Characters/Art Kit/" +
            "Validate Selected Art Kit";


        [MenuItem(CreateMenuPath)]
        public static void CreateCanonicalArtKit()
        {
            EnsureAssetFolder(
                SouthEastFolder);
            EnsureAssetFolder(
                NorthEastFolder);

            NpcRigArtKit existingArtKit =
                AssetDatabase.LoadAssetAtPath<NpcRigArtKit>(
                    CanonicalArtKitPath);

            if (existingArtKit != null)
            {
                SelectAndPing(
                    existingArtKit);

                Debug.Log(
                    $"Canonical NPC art kit already exists at " +
                    $"'{CanonicalArtKitPath}'.");
                return;
            }

            NpcRigArtKit artKit =
                ScriptableObject.CreateInstance<NpcRigArtKit>();

            artKit.NormalizeCanonicalLayout();

            AssetDatabase.CreateAsset(
                artKit,
                CanonicalArtKitPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectAndPing(
                artKit);

            Debug.Log(
                $"Created the canonical 36-sprite NPC art intake at " +
                $"'{CanonicalArtKitPath}'.");
        }

        [MenuItem(PopulateMenuPath)]
        private static void PopulateSelectedArtKit()
        {
            PopulateFromCanonicalFolders(
                Selection.activeObject as NpcRigArtKit);
        }

        [MenuItem(PopulateMenuPath, true)]
        private static bool CanPopulateSelectedArtKit()
        {
            return Selection.activeObject
                is NpcRigArtKit;
        }

        [MenuItem(ApplyMenuPath)]
        private static void ApplySelectedArtKit()
        {
            ApplyToCanonicalRig(
                Selection.activeObject as NpcRigArtKit);
        }

        [MenuItem(ApplyMenuPath, true)]
        private static bool CanApplySelectedArtKit()
        {
            return Selection.activeObject
                is NpcRigArtKit;
        }

        [MenuItem(ValidateMenuPath)]
        private static void ValidateSelectedArtKit()
        {
            ValidateArtKit(
                Selection.activeObject as NpcRigArtKit);
        }

        [MenuItem(ValidateMenuPath, true)]
        private static bool CanValidateSelectedArtKit()
        {
            return Selection.activeObject
                is NpcRigArtKit;
        }


        public static void PopulateFromCanonicalFolders(
            NpcRigArtKit artKit)
        {
            if (artKit == null)
            {
                Debug.LogError(
                    "Select an NPC rig art kit first.");
                return;
            }

            Undo.RecordObject(
                artKit,
                "Populate NPC Art Kit");

            artKit.NormalizeCanonicalLayout();

            int southEastMatches =
                PopulateDirection(
                    artKit,
                    NpcAuthoredDirection.SouthEast,
                    SouthEastFolder);

            int northEastMatches =
                PopulateDirection(
                    artKit,
                    NpcAuthoredDirection.NorthEast,
                    NorthEastFolder);

            EditorUtility.SetDirty(
                artKit);
            AssetDatabase.SaveAssets();

            SelectAndPing(
                artKit);

            Debug.Log(
                $"Populated NPC art kit from canonical folders. " +
                $"SouthEast matched {southEastMatches}/" +
                $"{NpcRigDefinition.ExpectedPartCount}; " +
                $"NorthEast matched {northEastMatches}/" +
                $"{NpcRigDefinition.ExpectedPartCount}.");
        }

        public static void ApplyToCanonicalRig(
            NpcRigArtKit artKit)
        {
            if (artKit == null)
            {
                Debug.LogError(
                    "Select an NPC rig art kit first.");
                return;
            }

            GameObject prefabAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CanonicalRigPrefabPath);

            if (prefabAsset == null)
            {
                Debug.LogError(
                    $"Canonical NPC rig prefab was not found at " +
                    $"'{CanonicalRigPrefabPath}'.");
                return;
            }

            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(
                    CanonicalRigPrefabPath);

            try
            {
                NpcCutoutRig rig =
                    prefabRoot.GetComponent<NpcCutoutRig>();

                if (rig == null)
                {
                    Debug.LogError(
                        "The canonical prefab has no NpcCutoutRig " +
                        "component.");
                    return;
                }

                rig.SetArtKit(
                    artKit);
                EditorUtility.SetDirty(
                    rig);

                PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    CanonicalRigPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(
                    prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectAndPing(
                prefabAsset);

            Debug.Log(
                $"Applied '{artKit.name}' to the canonical NPC rig. " +
                "Missing sprites continue to display the mannequin " +
                "placeholders.");
        }

        public static void ValidateArtKit(
            NpcRigArtKit artKit)
        {
            if (artKit == null)
            {
                Debug.LogError(
                    "Select an NPC rig art kit first.");
                return;
            }

            if (!artKit.TryValidateStructure(
                    out string structureFailure))
            {
                Debug.LogError(
                    $"NPC art-kit structure is invalid: " +
                    $"{structureFailure}");
                return;
            }

            bool southEastIsComplete =
                artKit.TryValidateDirection(
                    NpcAuthoredDirection.SouthEast,
                    out string southEastFailure);

            bool northEastIsComplete =
                artKit.TryValidateDirection(
                    NpcAuthoredDirection.NorthEast,
                    out string northEastFailure);

            if (southEastIsComplete
                && northEastIsComplete)
            {
                Debug.Log(
                    "NPC art kit is complete: 18 SouthEast and 18 " +
                    "NorthEast sprites are uniquely assigned.");
                return;
            }

            string missingSummary =
                BuildMissingSummary(
                    artKit);

            Debug.LogWarning(
                $"NPC art kit is structurally valid but incomplete. " +
                $"SouthEast: " +
                $"{(southEastIsComplete ? "complete" : southEastFailure)} " +
                $"NorthEast: " +
                $"{(northEastIsComplete ? "complete" : northEastFailure)} " +
                $"{missingSummary}");
        }


        private static int PopulateDirection(
            NpcRigArtKit artKit,
            NpcAuthoredDirection direction,
            string folderPath)
        {
            Dictionary<string, List<Sprite>> spritesByName =
                FindSpritesByName(
                    folderPath);

            int matchCount = 0;

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                string expectedName =
                    definition.Id.ToString();

                if (!spritesByName.TryGetValue(
                        expectedName,
                        out List<Sprite> matches))
                {
                    continue;
                }

                if (matches.Count != 1)
                {
                    Debug.LogError(
                        $"Expected one sprite named '{expectedName}' " +
                        $"inside '{folderPath}' but found " +
                        $"{matches.Count}. That part was not changed.");
                    continue;
                }

                if (artKit.TrySetSprite(
                        definition.Id,
                        direction,
                        matches[0]))
                {
                    matchCount++;
                }
            }

            return matchCount;
        }

        private static Dictionary<string, List<Sprite>>
            FindSpritesByName(
                string folderPath)
        {
            Dictionary<string, List<Sprite>> spritesByName =
                new Dictionary<string, List<Sprite>>(
                    StringComparer.OrdinalIgnoreCase);

            string[] assetGuids =
                AssetDatabase.FindAssets(
                    "t:Sprite",
                    new[]
                    {
                        folderPath
                    });

            HashSet<string> visitedPaths =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            for (int guidIndex = 0;
                 guidIndex < assetGuids.Length;
                 guidIndex++)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(
                        assetGuids[guidIndex]);

                if (!visitedPaths.Add(
                        assetPath))
                {
                    continue;
                }

                UnityEngine.Object[] assets =
                    AssetDatabase.LoadAllAssetsAtPath(
                        assetPath);

                for (int assetIndex = 0;
                     assetIndex < assets.Length;
                     assetIndex++)
                {
                    if (!(assets[assetIndex]
                          is Sprite sprite))
                    {
                        continue;
                    }

                    if (!spritesByName.TryGetValue(
                            sprite.name,
                            out List<Sprite> matches))
                    {
                        matches =
                            new List<Sprite>();

                        spritesByName.Add(
                            sprite.name,
                            matches);
                    }

                    matches.Add(
                        sprite);
                }
            }

            return spritesByName;
        }

        private static string BuildMissingSummary(
            NpcRigArtKit artKit)
        {
            List<NpcRigPartId> southEastMissing =
                new List<NpcRigPartId>();

            List<NpcRigPartId> northEastMissing =
                new List<NpcRigPartId>();

            artKit.GetMissingParts(
                NpcAuthoredDirection.SouthEast,
                southEastMissing);

            artKit.GetMissingParts(
                NpcAuthoredDirection.NorthEast,
                northEastMissing);

            return
                $"Missing SE [{string.Join(", ", southEastMissing)}]. " +
                $"Missing NE [{string.Join(", ", northEastMissing)}].";
        }

        private static void SelectAndPing(
            UnityEngine.Object asset)
        {
            Selection.activeObject =
                asset;

            EditorGUIUtility.PingObject(
                asset);
        }

        private static void EnsureAssetFolder(
            string folderPath)
        {
            string[] pathParts =
                folderPath.Split('/');

            string currentPath =
                pathParts[0];

            for (int index = 1;
                 index < pathParts.Length;
                 index++)
            {
                string nextPath =
                    $"{currentPath}/{pathParts[index]}";

                if (!AssetDatabase.IsValidFolder(
                        nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        pathParts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
