using BigRetail.Departments.Unity;
using BigRetail.Departments.Unity.UI;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigRetail.Editor.Departments
{
    /// <summary>
    /// Creates the first authored department and wires the scene host without
    /// asking designers to manually relay a group of implementation references.
    /// Existing assets are deliberately preserved on repeated use.
    /// </summary>
    public static class DepartmentStarterSetupMenu
    {
        private const string DepartmentsFolder =
            "Assets/Design/Departments";

        private const string DryGoodsPath =
            DepartmentsFolder + "/DryGoodsDepartment.asset";

        private const string ProducePath =
            DepartmentsFolder + "/ProduceDepartment.asset";

        private const string FrozenFoodsPath =
            DepartmentsFolder + "/FrozenFoodsDepartment.asset";

        private const string CatalogPath =
            DepartmentsFolder + "/DepartmentDefinitionCatalog.asset";

        private const string IconsFolder =
            "Assets/Art/UI/Departments/Icons";

        private const string DepartmentsIconPath =
            IconsFolder + "/Icon_Departments.png";

        private const string DryGoodsIconPath =
            IconsFolder + "/Icon_Department_DryGoods.png";

        private const string ProduceIconPath =
            IconsFolder + "/Icon_Department_Produce.png";

        private const string FrozenFoodsIconPath =
            IconsFolder + "/Icon_Department_FrozenFoods.png";


        [MenuItem("Big Retail/Departments/Install Initial Department Planning")]
        private static void InstallInitialDepartmentPlanning()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(
                    "Exit Play Mode before installing Department Planning.");
                return;
            }

            EnsureFolder();
            ConfigureDepartmentIconImports();

            DepartmentDefinitionAsset[] initialDefinitions =
            {
                GetOrCreateDefinition(
                    DryGoodsPath,
                    "DRY_GOODS",
                    "Dry Goods",
                    DryGoodsIconPath),
                GetOrCreateDefinition(
                    ProducePath,
                    "PRODUCE",
                    "Produce",
                    ProduceIconPath),
                GetOrCreateDefinition(
                    FrozenFoodsPath,
                    "FROZEN_FOODS",
                    "Frozen Foods",
                    FrozenFoodsIconPath)
            };

            DepartmentDefinitionCatalogAsset catalog =
                GetOrCreateCatalog(initialDefinitions);

            GridMapHost mapHost =
                Object.FindAnyObjectByType<GridMapHost>(
                    FindObjectsInactive.Exclude);

            FoundationRuntimeHost foundationHost =
                Object.FindAnyObjectByType<FoundationRuntimeHost>(
                    FindObjectsInactive.Exclude);

            FloorRuntimeHost floorHost =
                Object.FindAnyObjectByType<FloorRuntimeHost>(
                    FindObjectsInactive.Exclude);

            if (mapHost == null
                || foundationHost == null
                || floorHost == null)
            {
                Debug.LogError(
                    "Department Planning requires GridMapHost, "
                    + "FoundationRuntimeHost, and FloorRuntimeHost in the "
                    + "open scene.");
                return;
            }

            DepartmentRuntimeHost departmentHost =
                mapHost.GetComponent<DepartmentRuntimeHost>();

            if (departmentHost == null)
            {
                departmentHost =
                    Undo.AddComponent<DepartmentRuntimeHost>(
                        mapHost.gameObject);
            }

            Undo.RecordObject(
                departmentHost,
                "Wire Dry Goods Department Planning");

            SerializedObject serializedHost =
                new SerializedObject(departmentHost);

            serializedHost.FindProperty("mapHost")
                .objectReferenceValue = mapHost;
            serializedHost.FindProperty("foundationRuntimeHost")
                .objectReferenceValue = foundationHost;
            serializedHost.FindProperty("floorRuntimeHost")
                .objectReferenceValue = floorHost;
            serializedHost.FindProperty("definitionAssets")
                .objectReferenceValue = catalog;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            WireDepartmentPicker(catalog);

            EditorSceneManager.MarkSceneDirty(mapHost.gameObject.scene);
            Selection.activeObject = departmentHost;

            Debug.Log(
                "Installed Department Planning with Dry Goods, Produce, "
                + "and Frozen Foods. Save Gameplay to preserve the scene "
                + "wiring.",
                departmentHost);
        }


        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Design"))
            {
                AssetDatabase.CreateFolder("Assets", "Design");
            }

            if (!AssetDatabase.IsValidFolder(DepartmentsFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Design",
                    "Departments");
            }
        }


        private static void WireDepartmentPicker(
            DepartmentDefinitionCatalogAsset catalog)
        {
            ConstructionToolbarDocumentHost documentHost =
                Object.FindAnyObjectByType<ConstructionToolbarDocumentHost>(
                    FindObjectsInactive.Exclude);

            ConstructionToolCoordinator toolCoordinator =
                Object.FindAnyObjectByType<ConstructionToolCoordinator>(
                    FindObjectsInactive.Exclude);

            if (documentHost == null || toolCoordinator == null)
            {
                Debug.LogWarning(
                    "Department Planning installed without its player UI because "
                    + "the ConstructionToolbarDocumentHost or "
                    + "ConstructionToolCoordinator was not found. Run this "
                    + "installer again after opening Gameplay.");
                return;
            }

            DepartmentDefinitionSelectionHost selectionHost =
                documentHost.GetComponent<DepartmentDefinitionSelectionHost>();

            if (selectionHost == null)
            {
                selectionHost =
                    Undo.AddComponent<DepartmentDefinitionSelectionHost>(
                        documentHost.gameObject);
            }

            SerializedObject serializedSelectionHost =
                new SerializedObject(selectionHost);

            serializedSelectionHost.FindProperty("definitionCatalog")
                .objectReferenceValue = catalog;
            serializedSelectionHost.ApplyModifiedPropertiesWithoutUndo();

            DepartmentPickerPresenter pickerPresenter =
                documentHost.GetComponent<DepartmentPickerPresenter>();

            if (pickerPresenter == null)
            {
                pickerPresenter =
                    Undo.AddComponent<DepartmentPickerPresenter>(
                        documentHost.gameObject);
            }

            SerializedObject serializedPickerPresenter =
                new SerializedObject(pickerPresenter);

            serializedPickerPresenter.FindProperty("documentHost")
                .objectReferenceValue = documentHost;
            serializedPickerPresenter.FindProperty("toolCoordinator")
                .objectReferenceValue = toolCoordinator;
            serializedPickerPresenter.FindProperty("selectionHost")
                .objectReferenceValue = selectionHost;
            serializedPickerPresenter.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(selectionHost);
            EditorUtility.SetDirty(pickerPresenter);
        }


        private static DepartmentDefinitionAsset
            GetOrCreateDefinition(
                string assetPath,
                string definitionId,
                string displayName,
                string iconPath)
        {
            DepartmentDefinitionAsset existing =
                AssetDatabase.LoadAssetAtPath<DepartmentDefinitionAsset>(
                    assetPath);

            if (existing != null)
            {
                EnsureDefinitionHasIcon(
                    existing,
                    iconPath);
                return existing;
            }

            DepartmentDefinitionAsset definition =
                ScriptableObject.CreateInstance<DepartmentDefinitionAsset>();

            SerializedObject serializedDefinition =
                new SerializedObject(definition);

            serializedDefinition.FindProperty("definitionId")
                .stringValue = definitionId;
            serializedDefinition.FindProperty("displayName")
                .stringValue = displayName;
            serializedDefinition.FindProperty("minimumCellCount")
                .intValue = 1;
            serializedDefinition.FindProperty("catalogIcon")
                .objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(definition, assetPath);
            return definition;
        }


        private static void EnsureDefinitionHasIcon(
            DepartmentDefinitionAsset definition,
            string iconPath)
        {
            SerializedObject serializedDefinition =
                new SerializedObject(definition);

            SerializedProperty icon =
                serializedDefinition.FindProperty("catalogIcon");

            if (icon.objectReferenceValue != null)
            {
                return;
            }

            Sprite importedIcon =
                AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            if (importedIcon == null)
            {
                return;
            }

            icon.objectReferenceValue = importedIcon;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }


        private static void ConfigureDepartmentIconImports()
        {
            ConfigureIconImport(DepartmentsIconPath);
            ConfigureIconImport(DryGoodsIconPath);
            ConfigureIconImport(ProduceIconPath);
            ConfigureIconImport(FrozenFoodsIconPath);
        }


        private static void ConfigureIconImport(
            string assetPath)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                return;
            }

            importer.textureType =
                TextureImporterType.Sprite;
            importer.spriteImportMode =
                SpriteImportMode.Single;
            TextureImporterSettings settings =
                new TextureImporterSettings();

            importer.ReadTextureSettings(settings);
            settings.spriteMeshType =
                SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape =
                false;

            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }


        private static DepartmentDefinitionCatalogAsset
            GetOrCreateCatalog(
                DepartmentDefinitionAsset[] initialDefinitions)
        {
            DepartmentDefinitionCatalogAsset existing =
                AssetDatabase.LoadAssetAtPath<
                    DepartmentDefinitionCatalogAsset>(CatalogPath);

            if (existing != null)
            {
                EnsureCatalogContains(
                    existing,
                    initialDefinitions);
                return existing;
            }

            DepartmentDefinitionCatalogAsset catalog =
                ScriptableObject.CreateInstance<
                    DepartmentDefinitionCatalogAsset>();

            SerializedObject serializedCatalog =
                new SerializedObject(catalog);

            SerializedProperty definitions =
                serializedCatalog.FindProperty("definitions");

            definitions.arraySize = initialDefinitions.Length;

            for (int index = 0;
                 index < initialDefinitions.Length;
                 index++)
            {
                definitions.GetArrayElementAtIndex(index)
                    .objectReferenceValue = initialDefinitions[index];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }


        private static void EnsureCatalogContains(
            DepartmentDefinitionCatalogAsset catalog,
            DepartmentDefinitionAsset[] initialDefinitions)
        {
            SerializedObject serializedCatalog =
                new SerializedObject(catalog);

            SerializedProperty definitions =
                serializedCatalog.FindProperty("definitions");

            for (int definitionIndex = 0;
                 definitionIndex < initialDefinitions.Length;
                 definitionIndex++)
            {
                DepartmentDefinitionAsset candidate =
                    initialDefinitions[definitionIndex];

                bool isRegistered = false;

                for (int catalogIndex = 0;
                     catalogIndex < definitions.arraySize;
                     catalogIndex++)
                {
                    if (definitions.GetArrayElementAtIndex(catalogIndex)
                        .objectReferenceValue == candidate)
                    {
                        isRegistered = true;
                        break;
                    }
                }

                if (isRegistered)
                {
                    continue;
                }

                int newIndex =
                    definitions.arraySize;

                definitions.arraySize++;
                definitions.GetArrayElementAtIndex(newIndex)
                    .objectReferenceValue = candidate;
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
