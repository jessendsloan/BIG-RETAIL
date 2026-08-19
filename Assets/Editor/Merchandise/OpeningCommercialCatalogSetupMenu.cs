using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Purchasing.Unity.UI;
using BigRetail.Simulation.Time.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace BigRetail.Editor.Merchandise
{
    /// <summary>
    /// Creates the accepted opening brands, products, suppliers, offers, and
    /// an isolated Purchasing UI lab. Rebuilding preserves assigned artwork.
    /// </summary>
    public static class OpeningCommercialCatalogSetupMenu
    {
        private const string MenuRoot = "Big Retail/Merchandise/";
        private const string DesignRoot = "Assets/Design";
        private const string MerchandiseRoot = DesignRoot + "/Merchandise";
        private const string BrandFolder = MerchandiseRoot + "/Brands";
        private const string ProductFolder = MerchandiseRoot + "/Products";
        private const string MerchandiseCatalogFolder =
            MerchandiseRoot + "/Catalogs";
        private const string PurchasingRoot = DesignRoot + "/Purchasing";
        private const string SupplierFolder = PurchasingRoot + "/Suppliers";
        private const string OfferFolder = PurchasingRoot + "/Offers";
        private const string PurchasingCatalogFolder =
            PurchasingRoot + "/Catalogs";
        private const string BrandCatalogPath =
            MerchandiseCatalogFolder + "/OpeningBrandCatalog.asset";
        private const string ProductCatalogPath =
            MerchandiseCatalogFolder + "/OpeningProductCatalog.asset";
        private const string SupplierCatalogPath =
            PurchasingCatalogFolder + "/OpeningSupplierCatalog.asset";
        private const string OfferCatalogPath =
            PurchasingCatalogFolder + "/OpeningSupplierOfferCatalog.asset";
        private const string CommercialCatalogPath =
            PurchasingCatalogFolder + "/OpeningCommercialCatalog.asset";
        private const string PurchasingUxmlPath =
            "Assets/UI/Purchasing/PC/PurchasingWorkspace.uxml";
        private const string CommercialDirectoryUxmlPath =
            "Assets/UI/Purchasing/PC/CommercialDirectory.uxml";
        private const string PanelSettingsPath =
            "Assets/UI/Construction/PC/ConstructionToolbarPanelSettings.asset";
        private const string LabSceneFolder = "Assets/Scenes/Labs";
        private const string PurchasingLabScenePath =
            LabSceneFolder + "/PurchasingWorkspaceLab.unity";
        private const string CommercialDirectoryLabScenePath =
            LabSceneFolder + "/CommercialDirectoryLab.unity";
        private const string GameplayScenePath =
            "Assets/Scenes/Gameplay.unity";


        private static readonly BrandSeed[] BrandSeeds =
        {
            new BrandSeed(
                "BRIGHT",
                "Bright Beverage Co.",
                "Ubiquitous mainstream beverage company",
                "BrightBeverage",
                new Color(0.87f, 0.20f, 0.15f, 1f)),
            new BrandSeed(
                "CLEARSPRING",
                "ClearSpring",
                "Clean, simple hydration brand",
                "ClearSpring",
                new Color(0.13f, 0.51f, 0.73f, 1f)),
            new BrandSeed(
                "RIDGEWAY",
                "Ridgeway Snacks",
                "Bold mainstream savory snack company",
                "RidgewaySnacks",
                new Color(0.91f, 0.43f, 0.10f, 1f)),
            new BrandSeed(
                "CHOCOMAX",
                "ChocoMax",
                "Large commercial chocolate and candy brand",
                "ChocoMax",
                new Color(0.40f, 0.20f, 0.12f, 1f)),
            new BrandSeed(
                "SUNBURST",
                "Sunburst Candy Co.",
                "Colorful non-chocolate candy company",
                "SunburstCandy",
                new Color(0.88f, 0.22f, 0.45f, 1f)),
            new BrandSeed(
                "HOMESTEAD",
                "Homestead Foods",
                "Dependable household grocery staples",
                "HomesteadFoods",
                new Color(0.46f, 0.54f, 0.24f, 1f)),
            new BrandSeed(
                "CRUNCH-O",
                "Crunch-O",
                "Cheerful mainstream breakfast cereal brand",
                "CrunchO",
                new Color(0.91f, 0.66f, 0.12f, 1f)),
            new BrandSeed(
                "CLEANMAX",
                "CleanMax Home",
                "Practical household cleaning and consumables",
                "CleanMaxHome",
                new Color(0.13f, 0.55f, 0.57f, 1f)),
            new BrandSeed(
                "SPARK",
                "Spark",
                "Mass-market battery and electrical convenience brand",
                "Spark",
                new Color(0.91f, 0.73f, 0.08f, 1f)),
            new BrandSeed(
                "FRESHMINT",
                "FreshMint",
                "Clean and approachable mass-market oral care",
                "FreshMint",
                new Color(0.20f, 0.67f, 0.50f, 1f))
        };


        private static readonly ProductSeed[] ProductSeeds =
        {
            new ProductSeed(
                "BRIGHT-COLA-20OZ", "Bright Cola", 0, "Cola", "BEVERAGES",
                "20 oz Bottle", "BrightCola"),
            new ProductSeed(
                "CLEARSPRING-WATER-20OZ", "ClearSpring Pure Water", 1,
                "Bottled Water", "BEVERAGES", "20 oz Bottle",
                "ClearSpringWater"),
            new ProductSeed(
                "RIDGEWAY-ORIGINAL-CHIPS-SINGLE",
                "Ridgeway Original Potato Chips", 2, "Potato Chips", "SNACKS",
                "Single Bag", "RidgewayChips"),
            new ProductSeed(
                "CHOCOMAX-MILK-CHOCOLATE-BAR",
                "ChocoMax Milk Chocolate", 3, "Chocolate Bar", "SNACKS", "Bar",
                "ChocoMaxMilkChocolate"),
            new ProductSeed(
                "SUNBURST-FRUIT-CHEWS-PACK", "Sunburst Fruit Chews", 4,
                "Fruit Candy", "SNACKS", "Pack", "SunburstFruitChews"),
            new ProductSeed(
                "HOMESTEAD-WHITE-BREAD-LOAF", "Homestead White Bread", 5,
                "White Bread", "GROCERY", "Loaf", "HomesteadWhiteBread"),
            new ProductSeed(
                "HOMESTEAD-WHOLE-MILK-JUG", "Homestead Whole Milk", 5,
                "Whole Milk", "GROCERY", "Jug", "HomesteadWholeMilk"),
            new ProductSeed(
                "CRUNCH-O-CORN-FLAKES-BOX", "Crunch-O Corn Flakes", 6,
                "Corn Flakes", "GROCERY", "Box", "CrunchOCornFlakes"),
            new ProductSeed(
                "CLEANMAX-PAPER-TOWELS-ROLL", "CleanMax Paper Towels", 7,
                "Paper Towels", "HOUSEHOLD", "Roll", "CleanMaxPaperTowels"),
            new ProductSeed(
                "CLEANMAX-DISH-SOAP-BOTTLE", "CleanMax Dish Soap", 7,
                "Dish Soap", "HOUSEHOLD", "Bottle", "CleanMaxDishSoap"),
            new ProductSeed(
                "SPARK-ALKALINE-BATTERIES-4PK", "Spark Alkaline Batteries", 8,
                "Batteries", "HOUSEHOLD", "4-Pack", "SparkBatteries"),
            new ProductSeed(
                "FRESHMINT-TOOTHPASTE-TUBE", "FreshMint Toothpaste", 9,
                "Toothpaste", "PERSONAL-CARE", "Tube", "FreshMintToothpaste")
        };

        private static readonly long[] OpeningRetailUnitPricesCents =
        {
            199, 149, 249, 179, 169, 299,
            399, 449, 249, 399, 699, 399
        };


        private static readonly SupplierSeed[] SupplierSeeds =
        {
            new SupplierSeed(
                "BIG", "BIG Wholesale", "Broadline emergency wholesaler",
                "Small packs, no minimum, and same-day certainty at the highest price.",
                0, SupplierDeliveryKind.SameDay, 3, SupplierWeekday.None,
                "BIGWholesale", new Color(0.82f, 0.20f, 0.15f, 1f)),
            new SupplierSeed(
                "CENTRAL", "Central Grocery Supply", "Regional grocery distributor",
                "Larger packs and better margins when the store plans for tomorrow.",
                10000, SupplierDeliveryKind.NextDay, 0, SupplierWeekday.None,
                "CentralGrocery", new Color(0.12f, 0.42f, 0.50f, 1f)),
            new SupplierSeed(
                "BEACON", "Beacon Beverage Distribution", "Beverage route specialist",
                "The best beverage economics on a fixed Tuesday and Friday route.",
                7500, SupplierDeliveryKind.WeeklyRoute, 0,
                SupplierWeekday.Tuesday | SupplierWeekday.Friday,
                "BeaconBeverage", new Color(0.20f, 0.53f, 0.29f, 1f))
        };


        private static readonly OfferSeed[] OfferSeeds =
        {
            new OfferSeed(0, 0, 12, 1200),
            new OfferSeed(1, 0, 24, 2100),
            new OfferSeed(2, 0, 24, 1920),
            new OfferSeed(0, 1, 12, 840),
            new OfferSeed(1, 1, 24, 1440),
            new OfferSeed(2, 1, 24, 1296),
            new OfferSeed(0, 2, 12, 1140),
            new OfferSeed(1, 2, 24, 2040),
            new OfferSeed(0, 3, 24, 1680),
            new OfferSeed(1, 3, 48, 2976),
            new OfferSeed(0, 4, 12, 900),
            new OfferSeed(1, 4, 24, 1584),
            new OfferSeed(0, 5, 8, 1200),
            new OfferSeed(1, 5, 16, 2160),
            new OfferSeed(0, 6, 6, 1080),
            new OfferSeed(1, 6, 12, 1920),
            new OfferSeed(0, 7, 8, 1440),
            new OfferSeed(1, 7, 12, 1920),
            new OfferSeed(0, 8, 12, 1200),
            new OfferSeed(1, 8, 24, 2112),
            new OfferSeed(0, 9, 12, 1440),
            new OfferSeed(1, 9, 24, 2544),
            new OfferSeed(0, 10, 12, 2880),
            new OfferSeed(0, 11, 12, 1800)
        };


        [MenuItem(MenuRoot + "Build Opening Commercial Catalog")]
        public static void BuildOpeningCommercialCatalog()
        {
            CommercialCatalogAsset catalog = BuildCatalogAssets();
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
            Debug.Log(
                "Built the opening catalog: 10 brands, 12 products, "
                + "3 suppliers, and 24 supplier offers.",
                catalog);
        }

        [MenuItem(MenuRoot + "Open Purchasing Workspace Lab")]
        public static void OpenPurchasingWorkspaceLab()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CommercialCatalogAsset catalog = BuildCatalogAssets();
            BuildPurchasingLabScene(catalog);
            EditorSceneManager.OpenScene(
                PurchasingLabScenePath,
                OpenSceneMode.Single);
        }

        [MenuItem(MenuRoot + "Open Commercial Directory Lab")]
        public static void OpenCommercialDirectoryLab()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CommercialCatalogAsset catalog = BuildCatalogAssets();
            BuildCommercialDirectoryLabScene(catalog);
            EditorSceneManager.OpenScene(
                CommercialDirectoryLabScenePath,
                OpenSceneMode.Single);
        }

        [MenuItem(MenuRoot + "Integrate Purchasing Into Gameplay")]
        public static void IntegratePurchasingIntoGameplay()
        {
            BuildCatalogAssets();
            Scene scene = EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);
            CommercialCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(
                    CommercialCatalogPath);
            ProductCatalogAsset productCatalog =
                AssetDatabase.LoadAssetAtPath<ProductCatalogAsset>(
                    ProductCatalogPath);
            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    PurchasingUxmlPath);
            PanelSettings panelSettings =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    PanelSettingsPath);

            if (catalog == null
                || productCatalog == null
                || visualTree == null
                || panelSettings == null)
            {
                throw new InvalidOperationException(
                    "Gameplay Purchasing integration is missing one or more authored assets.");
            }

            FixturePlanogramRuntimeHost planogramHost =
                UnityEngine.Object.FindAnyObjectByType<
                    FixturePlanogramRuntimeHost>(
                    FindObjectsInactive.Include);
            SimulationTimeRuntimeHost timeHost =
                UnityEngine.Object.FindAnyObjectByType<
                    SimulationTimeRuntimeHost>(
                    FindObjectsInactive.Include);
            ConstructionToolbarDocumentHost toolbarDocumentHost =
                UnityEngine.Object.FindAnyObjectByType<
                    ConstructionToolbarDocumentHost>(
                    FindObjectsInactive.Include);
            ConstructionToolCoordinator toolCoordinator =
                UnityEngine.Object.FindAnyObjectByType<
                    ConstructionToolCoordinator>(
                    FindObjectsInactive.Include);
            FixtureMerchandisingInspectorPresenter fixtureInspector =
                UnityEngine.Object.FindAnyObjectByType<
                    FixtureMerchandisingInspectorPresenter>(
                    FindObjectsInactive.Include);
            GridMapHost mapHost =
                UnityEngine.Object.FindAnyObjectByType<GridMapHost>(
                    FindObjectsInactive.Include);
            IsometricViewHost viewHost =
                UnityEngine.Object.FindAnyObjectByType<IsometricViewHost>(
                    FindObjectsInactive.Include);
            GameObject mapVisuals =
                FindSceneGameObject(scene, "MapVIsuals");
            Tilemap coordinateTilemap =
                mapVisuals != null
                    ? mapVisuals.GetComponent<Tilemap>()
                    : null;

            if (planogramHost == null
                || timeHost == null
                || toolbarDocumentHost == null
                || toolCoordinator == null
                || fixtureInspector == null
                || mapHost == null
                || viewHost == null
                || coordinateTilemap == null)
            {
                throw new InvalidOperationException(
                    "Gameplay is missing a required time, map, fixture, toolbar, or construction host.");
            }

            SetObjectReference(
                planogramHost,
                "productCatalogAsset",
                productCatalog);

            PurchasingRuntimeHost runtimeHost =
                GetOrAddComponent<PurchasingRuntimeHost>(
                    planogramHost.gameObject);
            SetObjectReference(runtimeHost, "commercialCatalog", catalog);
            SetObjectReference(runtimeHost, "timeHost", timeHost);
            SetObjectReference(
                runtimeHost,
                "planogramRuntimeHost",
                planogramHost);

            GameObject workspaceObject =
                FindSceneGameObject(scene, "PurchasingWorkspaceUI");

            if (workspaceObject == null)
            {
                workspaceObject = new GameObject("PurchasingWorkspaceUI");
                workspaceObject.transform.SetParent(
                    toolbarDocumentHost.transform.parent,
                    false);
            }

            workspaceObject.SetActive(true);
            workspaceObject.transform.SetAsLastSibling();

            PanelRenderer panelRenderer =
                GetOrAddComponent<PanelRenderer>(workspaceObject);
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.visualTreeAsset = visualTree;
            panelRenderer.sortingOrder = 100;
            EditorUtility.SetDirty(panelRenderer);

            PurchasingWorkspaceDocumentHost purchasingDocumentHost =
                GetOrAddComponent<PurchasingWorkspaceDocumentHost>(
                    workspaceObject);
            SetObjectReference(
                purchasingDocumentHost,
                "panelRenderer",
                panelRenderer);

            PurchasingWorkspacePresenter purchasingPresenter =
                GetOrAddComponent<PurchasingWorkspacePresenter>(
                    workspaceObject);
            SetObjectReference(
                purchasingPresenter,
                "documentHost",
                purchasingDocumentHost);
            SetObjectReference(
                purchasingPresenter,
                "commercialCatalog",
                catalog);
            SetObjectReference(
                purchasingPresenter,
                "runtimeHost",
                runtimeHost);

            PurchasingGameplayOverlayController overlayController =
                GetOrAddComponent<PurchasingGameplayOverlayController>(
                    toolbarDocumentHost.gameObject);
            SetObjectReference(
                overlayController,
                "toolbarDocumentHost",
                toolbarDocumentHost);
            SetObjectReference(
                overlayController,
                "toolCoordinator",
                toolCoordinator);
            SetObjectReference(
                overlayController,
                "purchasingWorkspace",
                workspaceObject);
            SetObjectReference(
                overlayController,
                "purchasingPresenter",
                purchasingPresenter);

            SetObjectReference(
                fixtureInspector,
                "purchasingRuntimeHost",
                runtimeHost);

            InboundDeliveryViewSystem deliveryViewSystem =
                GetOrAddComponent<InboundDeliveryViewSystem>(
                    planogramHost.gameObject);
            SetObjectReference(
                deliveryViewSystem,
                "purchasingRuntimeHost",
                runtimeHost);
            SetObjectReference(
                deliveryViewSystem,
                "mapHost",
                mapHost);
            SetObjectReference(
                deliveryViewSystem,
                "viewHost",
                viewHost);
            SetObjectReference(
                deliveryViewSystem,
                "coordinateTilemap",
                coordinateTilemap);

            workspaceObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Integrated the live Purchasing workspace, visible supplier pallets, deliveries, and opening 12-product catalog into Gameplay.",
                workspaceObject);
        }

        /// <summary>
        /// Batch-mode entry point used by automated validation.
        /// </summary>
        public static void BuildAllForAutomation()
        {
            CommercialCatalogAsset catalog = BuildCatalogAssets();
            BuildPurchasingLabScene(catalog);
            BuildCommercialDirectoryLabScene(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        private static CommercialCatalogAsset BuildCatalogAssets()
        {
            EnsureFolder(BrandFolder);
            EnsureFolder(ProductFolder);
            EnsureFolder(MerchandiseCatalogFolder);
            EnsureFolder(SupplierFolder);
            EnsureFolder(OfferFolder);
            EnsureFolder(PurchasingCatalogFolder);

            BrandDefinitionAsset[] brands = BuildBrands();
            ProductDefinitionAsset[] products = BuildProducts(brands);
            SupplierDefinitionAsset[] suppliers = BuildSuppliers();
            SupplierOfferDefinitionAsset[] offers =
                BuildOffers(products, suppliers);

            BrandCatalogAsset brandCatalog =
                CreateOrLoad<BrandCatalogAsset>(BrandCatalogPath, out _);
            SetObjectArray(brandCatalog, "brands", brands);

            ProductCatalogAsset productCatalog =
                CreateOrLoad<ProductCatalogAsset>(ProductCatalogPath, out _);
            SetObjectArray(productCatalog, "products", products);

            SupplierCatalogAsset supplierCatalog =
                CreateOrLoad<SupplierCatalogAsset>(SupplierCatalogPath, out _);
            SetObjectArray(supplierCatalog, "suppliers", suppliers);

            SupplierOfferCatalogAsset offerCatalog =
                CreateOrLoad<SupplierOfferCatalogAsset>(OfferCatalogPath, out _);
            SetObjectArray(offerCatalog, "offers", offers);

            CommercialCatalogAsset commercialCatalog =
                CreateOrLoad<CommercialCatalogAsset>(
                    CommercialCatalogPath,
                    out _);
            SerializedObject serializedCatalog =
                new SerializedObject(commercialCatalog);
            serializedCatalog.FindProperty("brandCatalog").objectReferenceValue =
                brandCatalog;
            serializedCatalog.FindProperty("productCatalog").objectReferenceValue =
                productCatalog;
            serializedCatalog.FindProperty("supplierCatalog").objectReferenceValue =
                supplierCatalog;
            serializedCatalog.FindProperty("supplierOfferCatalog")
                .objectReferenceValue = offerCatalog;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(commercialCatalog);

            AssetDatabase.SaveAssets();
            return commercialCatalog;
        }

        private static BrandDefinitionAsset[] BuildBrands()
        {
            BrandDefinitionAsset[] assets =
                new BrandDefinitionAsset[BrandSeeds.Length];

            for (int index = 0; index < BrandSeeds.Length; index++)
            {
                BrandSeed seed = BrandSeeds[index];
                string path = $"{BrandFolder}/{seed.FileName}.asset";
                BrandDefinitionAsset asset =
                    CreateOrLoad<BrandDefinitionAsset>(path, out bool created);
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("brandId").stringValue = seed.Id;
                serialized.FindProperty("displayName").stringValue = seed.DisplayName;
                serialized.FindProperty("identity").stringValue = seed.Identity;

                if (created)
                {
                    serialized.FindProperty("accentColor").colorValue = seed.Color;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                assets[index] = asset;
            }

            return assets;
        }

        private static ProductDefinitionAsset[] BuildProducts(
            IReadOnlyList<BrandDefinitionAsset> brands)
        {
            ProductDefinitionAsset[] assets =
                new ProductDefinitionAsset[ProductSeeds.Length];

            for (int index = 0; index < ProductSeeds.Length; index++)
            {
                ProductSeed seed = ProductSeeds[index];
                string path = $"{ProductFolder}/{seed.FileName}.asset";
                ProductDefinitionAsset asset =
                    CreateOrLoad<ProductDefinitionAsset>(path, out _);
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("productId").stringValue = seed.Id;
                serialized.FindProperty("displayName").stringValue = seed.DisplayName;
                serialized.FindProperty("brand").objectReferenceValue =
                    brands[seed.BrandIndex];
                serialized.FindProperty("productLine").stringValue = seed.ProductLine;
                serialized.FindProperty("categoryId").stringValue = seed.CategoryId;
                serialized.FindProperty("marketPosition").enumValueIndex =
                    (int)MarketPosition.Standard;
                serialized.FindProperty("packageForm").stringValue = seed.PackageForm;
                serialized.FindProperty("stockUnit").enumValueIndex =
                    (int)StockUnit.Each;
                serialized.FindProperty("retailUnitPriceCents").longValue =
                    OpeningRetailUnitPricesCents[index];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                assets[index] = asset;
            }

            return assets;
        }

        private static SupplierDefinitionAsset[] BuildSuppliers()
        {
            SupplierDefinitionAsset[] assets =
                new SupplierDefinitionAsset[SupplierSeeds.Length];

            for (int index = 0; index < SupplierSeeds.Length; index++)
            {
                SupplierSeed seed = SupplierSeeds[index];
                string path = $"{SupplierFolder}/{seed.FileName}.asset";
                SupplierDefinitionAsset asset =
                    CreateOrLoad<SupplierDefinitionAsset>(path, out bool created);
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("supplierId").stringValue = seed.Id;
                serialized.FindProperty("displayName").stringValue = seed.DisplayName;
                serialized.FindProperty("specialty").stringValue = seed.Specialty;
                serialized.FindProperty("description").stringValue = seed.Description;
                serialized.FindProperty("minimumOrderCents").longValue =
                    seed.MinimumOrderCents;
                serialized.FindProperty("deliveryKind").enumValueIndex =
                    (int)seed.DeliveryKind;
                serialized.FindProperty("sameDayLeadHours").intValue =
                    Math.Max(1, seed.SameDayLeadHours);
                serialized.FindProperty("routeDays").intValue =
                    (int)seed.RouteDays;

                if (created)
                {
                    serialized.FindProperty("accentColor").colorValue = seed.Color;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                assets[index] = asset;
            }

            return assets;
        }

        private static SupplierOfferDefinitionAsset[] BuildOffers(
            IReadOnlyList<ProductDefinitionAsset> products,
            IReadOnlyList<SupplierDefinitionAsset> suppliers)
        {
            SupplierOfferDefinitionAsset[] assets =
                new SupplierOfferDefinitionAsset[OfferSeeds.Length];

            for (int index = 0; index < OfferSeeds.Length; index++)
            {
                OfferSeed seed = OfferSeeds[index];
                ProductSeed productSeed = ProductSeeds[seed.ProductIndex];
                SupplierSeed supplierSeed = SupplierSeeds[seed.SupplierIndex];
                string offerId = supplierSeed.Id + "-" + productSeed.Id;
                string path =
                    $"{OfferFolder}/{supplierSeed.FileName}_"
                    + $"{productSeed.FileName}.asset";
                SupplierOfferDefinitionAsset asset =
                    CreateOrLoad<SupplierOfferDefinitionAsset>(path, out _);
                SerializedObject serialized = new SerializedObject(asset);
                serialized.FindProperty("offerId").stringValue = offerId;
                serialized.FindProperty("supplier").objectReferenceValue =
                    suppliers[seed.SupplierIndex];
                serialized.FindProperty("product").objectReferenceValue =
                    products[seed.ProductIndex];
                serialized.FindProperty("purchasePackQuantity").intValue =
                    seed.PackQuantity;
                serialized.FindProperty("packPriceCents").longValue =
                    seed.PackPriceCents;
                serialized.FindProperty("isAvailable").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                assets[index] = asset;
            }

            return assets;
        }

        private static void BuildPurchasingLabScene(
            CommercialCatalogAsset catalog)
        {
            EnsureFolder(LabSceneFolder);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PurchasingUxmlPath);
            PanelSettings panelSettings =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            catalog = AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(
                CommercialCatalogPath);

            if (visualTree == null)
            {
                throw new InvalidOperationException(
                    $"Purchasing UXML is missing at '{PurchasingUxmlPath}'.");
            }

            if (panelSettings == null)
            {
                throw new InvalidOperationException(
                    $"Panel settings are missing at '{PanelSettingsPath}'.");
            }

            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Commercial catalog is missing at '{CommercialCatalogPath}'.");
            }

            GameObject cameraObject = new GameObject("Lab Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.12f, 0.13f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";

            GameObject uiObject = new GameObject("PurchasingWorkspaceUI");
            PanelRenderer panelRenderer = uiObject.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.visualTreeAsset = visualTree;
            EditorUtility.SetDirty(panelRenderer);

            PurchasingWorkspaceDocumentHost documentHost =
                uiObject.AddComponent<PurchasingWorkspaceDocumentHost>();
            SerializedObject serializedHost = new SerializedObject(documentHost);
            serializedHost.Update();
            serializedHost.FindProperty("panelRenderer").objectReferenceValue =
                panelRenderer;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(documentHost);

            PurchasingWorkspacePresenter presenter =
                uiObject.AddComponent<PurchasingWorkspacePresenter>();
            SerializedObject serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.Update();
            serializedPresenter.FindProperty("documentHost").objectReferenceValue =
                documentHost;
            serializedPresenter.FindProperty("commercialCatalog")
                .objectReferenceValue = catalog;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PurchasingLabScenePath);
        }

        private static void BuildCommercialDirectoryLabScene(
            CommercialCatalogAsset catalog)
        {
            EnsureFolder(LabSceneFolder);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    CommercialDirectoryUxmlPath);
            PanelSettings panelSettings =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            catalog = AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(
                CommercialCatalogPath);

            if (visualTree == null)
            {
                throw new InvalidOperationException(
                    $"Commercial Directory UXML is missing at "
                    + $"'{CommercialDirectoryUxmlPath}'.");
            }

            if (panelSettings == null)
            {
                throw new InvalidOperationException(
                    $"Panel settings are missing at '{PanelSettingsPath}'.");
            }

            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Commercial catalog is missing at '{CommercialCatalogPath}'.");
            }

            GameObject cameraObject = new GameObject("Lab Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.12f, 0.13f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";

            GameObject uiObject = new GameObject("CommercialDirectoryUI");
            PanelRenderer panelRenderer = uiObject.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panelSettings;
            panelRenderer.visualTreeAsset = visualTree;
            EditorUtility.SetDirty(panelRenderer);

            CommercialDirectoryDocumentHost documentHost =
                uiObject.AddComponent<CommercialDirectoryDocumentHost>();
            SerializedObject serializedHost = new SerializedObject(documentHost);
            serializedHost.Update();
            serializedHost.FindProperty("panelRenderer").objectReferenceValue =
                panelRenderer;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(documentHost);

            CommercialDirectoryPresenter presenter =
                uiObject.AddComponent<CommercialDirectoryPresenter>();
            SerializedObject serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.Update();
            serializedPresenter.FindProperty("documentHost").objectReferenceValue =
                documentHost;
            serializedPresenter.FindProperty("commercialCatalog")
                .objectReferenceValue = catalog;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(
                scene,
                CommercialDirectoryLabScenePath);
        }

        private static T CreateOrLoad<T>(string path, out bool created)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                created = false;
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        private static void SetObjectArray<T>(
            ScriptableObject owner,
            string propertyName,
            IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Count;

            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }

        private static void SetObjectReference(
            UnityEngine.Object owner,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{owner.GetType().Name} has no serialized property '{propertyName}'.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }

        private static T GetOrAddComponent<T>(
            GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static GameObject FindSceneGameObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0; index < roots.Length; index++)
            {
                Transform[] transforms =
                    roots[index].GetComponentsInChildren<Transform>(true);

                for (int childIndex = 0;
                     childIndex < transforms.Length;
                     childIndex++)
                {
                    if (transforms[childIndex].name == objectName)
                    {
                        return transforms[childIndex].gameObject;
                    }
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);

            if (string.IsNullOrEmpty(parent))
            {
                throw new InvalidOperationException(
                    $"Cannot determine parent folder for '{path}'.");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }


        private readonly struct BrandSeed
        {
            public BrandSeed(
                string id,
                string displayName,
                string identity,
                string fileName,
                Color color)
            {
                Id = id;
                DisplayName = displayName;
                Identity = identity;
                FileName = fileName;
                Color = color;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Identity { get; }
            public string FileName { get; }
            public Color Color { get; }
        }

        private readonly struct ProductSeed
        {
            public ProductSeed(
                string id,
                string displayName,
                int brandIndex,
                string productLine,
                string categoryId,
                string packageForm,
                string fileName)
            {
                Id = id;
                DisplayName = displayName;
                BrandIndex = brandIndex;
                ProductLine = productLine;
                CategoryId = categoryId;
                PackageForm = packageForm;
                FileName = fileName;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int BrandIndex { get; }
            public string ProductLine { get; }
            public string CategoryId { get; }
            public string PackageForm { get; }
            public string FileName { get; }
        }

        private readonly struct SupplierSeed
        {
            public SupplierSeed(
                string id,
                string displayName,
                string specialty,
                string description,
                long minimumOrderCents,
                SupplierDeliveryKind deliveryKind,
                int sameDayLeadHours,
                SupplierWeekday routeDays,
                string fileName,
                Color color)
            {
                Id = id;
                DisplayName = displayName;
                Specialty = specialty;
                Description = description;
                MinimumOrderCents = minimumOrderCents;
                DeliveryKind = deliveryKind;
                SameDayLeadHours = sameDayLeadHours;
                RouteDays = routeDays;
                FileName = fileName;
                Color = color;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Specialty { get; }
            public string Description { get; }
            public long MinimumOrderCents { get; }
            public SupplierDeliveryKind DeliveryKind { get; }
            public int SameDayLeadHours { get; }
            public SupplierWeekday RouteDays { get; }
            public string FileName { get; }
            public Color Color { get; }
        }

        private readonly struct OfferSeed
        {
            public OfferSeed(
                int supplierIndex,
                int productIndex,
                int packQuantity,
                long packPriceCents)
            {
                SupplierIndex = supplierIndex;
                ProductIndex = productIndex;
                PackQuantity = packQuantity;
                PackPriceCents = packPriceCents;
            }

            public int SupplierIndex { get; }
            public int ProductIndex { get; }
            public int PackQuantity { get; }
            public long PackPriceCents { get; }
        }
    }
}
