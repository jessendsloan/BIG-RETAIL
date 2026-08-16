using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Defines the appearance pools available to each gameplay population.
    /// Character previewing and animation testing intentionally live in
    /// separate tools.
    /// </summary>
    public sealed class NpcPopulationDefinitionsWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Population/Definitions";

        private const string DefinitionFolder =
            "Assets/Art/Characters/Appearance/Population Definitions";

        private NpcAppearanceCatalog catalog;
        private NpcPopulationDefinition selectedDefinition;
        private SerializedObject serializedDefinition;
        private NpcPersonGender selectedGenderPool =
            NpcPersonGender.Man;
        private Vector2 scrollPosition;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<NpcPopulationDefinitionsWindow>(
                "Population Definitions");
        }


        private void OnEnable()
        {
            FindCatalog();
            TryUseSelection();
            EnsureSelectedDefinition();
        }


        private void OnSelectionChange()
        {
            TryUseSelection();
            Repaint();
        }


        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Big Retail Population Definitions",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Define each population type, how often men and women " +
                "appear, and the separate Body, Skin, Outfit, and Hair " +
                "options the simulation may use for each. " +
                "This tool does not create or preview individual people.",
                MessageType.Info);

            if (catalog == null)
            {
                DrawMissingCatalog();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawPopulationSelector();

            if (selectedDefinition != null)
            {
                EditorGUILayout.Space(10f);
                DrawSelectedDefinition();
            }

            EditorGUILayout.EndScrollView();
        }


        private void DrawMissingCatalog()
        {
            EditorGUILayout.HelpBox(
                "No appearance catalog was found. Repair the starter " +
                "content once to restore the population definition library.",
                MessageType.Warning);

            if (!GUILayout.Button("Repair Starter Content"))
            {
                return;
            }

            NpcPopulationStarterFactory.CreateOrUpdateStarterCatalog();
            FindCatalog();
            EnsureSelectedDefinition();
        }


        private void DrawPopulationSelector()
        {
            EditorGUILayout.LabelField(
                "Population Types",
                EditorStyles.boldLabel);

            List<NpcPopulationDefinition> definitions =
                GetDefinitions();

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This library has no population types yet.",
                    MessageType.Warning);
            }
            else
            {
                string[] labels = new string[definitions.Count];
                int selectedIndex = 0;

                for (int index = 0; index < definitions.Count; index++)
                {
                    labels[index] = definitions[index].DisplayName;

                    if (definitions[index] == selectedDefinition)
                    {
                        selectedIndex = index;
                    }
                }

                EditorGUI.BeginChangeCheck();
                selectedIndex = EditorGUILayout.Popup(
                    "Population Type",
                    selectedIndex,
                    labels);

                if (EditorGUI.EndChangeCheck())
                {
                    SelectDefinition(definitions[selectedIndex]);
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Population Type"))
            {
                CreateDefinition();
            }

            using (new EditorGUI.DisabledScope(
                       selectedDefinition == null))
            {
                if (GUILayout.Button("Duplicate Selected Type"))
                {
                    DuplicateDefinition();
                }

                if (GUILayout.Button(
                        "Show Asset",
                        GUILayout.Width(90f)))
                {
                    Selection.activeObject = selectedDefinition;
                    EditorGUIUtility.PingObject(selectedDefinition);
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawSelectedDefinition()
        {
            if (serializedDefinition == null
                || serializedDefinition.targetObject != selectedDefinition)
            {
                serializedDefinition =
                    new SerializedObject(selectedDefinition);
            }

            serializedDefinition.Update();

            EditorGUILayout.LabelField(
                "Selected Population",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("displayName"),
                new GUIContent("Name"));

            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("role"),
                new GUIContent(
                    "Behavior Family",
                    "Customer and Employee are broad gameplay families. " +
                    "Specific population types may share the same family."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Gender Distribution",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("menWeight"),
                    new GUIContent(
                        "Men Weight",
                        "Relative likelihood of generating a man. Zero " +
                        "disables men for this population."));

                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("womenWeight"),
                    new GUIContent(
                        "Women Weight",
                        "Relative likelihood of generating a woman. Zero " +
                        "disables women for this population."));
            }

            EditorGUILayout.LabelField(
                "Weights are relative: 1 / 1 is an even split, while " +
                "1 / 3 generates women three times as often as men.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Appearance Pools",
                EditorStyles.boldLabel);

            int selectedPoolIndex = GUILayout.Toolbar(
                selectedGenderPool == NpcPersonGender.Man ? 0 : 1,
                new[] { "Men", "Women" });

            selectedGenderPool = selectedPoolIndex == 0
                ? NpcPersonGender.Man
                : NpcPersonGender.Woman;

            SerializedProperty appearancePool =
                serializedDefinition.FindProperty(
                    selectedGenderPool == NpcPersonGender.Man
                        ? "menAppearance"
                        : "womenAppearance");

            int selectedWeight = selectedGenderPool == NpcPersonGender.Man
                ? serializedDefinition.FindProperty("menWeight").intValue
                : serializedDefinition.FindProperty("womenWeight").intValue;

            if (selectedWeight == 0)
            {
                EditorGUILayout.HelpBox(
                    $"{GetGenderLabel(selectedGenderPool)} are disabled " +
                    "for generation. You can still prepare their " +
                    "appearance pool here, then raise its weight when " +
                    "ready.",
                    MessageType.Info);
            }

            DrawPool(
                "Body Types",
                appearancePool,
                "bodies",
                typeof(NpcBodySilhouette));
            DrawPool(
                "Skin Palettes",
                appearancePool,
                "skins",
                typeof(NpcSkinPalette));
            DrawPool(
                "Outfit Sets",
                appearancePool,
                "outfits",
                typeof(NpcOutfitSet));
            DrawPool(
                "Hair Sets",
                appearancePool,
                "hair",
                typeof(NpcHairSet));

            if (serializedDefinition.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedDefinition);
                RegisterDefinitionAssets();
            }

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.LabelField(
                "Changes save automatically to the selected population " +
                "definition. Removing an option here does not delete its " +
                "underlying art asset.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawPool(
            string title,
            SerializedProperty appearancePool,
            string propertyName,
            Type assetType)
        {
            SerializedProperty pool =
                appearancePool.FindPropertyRelative(propertyName);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"{title} ({pool.arraySize})",
                EditorStyles.boldLabel);

            int removeIndex = -1;

            for (int index = 0; index < pool.arraySize; index++)
            {
                SerializedProperty choice =
                    pool.GetArrayElementAtIndex(index);
                SerializedProperty asset =
                    choice.FindPropertyRelative("asset");

                EditorGUILayout.BeginHorizontal();
                asset.objectReferenceValue =
                    EditorGUILayout.ObjectField(
                        asset.objectReferenceValue,
                        assetType,
                        false);

                if (GUILayout.Button(
                        "Remove",
                        GUILayout.Width(70f)))
                {
                    removeIndex = index;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                pool.DeleteArrayElementAtIndex(removeIndex);
            }

            if (GUILayout.Button($"Add {GetSingularTitle(title)}"))
            {
                int newIndex = pool.arraySize;
                pool.arraySize++;

                SerializedProperty newChoice =
                    pool.GetArrayElementAtIndex(newIndex);

                newChoice.FindPropertyRelative("asset")
                    .objectReferenceValue = null;
                newChoice.FindPropertyRelative("weight")
                    .intValue = 1;
            }

            EditorGUILayout.EndVertical();
        }


        private void DrawValidation()
        {
            if (selectedDefinition.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    $"{selectedDefinition.DisplayName} is ready for the " +
                    "simulation.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }


        private void CreateDefinition()
        {
            EnsureDefinitionFolder();

            NpcPopulationDefinition definition =
                CreateInstance<NpcPopulationDefinition>();

            definition.Configure(
                "New Population Type",
                NpcCharacterRole.Customer,
                new NpcPopulationAppearancePool(),
                new NpcPopulationAppearancePool(),
                0,
                0);

            string path = AssetDatabase.GenerateUniqueAssetPath(
                DefinitionFolder + "/NewPopulationType.asset");

            AssetDatabase.CreateAsset(definition, path);
            Undo.RegisterCreatedObjectUndo(
                definition,
                "Add Population Type");

            Undo.RecordObject(catalog, "Add Population Type");
            catalog.AddDefinition(definition);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            SelectDefinition(definition);
            Selection.activeObject = definition;
        }


        private void DuplicateDefinition()
        {
            if (selectedDefinition == null)
            {
                return;
            }

            EnsureDefinitionFolder();

            NpcPopulationDefinition duplicate =
                Instantiate(selectedDefinition);

            duplicate.name = selectedDefinition.name + "Copy";
            duplicate.SetMetadata(
                selectedDefinition.DisplayName + " Copy",
                selectedDefinition.Role);

            string path = AssetDatabase.GenerateUniqueAssetPath(
                DefinitionFolder + "/" + duplicate.name + ".asset");

            AssetDatabase.CreateAsset(duplicate, path);
            Undo.RegisterCreatedObjectUndo(
                duplicate,
                "Duplicate Population Type");

            Undo.RecordObject(catalog, "Duplicate Population Type");
            catalog.AddDefinition(duplicate);
            catalog.RegisterAssetsFrom(duplicate);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            SelectDefinition(duplicate);
            Selection.activeObject = duplicate;
        }


        private void RegisterDefinitionAssets()
        {
            if (catalog == null || selectedDefinition == null)
            {
                return;
            }

            Undo.RecordObject(catalog, "Register Appearance Assets");

            if (!catalog.RegisterAssetsFrom(selectedDefinition))
            {
                return;
            }

            EditorUtility.SetDirty(catalog);
        }


        private void FindCatalog()
        {
            string[] guids =
                AssetDatabase.FindAssets("t:NpcAppearanceCatalog");

            catalog = guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(
                    AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }


        private void TryUseSelection()
        {
            if (Selection.activeObject is NpcAppearanceCatalog selectedCatalog)
            {
                catalog = selectedCatalog;
                EnsureSelectedDefinition();
                return;
            }

            if (Selection.activeObject
                is NpcPopulationDefinition definition)
            {
                SelectDefinition(definition);
            }
        }


        private void EnsureSelectedDefinition()
        {
            if (catalog == null)
            {
                SelectDefinition(null);
                return;
            }

            List<NpcPopulationDefinition> definitions =
                GetDefinitions();

            if (selectedDefinition == null
                || !definitions.Contains(selectedDefinition))
            {
                SelectDefinition(
                    definitions.Count > 0
                        ? definitions[0]
                        : null);
            }
        }


        private List<NpcPopulationDefinition> GetDefinitions()
        {
            List<NpcPopulationDefinition> definitions =
                new List<NpcPopulationDefinition>();

            if (catalog?.Definitions == null)
            {
                return definitions;
            }

            for (int index = 0;
                 index < catalog.Definitions.Count;
                 index++)
            {
                NpcPopulationDefinition definition =
                    catalog.Definitions[index];

                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            return definitions;
        }


        private void SelectDefinition(
            NpcPopulationDefinition definition)
        {
            if (definition != null
                && !definition.HasGenderAppearancePools)
            {
                Undo.RecordObject(
                    definition,
                    "Migrate Population Appearance Pools");

                if (definition.EnsureGenderAppearancePools())
                {
                    EditorUtility.SetDirty(definition);
                }
            }

            selectedDefinition = definition;
            serializedDefinition = definition != null
                ? new SerializedObject(definition)
                : null;
        }


        private static string GetSingularTitle(
            string title)
        {
            if (title.EndsWith("ies", StringComparison.Ordinal))
            {
                return title.Substring(0, title.Length - 3) + "y";
            }

            if (title.EndsWith("s", StringComparison.Ordinal))
            {
                return title.Substring(0, title.Length - 1);
            }

            return title;
        }


        private static string GetGenderLabel(
            NpcPersonGender gender)
        {
            return gender == NpcPersonGender.Woman
                ? "Women"
                : "Men";
        }


        private static void EnsureDefinitionFolder()
        {
            if (AssetDatabase.IsValidFolder(DefinitionFolder))
            {
                return;
            }

            const string Parent =
                "Assets/Art/Characters/Appearance";

            AssetDatabase.CreateFolder(
                Parent,
                "Population Definitions");
        }
    }
}
