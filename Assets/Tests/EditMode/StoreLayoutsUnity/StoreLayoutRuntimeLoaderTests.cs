using System;
using System.Collections.Generic;
using System.Reflection;
using BigRetail.Departments.Unity;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Receiving.Unity;
using BigRetail.StoreLayouts.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.StoreLayouts.Unity.Tests
{
    public sealed class StoreLayoutRuntimeLoaderTests
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";


        [Test]
        public void FrankRoadside_TinyLayoutLoadsAndResetsDeterministically()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            try
            {
                GridMapHost mapHost =
                    FindRequired<GridMapHost>(scene);
                FoundationRuntimeHost foundationHost =
                    FindRequired<FoundationRuntimeHost>(scene);
                SidewalkRuntimeHost sidewalkHost =
                    FindRequired<SidewalkRuntimeHost>(scene);
                FloorRuntimeHost floorHost =
                    FindRequired<FloorRuntimeHost>(scene);
                FixtureRuntimeHost fixtureHost =
                    FindRequired<FixtureRuntimeHost>(scene);
                DepartmentRuntimeHost departmentHost =
                    FindRequired<DepartmentRuntimeHost>(scene);
                ReceivingAreaRuntimeHost receivingHost =
                    FindRequired<ReceivingAreaRuntimeHost>(scene);

                mapHost.Initialize();
                Assert.That(
                    fixtureHost.TryInitialize(),
                    Is.True);
                FixtureEquipmentPlanState fixturePlans =
                    new FixtureEquipmentPlanState();

                StoreLayoutRuntimeLoader loader =
                    new StoreLayoutRuntimeLoader(
                        mapHost,
                        foundationHost,
                        sidewalkHost,
                        floorHost,
                        fixtureHost,
                        fixturePlans,
                        departmentHost,
                        receivingHost);

                StoreLayoutData proof =
                    CreateProofLayout(
                        mapHost,
                        fixtureHost);
                string canonicalLayoutId =
                    new StoreDataCanonicalizer()
                        .CreateCanonicalCopy(proof)
                        .LayoutId;
                int completionCount = 0;

                loader.LayoutLoaded += loadedLayout =>
                {
                    completionCount++;
                    Assert.That(
                        loadedLayout.LayoutId,
                        Is.EqualTo(canonicalLayoutId));
                    Assert.That(
                        foundationHost.FoundationState.FoundationCount,
                        Is.EqualTo(16));
                    Assert.That(
                        floorHost.FloorState.FloorCount,
                        Is.EqualTo(proof.Floors.Count));
                    Assert.That(
                        receivingHost.State.CellCount,
                        Is.EqualTo(1));
                };

                StoreLayoutAsset asset =
                    ScriptableObject.CreateInstance<StoreLayoutAsset>();

                try
                {
                    asset.ReplaceData(proof);
                    string templateBefore =
                        EditorJsonUtility.ToJson(asset);
                    int historyBefore =
                        ReadConstructionHistoryCount(scene);

                    StoreLayoutLoadResult first =
                        loader.Load(asset);

                    Assert.That(
                        first.Succeeded,
                        Is.True,
                        first.Message);

                    StoreLayoutData firstSnapshot =
                        loader.CaptureCurrent(
                            proof.LayoutId,
                            proof.DisplayName);

                    StoreLayoutLoadResult second =
                        loader.Load(asset);

                    Assert.That(
                        second.Succeeded,
                        Is.True,
                        second.Message);

                    StoreLayoutData secondSnapshot =
                        loader.CaptureCurrent(
                            proof.LayoutId,
                            proof.DisplayName);

                    Assert.That(
                        JsonUtility.ToJson(secondSnapshot),
                        Is.EqualTo(JsonUtility.ToJson(firstSnapshot)));
                    Assert.That(
                        EditorJsonUtility.ToJson(asset),
                        Is.EqualTo(templateBefore));
                    Assert.That(
                        ReadConstructionHistoryCount(scene),
                        Is.EqualTo(historyBefore));
                    Assert.That(completionCount, Is.EqualTo(2));
                    Assert.That(
                        loader.ActiveLayoutId,
                        Is.EqualTo(canonicalLayoutId));
                    Assert.That(
                        foundationHost.FoundationState.FoundationCount,
                        Is.EqualTo(16));
                    Assert.That(
                        sidewalkHost.SidewalkState.SidewalkCount,
                        Is.EqualTo(1));
                    Assert.That(
                        floorHost.FloorState.FloorCount,
                        Is.EqualTo(proof.Floors.Count));
                    Assert.That(mapHost.WallState.WallCount, Is.EqualTo(1));
                    Assert.That(
                        mapHost.DoorAssemblies.AssemblyCount,
                        Is.EqualTo(1));
                    Assert.That(
                        fixtureHost.FixtureState.FixtureCount,
                        Is.EqualTo(1));
                    Assert.That(fixturePlans.Count, Is.EqualTo(1));
                    Assert.That(
                        departmentHost.PlanningState.PlanCount,
                        Is.EqualTo(1));
                    Assert.That(
                        receivingHost.State.CellCount,
                        Is.EqualTo(1));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }
            finally
            {
                OpenEmptyScene();
            }
        }


        [Test]
        public void FrankRoadside_InvalidLayoutDoesNotMutateRuntime()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            try
            {
                GridMapHost mapHost = FindRequired<GridMapHost>(scene);
                FoundationRuntimeHost foundationHost =
                    FindRequired<FoundationRuntimeHost>(scene);
                SidewalkRuntimeHost sidewalkHost =
                    FindRequired<SidewalkRuntimeHost>(scene);
                FloorRuntimeHost floorHost =
                    FindRequired<FloorRuntimeHost>(scene);
                FixtureRuntimeHost fixtureHost =
                    FindRequired<FixtureRuntimeHost>(scene);
                DepartmentRuntimeHost departmentHost =
                    FindRequired<DepartmentRuntimeHost>(scene);
                ReceivingAreaRuntimeHost receivingHost =
                    FindRequired<ReceivingAreaRuntimeHost>(scene);

                mapHost.Initialize();
                Assert.That(fixtureHost.TryInitialize(), Is.True);
                FixtureEquipmentPlanState fixturePlans =
                    new FixtureEquipmentPlanState();

                StoreLayoutRuntimeLoader loader =
                    new StoreLayoutRuntimeLoader(
                        mapHost,
                        foundationHost,
                        sidewalkHost,
                        floorHost,
                        fixtureHost,
                        fixturePlans,
                        departmentHost,
                        receivingHost);

                StoreLayoutData valid =
                    CreateProofLayout(mapHost, fixtureHost);
                StoreLayoutLoadResult loaded = loader.Load(valid);
                Assert.That(loaded.Succeeded, Is.True, loaded.Message);

                StoreLayoutData before =
                    loader.CaptureCurrent(
                        valid.LayoutId,
                        valid.DisplayName);

                StoreLayoutData invalid =
                    new StoreDataCanonicalizer()
                        .CreateCanonicalCopy(valid);
                invalid.MapFingerprint = "wrong-map";

                StoreLayoutLoadResult rejected =
                    loader.Load(invalid);
                StoreLayoutData after =
                    loader.CaptureCurrent(
                        valid.LayoutId,
                        valid.DisplayName);

                Assert.That(rejected.Succeeded, Is.False);
                Assert.That(
                    rejected.Failure,
                    Is.EqualTo(
                        StoreLayoutLoadFailure.ValidationFailed));
                Assert.That(
                    JsonUtility.ToJson(after),
                    Is.EqualTo(JsonUtility.ToJson(before)));
            }
            finally
            {
                OpenEmptyScene();
            }
        }


        [Test]
        public void FrankRoadside_CaptureSaveReloadBuildRoundTrip()
        {
            string assetPath =
                $"Assets/Tests/EditMode/StoreLayoutsUnity/"
                + $"TempStoreLayout_{Guid.NewGuid():N}.asset";
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            try
            {
                GridMapHost mapHost = FindRequired<GridMapHost>(scene);
                FoundationRuntimeHost foundationHost =
                    FindRequired<FoundationRuntimeHost>(scene);
                SidewalkRuntimeHost sidewalkHost =
                    FindRequired<SidewalkRuntimeHost>(scene);
                FloorRuntimeHost floorHost =
                    FindRequired<FloorRuntimeHost>(scene);
                FixtureRuntimeHost fixtureHost =
                    FindRequired<FixtureRuntimeHost>(scene);
                DepartmentRuntimeHost departmentHost =
                    FindRequired<DepartmentRuntimeHost>(scene);
                ReceivingAreaRuntimeHost receivingHost =
                    FindRequired<ReceivingAreaRuntimeHost>(scene);

                mapHost.Initialize();
                Assert.That(fixtureHost.TryInitialize(), Is.True);
                FixtureEquipmentPlanState fixturePlans =
                    new FixtureEquipmentPlanState();

                StoreLayoutRuntimeLoader loader =
                    new StoreLayoutRuntimeLoader(
                        mapHost,
                        foundationHost,
                        sidewalkHost,
                        floorHost,
                        fixtureHost,
                        fixturePlans,
                        departmentHost,
                        receivingHost);

                StoreLayoutData proof =
                    CreateProofLayout(mapHost, fixtureHost);
                StoreLayoutLoadResult built = loader.Load(proof);
                Assert.That(built.Succeeded, Is.True, built.Message);

                StoreLayoutData captured =
                    loader.CaptureCurrent(
                        proof.LayoutId,
                        proof.DisplayName);
                StoreLayoutAsset created =
                    StoreLayoutAssetWriter.CreateNew(
                        assetPath,
                        captured);

                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceSynchronousImport);

                StoreLayoutAsset reloaded =
                    AssetDatabase.LoadAssetAtPath<StoreLayoutAsset>(
                        assetPath);
                Assert.That(reloaded, Is.Not.Null);
                Assert.That(created.LayoutId, Is.EqualTo(reloaded.LayoutId));
                Assert.That(
                    StoreLayoutAssetWriter.Matches(reloaded, captured),
                    Is.True);

                Assert.Throws<InvalidOperationException>(
                    () => StoreLayoutAssetWriter.CreateNew(
                        assetPath,
                        captured));

                StoreLayoutLoadResult rebuilt = loader.Load(reloaded);
                Assert.That(rebuilt.Succeeded, Is.True, rebuilt.Message);

                StoreLayoutData afterReload =
                    loader.CaptureCurrent(
                        proof.LayoutId,
                        proof.DisplayName);
                Assert.That(
                    JsonUtility.ToJson(afterReload),
                    Is.EqualTo(JsonUtility.ToJson(captured)));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                OpenEmptyScene();
            }
        }


        private static StoreLayoutData CreateProofLayout(
            GridMapHost mapHost,
            FixtureRuntimeHost fixtureHost)
        {
            StoreLayoutData layout =
                new StoreLayoutData
                {
                    LayoutId = "bigretail.layout.frank.proof",
                    DisplayName = "Frank Roadside Loader Proof",
                    MapId = mapHost.MapDefinition.MapId,
                    MapFingerprint = mapHost.MapFingerprint
                };

            foreach (string regionId in
                     mapHost.LandPolicy.EnumerateOwnedLandRegionIds())
            {
                layout.OwnedLandRegionIds.Add(regionId);
            }

            for (int y = 42; y <= 45; y++)
            {
                for (int x = -21; x <= -18; x++)
                {
                    StoreCellData cell =
                        new StoreCellData(x, y);

                    layout.Foundations.Add(cell);
                    layout.Floors.Add(
                        new StoreFloorData(cell, "tile-1"));
                }
            }

            layout.Sidewalks.Add(
                new StoreCellData(-22, 42));

            StoreEdgeData frontEdge =
                new StoreEdgeData(
                    new StoreCellData(-21, 42),
                    StoreEdgeDirection.NorthEast);

            layout.Walls.Add(
                new StoreWallData(
                    frontEdge,
                    "white",
                    "white"));

            layout.Openings.Add(
                new StoreOpeningData
                {
                    InstanceId = "proof-window",
                    DefinitionId = "fixed-window",
                    Edges =
                        new List<StoreEdgeData>
                        {
                            frontEdge
                        }
                });

            GridPosition fixtureAnchor =
                new GridPosition(-21, 44);
            FixtureDefinitionId fixtureDefinitionId =
                new FixtureDefinitionId("half_shelf");

            Assert.That(
                fixtureHost.Definitions.TryGetDefinition(
                    fixtureDefinitionId,
                    out FixtureDefinition definition),
                Is.True);

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    fixtureAnchor,
                    FixtureOrientation.North);

            StoreFixtureData fixture =
                new StoreFixtureData
                {
                    InstanceId = "proof-shelf",
                    DefinitionId = fixtureDefinitionId.Value,
                    AnchorCell = new StoreCellData(
                        fixtureAnchor.X,
                        fixtureAnchor.Y,
                        fixtureAnchor.Level),
                    Orientation = StoreOrientation.North
                };

            for (int index = 0;
                 index < footprint.CellCount;
                 index++)
            {
                GridPosition cell = footprint.GetCell(index);
                fixture.OccupiedCells.Add(
                    new StoreCellData(
                        cell.X,
                        cell.Y,
                        cell.Level));
            }

            layout.Fixtures.Add(fixture);

            GridPosition planAnchor =
                new GridPosition(-19, 45);
            FixtureFootprint planFootprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    planAnchor,
                    FixtureOrientation.North);
            StoreFixturePlanData fixturePlan =
                new StoreFixturePlanData
                {
                    InstanceId = "proof-planned-shelf",
                    DefinitionId = fixtureDefinitionId.Value,
                    AnchorCell = new StoreCellData(
                        planAnchor.X,
                        planAnchor.Y,
                        planAnchor.Level),
                    Orientation = StoreOrientation.North
                };

            for (int index = 0;
                 index < planFootprint.CellCount;
                 index++)
            {
                GridPosition cell = planFootprint.GetCell(index);
                fixturePlan.OccupiedCells.Add(
                    new StoreCellData(
                        cell.X,
                        cell.Y,
                        cell.Level));
            }

            layout.FixturePlans.Add(fixturePlan);

            // Fixture placement is supported by foundation. A decorative
            // finish may be painted later without invalidating the authored
            // building layout.
            for (int index = 0;
                 index < fixture.OccupiedCells.Count;
                 index++)
            {
                StoreCellData fixtureCell =
                    fixture.OccupiedCells[index];

                layout.Floors.RemoveAll(
                    floor => floor.Cell == fixtureCell);
            }

            layout.Departments.Add(
                new StoreDepartmentData
                {
                    InstanceId = "proof-dry-goods",
                    DefinitionId = "dry_goods",
                    Cells =
                        new List<StoreCellData>
                        {
                            new StoreCellData(-19, 43)
                        }
                });
            layout.ReceivingCells.Add(
                new StoreCellData(-18, 45));

            return layout;
        }


        private static int ReadConstructionHistoryCount(
            Scene scene)
        {
            MonoBehaviour historyHost =
                FindMonoBehaviour(
                    scene,
                    "BigRetail.Construction.Unity.History."
                    + "ConstructionHistoryHost");

            MethodInfo tryInitialize =
                historyHost.GetType().GetMethod(
                    "TryInitialize",
                    BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo historyProperty =
                historyHost.GetType().GetProperty(
                    "History",
                    BindingFlags.Instance | BindingFlags.Public);

            Assert.That(tryInitialize, Is.Not.Null);
            Assert.That(historyProperty, Is.Not.Null);
            Assert.That(
                (bool)tryInitialize.Invoke(
                    historyHost,
                    Array.Empty<object>()),
                Is.True);

            object history = historyProperty.GetValue(historyHost);
            PropertyInfo undoCountProperty =
                history.GetType().GetProperty(
                    "UndoCount",
                    BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo redoCountProperty =
                history.GetType().GetProperty(
                    "RedoCount",
                    BindingFlags.Instance | BindingFlags.Public);

            Assert.That(undoCountProperty, Is.Not.Null);
            Assert.That(redoCountProperty, Is.Not.Null);

            return (int)undoCountProperty.GetValue(history)
                + (int)redoCountProperty.GetValue(history);
        }


        private static T FindRequired<T>(
            Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component =
                    root.GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            Assert.Fail(
                $"Scene '{scene.path}' is missing {typeof(T).FullName}.");
            return null;
        }


        private static MonoBehaviour FindMonoBehaviour(
            Scene scene,
            string typeName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MonoBehaviour[] components =
                    root.GetComponentsInChildren<MonoBehaviour>(true);

                for (int index = 0;
                     index < components.Length;
                     index++)
                {
                    MonoBehaviour component = components[index];

                    if (component != null
                        && component.GetType().FullName == typeName)
                    {
                        return component;
                    }
                }
            }

            Assert.Fail(
                $"Scene '{scene.path}' is missing {typeName}.");
            return null;
        }


        private static void OpenEmptyScene()
        {
            try
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
            catch (InvalidOperationException)
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.DefaultGameObjects,
                    NewSceneMode.Single);
            }
        }
    }
}
