using System;
using System.Reflection;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;
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

        private const string SectionTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarSection, Assembly-CSharp";

        private const string ViewTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarView, Assembly-CSharp";

        private const string DocumentHostTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "ConstructionToolbarDocumentHost, Assembly-CSharp";

        private const string DoorDefinitionPickerViewTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "DoorDefinitionPickerView, Assembly-CSharp";

        private const string DoorDefinitionPickerItemTypeName =
            "BigRetail.Construction.Unity.UI.PC." +
            "DoorDefinitionPickerItem, Assembly-CSharp";


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


        private static VisualElement CreateToolbarRoot()
        {
            VisualElement root =
                new VisualElement();

            root.Add(CreateButton("departments-button"));
            VisualElement foundationPicker =
                new VisualElement
                {
                    name = "foundation-picker"
                };
            foundationPicker.Add(
                CreateButton("foundation-default-button"));
            root.Add(foundationPicker);
            root.Add(CreateButton("walls-button"));
            root.Add(CreateButton("doors-button"));
            root.Add(CreateButton("foundations-button"));
            root.Add(CreateButton("floors-button"));
            root.Add(CreateButton("demolition-button"));
            root.Add(CreateButton("demolish-foundations-button"));
            root.Add(CreateButton("demolish-floors-button"));
            root.Add(CreateButton("demolish-walls-button"));
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
