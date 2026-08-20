using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FoundationToolbarIntegrationTests
    {
        private const string MapperTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarModeMapper, Assembly-CSharp";

        private const string ModeTypeName =
            "BigRetail.Construction.Unity.Tools." +
            "ConstructionToolMode, Assembly-CSharp";

        private const string CoordinatorTypeName =
            "BigRetail.Construction.Unity.Tools." +
            "ConstructionToolCoordinator, Assembly-CSharp";

        private const string SectionTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarSection, Assembly-CSharp";

        private const string ViewTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarView, Assembly-CSharp";

        private const string WindowConstructionToolControllerTypeName =
            "BigRetail.Construction.Unity.Doors." +
            "WindowConstructionToolController, Assembly-CSharp";

        private const string DocumentHostTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarDocumentHost, Assembly-CSharp";

        private const string DoorDefinitionPickerViewTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "DoorDefinitionPickerView, Assembly-CSharp";

        private const string DoorDefinitionPickerItemTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "DoorDefinitionPickerItem, Assembly-CSharp";

        private const string MerchandisingInputControllerTypeName =
            "BigRetail.Construction.Unity.Fixtures." +
            "FixtureMerchandisingInputController, Assembly-CSharp";


        [Test]
        public void BuildFoundations_MapsToFoundationToolbarSection()
        {
            Type mapperType =
                RequireType(MapperTypeName);

            Type modeType =
                RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public
                    | BindingFlags.Static);

            Assert.That(toSection, Is.Not.Null);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(
                            modeType,
                            "BuildFoundations")
                    });

            Assert.That(
                section.ToString(),
                Is.EqualTo("Foundations"));
        }


        [Test]
        public void BuildSidewalks_MapsToSidewalksToolbarSection()
        {
            Type mapperType =
                RequireType(MapperTypeName);

            Type modeType =
                RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public
                    | BindingFlags.Static);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(
                            modeType,
                            "BuildSidewalks")
                    });

            Assert.That(
                section.ToString(),
                Is.EqualTo("Sidewalks"));
        }


        [Test]
        public void BuildWindows_MapsToWindowsToolbarSection()
        {
            Type mapperType =
                RequireType(MapperTypeName);

            Type modeType =
                RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public
                    | BindingFlags.Static);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(
                            modeType,
                            "BuildWindows")
                    });

            Assert.That(
                section.ToString(),
                Is.EqualTo("Windows"));
        }


        [Test]
        public void WindowTool_UsesDoorStylePlacementLifecycle()
        {
            Type controllerType =
                RequireType(
                    WindowConstructionToolControllerTypeName);

            Assert.That(
                controllerType.GetProperty(
                    "HasPlacementPreview",
                    BindingFlags.Public
                    | BindingFlags.Instance),
                Is.Not.Null);

            Assert.That(
                controllerType.GetMethod(
                    "ClearPlacementPreview",
                    BindingFlags.Public
                    | BindingFlags.Instance),
                Is.Not.Null);
        }


        [Test]
        public void GameplayScene_WiresWindowWallEdgeTarget()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    "Assets/Scenes/Gameplay.unity",
                    OpenSceneMode.Additive);

            try
            {
                Type controllerType =
                    RequireType(
                        WindowConstructionToolControllerTypeName);

                Component controller =
                    FindComponentInScene(
                        scene,
                        controllerType);

                Assert.That(
                    controller,
                    Is.Not.Null);

                FieldInfo wallTargetResolver =
                    controllerType.GetField(
                        "wallTargetResolver",
                        BindingFlags.NonPublic
                        | BindingFlags.Instance);

                Assert.That(
                    wallTargetResolver,
                    Is.Not.Null);

                Assert.That(
                    wallTargetResolver.GetValue(controller),
                    Is.Not.Null);

                Assert.That(
                    controllerType.GetField(
                            "placementPreviewView",
                            BindingFlags.NonPublic
                            | BindingFlags.Instance)
                        .GetValue(controller),
                    Is.Not.Null);

                Assert.That(
                    controllerType.GetField(
                            "windowDefinition",
                            BindingFlags.NonPublic
                            | BindingFlags.Instance)
                        .GetValue(controller),
                    Is.Not.Null);

                Type coordinatorType =
                    RequireType(
                        CoordinatorTypeName);

                Component coordinator =
                    FindComponentInScene(
                        scene,
                        coordinatorType);

                Assert.That(
                    coordinator,
                    Is.Not.Null);

                Assert.That(
                    coordinatorType.GetField(
                            "windowConstructionTool",
                            BindingFlags.NonPublic
                            | BindingFlags.Instance)
                        .GetValue(coordinator),
                    Is.SameAs(controller));
            }
            finally
            {
                EditorSceneManager.CloseScene(
                    scene,
                    removeScene: true);
            }
        }


        [Test]
        public void WindowDefinition_UsesDedicatedDirectionalMasksAtOneTileWidth()
        {
            DoorDefinitionAsset windowDefinition =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    DoorDefinitionAsset>(
                    "Assets/Design/Doors/FixedWindow.asset");

            Assert.That(
                windowDefinition,
                Is.Not.Null);

            Assert.DoesNotThrow(
                windowDefinition.ValidateConfiguration);

            Assert.That(
                windowDefinition.Id.Value,
                Is.EqualTo("FIXED-WINDOW"));

            Assert.That(windowDefinition.SegmentCount, Is.EqualTo(1));
            Assert.That(windowDefinition.HasPassageSegments, Is.False);

            DoorDefinition domainDefinition =
                windowDefinition.CreateDomainDefinition();

            Assert.That(
                domainDefinition.PassageSegmentCount,
                Is.EqualTo(0));

            Assert.That(
                windowDefinition.TryGetDoorwaySprites(
                    WallDisplaySlope.RisingLeft,
                    out DoorwaySprites risingLeft),
                Is.True);

            Assert.That(
                windowDefinition.TryGetDoorwaySprites(
                    WallDisplaySlope.RisingRight,
                    out DoorwaySprites risingRight),
                Is.True);

            Assert.That(
                risingLeft.Frame,
                Is.Not.SameAs(risingLeft.Aperture));

            Assert.That(
                risingRight.Frame,
                Is.Not.SameAs(risingRight.Aperture));

            Assert.That(
                UnityEditor.AssetDatabase.GetAssetPath(
                    risingLeft.Aperture),
                Is.EqualTo(
                    "Assets/Art/WallSegmentArt/Windows/"
                    + "Window_RisingLeft_Mask.png"));

            Assert.That(
                UnityEditor.AssetDatabase.GetAssetPath(
                    risingRight.Aperture),
                Is.EqualTo(
                    "Assets/Art/WallSegmentArt/Windows/"
                    + "Window_RisingRight_Mask.png"));

            Assert.That(
                risingLeft.Frame.pixelsPerUnit,
                Is.EqualTo(94f));

            Assert.That(
                risingRight.Frame.pixelsPerUnit,
                Is.EqualTo(94f));

            Assert.That(
                risingLeft.Aperture.pixelsPerUnit,
                Is.EqualTo(94f));

            Assert.That(
                risingRight.Aperture.pixelsPerUnit,
                Is.EqualTo(94f));

            Assert.That(
                risingLeft.Frame.bounds.size.x,
                Is.EqualTo(0.5f).Within(0.001f));

            Assert.That(
                risingRight.Frame.bounds.size.x,
                Is.EqualTo(0.5f).Within(0.001f));

            Assert.That(
                risingLeft.Aperture.bounds.size,
                Is.EqualTo(risingLeft.Frame.bounds.size));

            Assert.That(
                risingRight.Aperture.bounds.size,
                Is.EqualTo(risingRight.Frame.bounds.size));

            Assert.That(
                risingLeft.Aperture.pivot,
                Is.EqualTo(risingLeft.Frame.pivot));

            Assert.That(
                risingRight.Aperture.pivot,
                Is.EqualTo(risingRight.Frame.pivot));
        }

        [Test]
        public void DemolishFoundations_MapsToDemolitionToolbarSection()
        {
            Type mapperType =
                RequireType(MapperTypeName);

            Type modeType =
                RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public
                    | BindingFlags.Static);

            Assert.That(toSection, Is.Not.Null);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(
                            modeType,
                            "DemolishFoundations")
                    });

            Assert.That(
                section.ToString(),
                Is.EqualTo("Demolition"));
        }


        [Test]
        public void BuildFloors_MapsToFloorsToolbarSection()
        {
            Type mapperType =
                RequireType(MapperTypeName);

            Type modeType =
                RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public
                    | BindingFlags.Static);

            Assert.That(toSection, Is.Not.Null);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(
                            modeType,
                            "BuildFloors")
                    });

            Assert.That(
                section.ToString(),
                Is.EqualTo("Floors"));
        }


        [Test]
        public void BuildDoors_MapsToDoorsToolbarSection()
        {
            Type mapperType =
                RequireType(MapperTypeName);

            Type modeType =
                RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public
                    | BindingFlags.Static);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(
                            modeType,
                            "BuildDoors")
                    });

            Assert.That(
                section.ToString(),
                Is.EqualTo("Doors"));
        }


        [Test]
        public void BuildFixtures_MapsToFixturesToolbarSection()
        {
            Type mapperType = RequireType(MapperTypeName);
            Type modeType = RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public | BindingFlags.Static);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(modeType, "BuildFixtures")
                    });

            Assert.That(section.ToString(), Is.EqualTo("Fixtures"));
        }


        [Test]
        public void DemolishFixtures_MapsToDemolitionToolbarSection()
        {
            Type mapperType = RequireType(MapperTypeName);
            Type modeType = RequireType(ModeTypeName);

            MethodInfo toSection =
                mapperType.GetMethod(
                    "ToSection",
                    BindingFlags.Public | BindingFlags.Static);

            object section =
                toSection.Invoke(
                    null,
                    new object[]
                    {
                        Enum.Parse(modeType, "DemolishFixtures")
                    });

            Assert.That(section.ToString(), Is.EqualTo("Demolition"));
        }


        [Test]
        public void DoorDefinitionPicker_BuildsAndSelectsCatalogButton()
        {
            Type viewType =
                RequireType(DoorDefinitionPickerViewTypeName);

            Type itemType =
                RequireType(DoorDefinitionPickerItemTypeName);

            VisualElement root =
                new VisualElement();

            VisualElement panel =
                new VisualElement
                {
                    name = "door-definition-picker"
                };

            VisualElement itemsContainer =
                new VisualElement
                {
                    name = "door-definition-picker-items"
                };

            Button windowButton =
                new Button
                {
                    name = "windows-button"
                };

            itemsContainer.Add(
                windowButton);

            panel.Add(
                itemsContainer);
            root.Add(
                panel);

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                object item =
                    Activator.CreateInstance(
                        itemType,
                        "AUTOMATIC-FRONT-DOOR",
                        "Automatic Front Door",
                        null);

                Array items =
                    Array.CreateInstance(
                        itemType,
                        1);

                items.SetValue(
                    item,
                    0);

                viewType.GetMethod(
                        "SetItems",
                        BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(
                        view,
                        new object[] { items });

                Button button =
                    root.Q<Button>(
                        "door-definition-AUTOMATIC-FRONT-DOOR-button");

                Assert.That(
                    button,
                    Is.Not.Null);

                Assert.That(
                    root.Q<Button>("windows-button"),
                    Is.SameAs(windowButton));

                Assert.That(
                    button.tooltip,
                    Is.EqualTo("Automatic Front Door"));

                viewType.GetMethod(
                        "SetSelectedDefinition",
                        BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(
                        view,
                        new object[] { "AUTOMATIC-FRONT-DOOR" });

                Assert.That(
                    button.ClassListContains("is-selected"),
                    Is.True);

                viewType.GetMethod(
                        "SetVisible",
                        BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(
                        view,
                        new object[] { false });

                Assert.That(
                    panel.style.display.value,
                    Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void FoundationSection_SelectsOnlyFoundationButton()
        {
            Type viewType =
                RequireType(ViewTypeName);

            Type sectionType =
                RequireType(SectionTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                object foundationsSection =
                    Enum.Parse(
                        sectionType,
                        "Foundations");

                MethodInfo setSelectedSection =
                    viewType.GetMethod(
                        "SetSelectedSection",
                        BindingFlags.Public
                        | BindingFlags.Instance);

                Assert.That(
                    setSelectedSection,
                    Is.Not.Null);

                setSelectedSection.Invoke(
                    view,
                    new[] { foundationsSection });

                Assert.That(
                    root.Q<Button>("foundations-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                Assert.That(
                    root.Q<Button>("walls-button")
                        .ClassListContains("is-selected"),
                    Is.False);

                Assert.That(
                    root.Q<Button>("floors-button")
                        .ClassListContains("is-selected"),
                    Is.False);

                Assert.That(
                    root.Q<Button>("doors-button")
                        .ClassListContains("is-selected"),
                    Is.False);

                Assert.That(
                    root.Q<Button>("demolition-button")
                        .ClassListContains("is-selected"),
                    Is.False);

                Assert.That(
                    root.Q<VisualElement>("foundation-picker")
                        .style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));

                Assert.That(
                    root.Q<Button>("foundation-default-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                setSelectedSection.Invoke(
                    view,
                    new[]
                    {
                        Enum.Parse(
                            sectionType,
                            "Sidewalks")
                    });

                Assert.That(
                    root.Q<Button>("foundations-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                Assert.That(
                    root.Q<VisualElement>("foundation-picker")
                        .style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));

                Assert.That(
                    root.Q<Button>("sidewalks-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                Assert.That(
                    root.Q<Button>("foundation-default-button")
                        .ClassListContains("is-selected"),
                    Is.False);

                setSelectedSection.Invoke(
                    view,
                    new[]
                    {
                        Enum.Parse(
                            sectionType,
                            "Walls")
                    });

                Assert.That(
                    root.Q<VisualElement>("foundation-picker")
                        .style.display.value,
                    Is.EqualTo(DisplayStyle.None));

                Assert.That(
                    root.Q<Button>("foundation-default-button")
                        .ClassListContains("is-selected"),
                    Is.False);
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void WindowsSection_SelectsDoorsParentAndWindowChild()
        {
            Type viewType =
                RequireType(ViewTypeName);

            Type sectionType =
                RequireType(SectionTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                viewType.GetMethod(
                        "SetSelectedSection",
                        BindingFlags.Public
                        | BindingFlags.Instance)
                    .Invoke(
                        view,
                        new[]
                        {
                            Enum.Parse(
                                sectionType,
                                "Windows")
                        });

                Assert.That(
                    root.Q<Button>("doors-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                Assert.That(
                    root.Q<Button>("windows-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                Assert.That(
                    root.Q<Button>("walls-button")
                        .ClassListContains("is-selected"),
                    Is.False);
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void WindowButton_LivesInsideDoorPicker()
        {
            VisualTreeAsset toolbarAsset =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    VisualTreeAsset>(
                    "Assets/UI/Construction/PC/ConstructionToolbar.uxml");

            Assert.That(
                toolbarAsset,
                Is.Not.Null);

            VisualElement root =
                toolbarAsset.CloneTree();

            Button windowButton =
                root.Q<Button>("windows-button");

            VisualElement doorItems =
                root.Q<VisualElement>(
                    "door-definition-picker-items");

            Assert.That(
                windowButton,
                Is.Not.Null);

            Assert.That(
                windowButton.parent,
                Is.SameAs(doorItems));
        }


        [Test]
        public void HistoryButtons_ReflectTheirAvailability()
        {
            Type viewType =
                RequireType(ViewTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                viewType.GetMethod(
                    "SetUndoEnabled",
                    BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(view, new object[] { true });

                viewType.GetMethod(
                    "SetRedoEnabled",
                    BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(view, new object[] { false });

                Assert.That(
                    root.Q<Button>("undo-button").enabledSelf,
                    Is.True);

                Assert.That(
                    root.Q<Button>("redo-button").enabledSelf,
                    Is.False);
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void MerchandiseTool_ReflectsItsActiveState()
        {
            Type viewType =
                RequireType(ViewTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                MethodInfo setMerchandiseToolActive =
                    viewType.GetMethod(
                        "SetMerchandiseToolActive",
                        BindingFlags.Public
                        | BindingFlags.Instance);

                Assert.That(setMerchandiseToolActive, Is.Not.Null);

                setMerchandiseToolActive.Invoke(
                    view,
                    new object[] { true });

                Assert.That(
                    root.Q<Button>("merchandise-tool-button")
                        .ClassListContains("is-selected"),
                    Is.True);

                setMerchandiseToolActive.Invoke(
                    view,
                    new object[] { false });

                Assert.That(
                    root.Q<Button>("merchandise-tool-button")
                        .ClassListContains("is-selected"),
                    Is.False);
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void ReceivingArea_ReflectsActiveStateAndCapacity()
        {
            Type viewType = RequireType(ViewTypeName);
            VisualElement root = CreateToolbarRoot();
            IDisposable view =
                (IDisposable)Activator.CreateInstance(viewType, root);

            try
            {
                viewType.GetMethod(
                        "SetReceivingAreaActive",
                        BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(view, new object[] { true });
                viewType.GetMethod(
                        "SetReceivingAreaStatus",
                        BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(view, new object[] { 3, 1, 2 });

                Assert.That(
                    root.Q<Button>("receiving-area-button")
                        .ClassListContains("is-selected"),
                    Is.True);
                Assert.That(
                    root.Q<VisualElement>("receiving-area-panel")
                        .style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));
                Assert.That(
                    root.Q<Label>("receiving-area-capacity").text,
                    Is.EqualTo("3 PALLET SPACES · 1 OCCUPIED"));
                Assert.That(
                    root.Q<Label>("receiving-area-instruction").text,
                    Does.Contain("2 supplier orders"));
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void CashHud_FormatsAuthoritativeCentBalance()
        {
            Type viewType =
                RequireType(ViewTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                MethodInfo setCashBalance =
                    viewType.GetMethod(
                        "SetCashBalance",
                        BindingFlags.Public
                        | BindingFlags.Instance);

                Assert.That(setCashBalance, Is.Not.Null);

                setCashBalance.Invoke(
                    view,
                    new object[] { 250000L });

                Assert.That(
                    root.Q<Label>("store-cash-value").text,
                    Is.EqualTo("$2,500.00"));
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void MerchandiseInput_RepairsMissingHoverOutlineReference()
        {
            Type controllerType =
                RequireType(MerchandisingInputControllerTypeName);

            GameObject mapObject =
                new GameObject("Merchandising Map Test");
            GameObject inputObject =
                new GameObject("Merchandising Input Test");

            try
            {
                FixtureViewSystem fixtureViewSystem =
                    mapObject.AddComponent<FixtureViewSystem>();

                Component controller =
                    inputObject.AddComponent(controllerType);

                FieldInfo fixtureViewSystemField =
                    controllerType.GetField(
                        "fixtureViewSystem",
                        BindingFlags.NonPublic
                        | BindingFlags.Instance);

                MethodInfo resolveRuntimeReferences =
                    controllerType.GetMethod(
                        "ResolveRuntimeReferences",
                        BindingFlags.NonPublic
                        | BindingFlags.Instance);

                Assert.That(fixtureViewSystemField, Is.Not.Null);
                Assert.That(resolveRuntimeReferences, Is.Not.Null);

                fixtureViewSystemField.SetValue(
                    controller,
                    fixtureViewSystem);

                resolveRuntimeReferences.Invoke(controller, null);

                Assert.That(
                    mapObject.GetComponent<
                        FixtureMerchandisingHoverOutlineView>(),
                    Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inputObject);
                UnityEngine.Object.DestroyImmediate(mapObject);
            }
        }


        [Test]
        public void WallDisplayMode_SelectsOnlyRequestedModeButton()
        {
            Type viewType =
                RequireType(ViewTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                MethodInfo setWallDisplayMode =
                    viewType.GetMethod(
                        "SetWallDisplayMode",
                        BindingFlags.Public
                        | BindingFlags.Instance);

                Assert.That(setWallDisplayMode, Is.Not.Null);

                setWallDisplayMode.Invoke(
                    view,
                    new object[]
                    {
                        WallDisplayMode.Cutaway
                    });

                Assert.That(
                    root.Q<Button>("wall-view-cutaway-button")
                        .ClassListContains("is-selected"),
                    Is.True);
                Assert.That(
                    root.Q<Button>("wall-view-up-button")
                        .ClassListContains("is-selected"),
                    Is.False);
                Assert.That(
                    root.Q<Button>("wall-view-down-button")
                        .ClassListContains("is-selected"),
                    Is.False);
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void CameraOrientation_SelectsOnlyRequestedViewButton()
        {
            Type viewType =
                RequireType(ViewTypeName);

            VisualElement root =
                CreateToolbarRoot();

            IDisposable view =
                (IDisposable)Activator.CreateInstance(
                    viewType,
                    root);

            try
            {
                MethodInfo setCameraViewOrientation =
                    viewType.GetMethod(
                        "SetCameraViewOrientation",
                        BindingFlags.Public
                        | BindingFlags.Instance);

                Assert.That(
                    setCameraViewOrientation,
                    Is.Not.Null);

                setCameraViewOrientation.Invoke(
                    view,
                    new object[]
                    {
                        IsometricViewOrientation.South
                    });

                Assert.That(
                    root.Q<Button>("camera-view-north-button")
                        .ClassListContains("is-selected"),
                    Is.False);
                Assert.That(
                    root.Q<Button>("camera-view-east-button")
                        .ClassListContains("is-selected"),
                    Is.False);
                Assert.That(
                    root.Q<Button>("camera-view-south-button")
                        .ClassListContains("is-selected"),
                    Is.True);
                Assert.That(
                    root.Q<Button>("camera-view-west-button")
                        .ClassListContains("is-selected"),
                    Is.False);
            }
            finally
            {
                view.Dispose();
            }
        }


        [Test]
        public void UiHitTest_MirrorsBottomLeftPointerToTopLeftCoordinates()
        {
            Type documentHostType =
                RequireType(DocumentHostTypeName);

            MethodInfo toUiToolkitScreenPosition =
                documentHostType.GetMethod(
                    "ToUiToolkitScreenPosition",
                    BindingFlags.NonPublic
                    | BindingFlags.Static);

            Assert.That(
                toUiToolkitScreenPosition,
                Is.Not.Null);

            Vector2 convertedPosition =
                (Vector2)toUiToolkitScreenPosition.Invoke(
                    null,
                    new object[]
                    {
                        new Vector2(300f, 120f),
                        1080f
                    });

            Assert.That(
                convertedPosition,
                Is.EqualTo(
                    new Vector2(300f, 960f)));
        }


        [Test]
        public void GameplayUi_IconButtonsProvideVisibleContext()
        {
            VisualTreeAsset toolbarAsset =
                UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/UI/Construction/PC/ConstructionToolbar.uxml");

            Assert.That(toolbarAsset, Is.Not.Null);

            VisualElement root = toolbarAsset.CloneTree();
            int iconOnlyButtonCount = 0;

            root.Query<Button>().ForEach(
                button =>
                {
                    if (!string.IsNullOrWhiteSpace(button.text))
                    {
                        return;
                    }

                    iconOnlyButtonCount++;

                    Assert.That(
                        button.tooltip,
                        Is.Not.Null.And.Not.Empty,
                        $"Icon button '{button.name}' needs contextual text.");
                });

            Assert.That(
                iconOnlyButtonCount,
                Is.GreaterThanOrEqualTo(20));

            Assert.That(
                root.Q<VisualElement>("control-hint"),
                Is.Not.Null);

            Assert.That(
                root.Q<Label>("control-hint-title"),
                Is.Not.Null);

            Assert.That(
                root.Q<Label>("control-hint-description"),
                Is.Not.Null);
        }


        [Test]
        public void GameplayUi_UsesReadableTypographyHierarchy()
        {
            const string stylePath =
                "Assets/UI/Construction/PC/ConstructionToolbar.uss";

            string styleText = File.ReadAllText(stylePath);
            MatchCollection fontSizes = Regex.Matches(
                styleText,
                @"font-size:\s*(\d+)px;");

            Assert.That(
                fontSizes.Count,
                Is.GreaterThanOrEqualTo(40));

            int minimumFontSize = int.MaxValue;
            int maximumFontSize = int.MinValue;

            foreach (Match fontSize in fontSizes)
            {
                int pixelHeight = int.Parse(
                    fontSize.Groups[1].Value);

                minimumFontSize = Math.Min(
                    minimumFontSize,
                    pixelHeight);

                maximumFontSize = Math.Max(
                    maximumFontSize,
                    pixelHeight);

                Assert.That(
                    pixelHeight,
                    Is.GreaterThanOrEqualTo(15),
                    "Gameplay UI text must remain readable at the "
                    + "authored 1080p reference size.");
            }

            Assert.That(
                minimumFontSize,
                Is.LessThanOrEqualTo(18),
                "Captions and supporting copy should remain compact.");

            Assert.That(
                maximumFontSize,
                Is.GreaterThanOrEqualTo(26),
                "Primary values and actions should remain prominent.");

            StringAssert.Contains(
                "bottom: 118px;",
                styleText,
                "The control hint should stay in its dedicated HUD dock.");

            StringAssert.Contains(
                ".construction-toolbar-screen.is-reduced-motion",
                styleText);

            StringAssert.Contains(
                "transition-duration: 0ms;",
                styleText);
        }


        private static VisualElement CreateToolbarRoot()
        {
            VisualElement root =
                new VisualElement();

            root.Add(CreateButton("departments-button"));
            root.Add(CreateButton("merchandise-tool-button"));
            root.Add(CreateButton("purchasing-button"));
            root.Add(CreateButton("receiving-area-button"));
            VisualElement receivingAreaPanel =
                new VisualElement
                {
                    name = "receiving-area-panel"
                };
            receivingAreaPanel.Add(
                new Label
                {
                    name = "receiving-area-capacity"
                });
            receivingAreaPanel.Add(
                new Label
                {
                    name = "receiving-area-instruction"
                });
            root.Add(receivingAreaPanel);
            VisualElement foundationPicker =
                new VisualElement
                {
                    name = "foundation-picker"
                };
            foundationPicker.Add(
                CreateButton("foundation-default-button"));
            foundationPicker.Add(
                CreateButton("sidewalks-button"));
            root.Add(foundationPicker);
            root.Add(CreateButton("walls-button"));
            root.Add(CreateButton("windows-button"));
            root.Add(CreateButton("doors-button"));
            root.Add(CreateButton("fixtures-button"));
            root.Add(CreateButton("foundations-button"));
            root.Add(CreateButton("floors-button"));
            root.Add(CreateButton("demolition-button"));
            root.Add(CreateButton("demolish-foundations-button"));
            root.Add(CreateButton("demolish-sidewalks-button"));
            root.Add(CreateButton("demolish-floors-button"));
            root.Add(CreateButton("demolish-walls-button"));
            root.Add(CreateButton("demolish-fixtures-button"));
            root.Add(
                new VisualElement
                {
                    name = "demolition-picker"
                });

            root.Add(CreateButton("wall-view-up-button"));
            root.Add(CreateButton("wall-view-cutaway-button"));
            root.Add(CreateButton("wall-view-down-button"));
            root.Add(
                CreateButton(
                    "camera-view-north-button"));
            root.Add(
                CreateButton(
                    "camera-view-east-button"));
            root.Add(
                CreateButton(
                    "camera-view-south-button"));
            root.Add(
                CreateButton(
                    "camera-view-west-button"));
            root.Add(CreateButton("undo-button"));
            root.Add(CreateButton("redo-button"));
            root.Add(
                new Label
                {
                    name = "store-cash-value"
                });

            VisualElement controlHint =
                new VisualElement
                {
                    name = "control-hint"
                };

            controlHint.Add(
                new Label
                {
                    name = "control-hint-title"
                });

            controlHint.Add(
                new Label
                {
                    name = "control-hint-description"
                });

            root.Add(controlHint);

            return root;
        }


        private static Button CreateButton(
            string name)
        {
            return new Button
            {
                name = name
            };
        }


        private static Component FindComponentInScene(
            Scene scene,
            Type componentType)
        {
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                Component component =
                    roots[index].GetComponentInChildren(
                        componentType,
                        includeInactive: true);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }


        private static Type RequireType(
            string typeName)
        {
            Type type =
                Type.GetType(typeName);

            Assert.That(
                type,
                Is.Not.Null,
                $"Could not resolve {typeName}.");

            return type;
        }
    }
}
