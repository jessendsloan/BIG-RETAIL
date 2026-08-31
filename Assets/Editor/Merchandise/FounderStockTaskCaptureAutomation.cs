using System;
using System.Collections.Generic;
using System.IO;
using BigRetail.CameraControl;
using BigRetail.Core.Session;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Merchandise.Domain;
using BigRetail.Work.Domain;
using BigRetail.Work.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigRetail.Editor.Merchandise
{
    /// <summary>
    /// Runs the Frank Roadside Founder-stocking loop against its real scene,
    /// captures the handled case and finished fixture, and records an
    /// inventory smoke result for command-line validation.
    /// </summary>
    [InitializeOnLoad]
    public static class FounderStockTaskCaptureAutomation
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";
        private const string WorkingCapturePath =
            "Logs/FounderStockTaskWorking.png";
        private const string CompleteCapturePath =
            "Logs/FounderStockTaskComplete.png";
        private const string SmokeResultPath =
            "Logs/FounderStockTaskSmoke.txt";
        private const string SessionKey =
            "BigRetail.FounderStockTaskCapture.Active";
        private const string TargetFixtureValue =
            "D58D297252D749968D57BA9B107DBA1A";
        private const string ProductValue =
            "RIDGEWAY-ORIGINAL-CHIPS-SINGLE";
        private const int CaseCount = 4;
        private const int UnitsPerCase = 12;
        private const float AutomationTimeScale = 4f;
        private const double TimeoutSeconds = 90d;

        private static readonly FixtureInstanceId TargetFixtureId =
            new FixtureInstanceId(TargetFixtureValue);

        private static readonly ProductId ProductId =
            new ProductId(ProductValue);

        private static FounderStockTaskController controller;
        private static FixturePlanogramRuntimeHost planogramHost;
        private static FixtureRuntimeHost fixtureHost;
        private static bool taskStarted;
        private static bool workingCaptureCreated;
        private static double startedAt;


        static FounderStockTaskCaptureAutomation()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                RegisterCallbacks();
            }
        }


        [MenuItem("Big Retail/Merchandise/Capture Founder Stock Task")]
        public static void CaptureFromMenu()
        {
            BeginCapture(exitEditorWhenComplete: false);
        }

        public static void CaptureForAutomation()
        {
            BeginCapture(exitEditorWhenComplete: true);
        }


        private static void BeginCapture(bool exitEditorWhenComplete)
        {
            DeleteIfPresent(WorkingCapturePath);
            DeleteIfPresent(CompleteCapturePath);
            DeleteIfPresent(SmokeResultPath);

            SessionState.SetBool(SessionKey, true);
            SessionState.SetBool(
                SessionKey + ".ExitEditor",
                exitEditorWhenComplete);
            SessionState.SetBool(SessionKey + ".Failed", false);
            DevelopmentSessionBootstrap.ClearRequest();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DevelopmentSessionBootstrap.Arm(GameMode.Campaign);
            RegisterCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange stateChange)
        {
            if (!SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                controller = null;
                planogramHost = null;
                fixtureHost = null;
                taskStarted = false;
                workingCaptureCreated = false;
                startedAt = EditorApplication.timeSinceStartup;
                Time.timeScale = AutomationTimeScale;
                EditorApplication.update -= HandlePlayModeUpdate;
                EditorApplication.update += HandlePlayModeUpdate;
                return;
            }

            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                CompleteCapture();
            }
        }

        private static void HandlePlayModeUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt
                > TimeoutSeconds)
            {
                Fail("Founder stocking timed out before the task completed.");
                StopPlayMode();
                return;
            }

            if (!taskStarted)
            {
                TryStartTask();
                return;
            }

            StockFixtureWorkOrder work = controller.ActiveWork;

            if (work == null)
            {
                Fail("Founder stocking lost its active work order.");
                StopPlayMode();
                return;
            }

            if (!workingCaptureCreated
                && work.Phase == StockFixtureWorkPhase.StockingFixture
                && work.StockedUnitCount > 0)
            {
                if (!ValidateFounderDepthSorting())
                {
                    StopPlayMode();
                    return;
                }

                FocusTargetFixture();
                workingCaptureCreated = TryCapture(
                    WorkingCapturePath);
            }

            if (work.Phase == StockFixtureWorkPhase.Blocked
                || work.Phase == StockFixtureWorkPhase.Cancelled)
            {
                Fail(
                    "Founder stocking stopped early: "
                    + work.StatusMessage);
                StopPlayMode();
                return;
            }

            if (work.Phase != StockFixtureWorkPhase.Completed)
            {
                return;
            }

            FocusTargetFixture();
            bool completeCaptureCreated =
                TryCapture(CompleteCapturePath);
            bool stateValidated = ValidateCompletedState(work);

            if (workingCaptureCreated
                && completeCaptureCreated
                && stateValidated)
            {
                string message =
                    "PASS: Founder stocked 45 Ridgeway chip bags from four "
                    + "physical cases and returned the three-item remainder.";
                File.WriteAllText(
                    GetAbsolutePath(SmokeResultPath),
                    message);
                Debug.Log(message);
            }

            StopPlayMode();
        }

        private static bool TryStartTask()
        {
            controller ??=
                UnityEngine.Object.FindAnyObjectByType<
                    FounderStockTaskController>();
            planogramHost ??=
                UnityEngine.Object.FindAnyObjectByType<
                    FixturePlanogramRuntimeHost>();
            fixtureHost ??=
                UnityEngine.Object.FindAnyObjectByType<FixtureRuntimeHost>();

            if (controller == null
                || planogramHost == null
                || fixtureHost == null
                || !fixtureHost.TryInitialize()
                || !planogramHost.TryInitialize()
                || !controller.IsInitialized
                || !fixtureHost.FixtureState.TryGetFixture(
                    TargetFixtureId,
                    out _)
                || !planogramHost.DisplayInventory.TryGetSnapshot(
                    TargetFixtureId,
                    out FixtureDisplayStockSnapshot snapshot)
                || snapshot.CapacityUnitCount != 45
                || snapshot.StockedUnitCount != 0)
            {
                return false;
            }

            if (!TryFindRackWithSlots(
                    out FixtureInstanceId rackFixtureId))
            {
                Fail("Founder stocking could not find a four-case rack.");
                StopPlayMode();
                return false;
            }

            for (int index = 0; index < CaseCount; index++)
            {
                FixtureBackstockReceiptResult receipt =
                    planogramHost.Backstock.TryReceiveInboundAtRack(
                        rackFixtureId,
                        ProductId,
                        UnitsPerCase);

                if (!receipt.Succeeded)
                {
                    Fail(
                        "Founder stocking could not stage supplier case "
                        + (index + 1)
                        + ": "
                        + receipt.Failure
                        + ".");
                    StopPlayMode();
                    return false;
                }
            }

            if (!controller.TryAssignStockFixture(
                    TargetFixtureId,
                    out string status))
            {
                Fail("Founder stocking could not start: " + status);
                StopPlayMode();
                return false;
            }

            taskStarted = true;
            return true;
        }

        private static bool TryFindRackWithSlots(
            out FixtureInstanceId rackFixtureId)
        {
            foreach (
                FixtureInstance fixture
                in fixtureHost.FixtureState.EnumerateFixtures())
            {
                if (fixture.Definition.StorageProfile
                        .ProvidesBackstockStorage
                    && planogramHost.Backstock
                        .GetRackAvailableCaseSlotCount(fixture.Id)
                        >= CaseCount)
                {
                    rackFixtureId = fixture.Id;
                    return true;
                }
            }

            rackFixtureId = default;
            return false;
        }

        private static bool ValidateCompletedState(
            StockFixtureWorkOrder work)
        {
            if (!planogramHost.DisplayInventory.TryGetSnapshot(
                    TargetFixtureId,
                    out FixtureDisplayStockSnapshot snapshot))
            {
                Fail("Founder stocking lost the target fixture snapshot.");
                return false;
            }

            int remainingBackstock =
                planogramHost.Backstock.GetAvailableQuantity(ProductId);

            if (snapshot.StockedUnitCount != 45
                || snapshot.MissingUnitCount != 0
                || work.StockedUnitCount != 45
                || work.CarriedUnitCount != 0
                || remainingBackstock != 3)
            {
                Fail(
                    "Founder stocking completed with unexpected inventory: "
                    + $"display {snapshot.StockedUnitCount}/"
                    + $"{snapshot.CapacityUnitCount}, "
                    + $"work stocked {work.StockedUnitCount}, "
                    + $"carried {work.CarriedUnitCount}, "
                    + $"backstock {remainingBackstock}.");
                return false;
            }

            return true;
        }


        private static bool ValidateFounderDepthSorting()
        {
            IsometricDepthSortingGroup depthSorting =
                UnityEngine.Object.FindAnyObjectByType<
                    IsometricDepthSortingGroup>();

            if (depthSorting == null
                || !depthSorting.RefreshSortingOrder()
                || !depthSorting.HasAppliedDepth)
            {
                Fail(
                    "Founder stocking did not apply dynamic isometric depth sorting.");
                return false;
            }

            int expectedOrder =
                IsometricRenderOrderResolver.ResolveCell(
                    depthSorting.CurrentDisplayCell);

            if (depthSorting.CurrentSortingOrder != expectedOrder)
            {
                Fail(
                    "Founder depth sorting did not match the shared cell "
                    + $"contract: expected {expectedOrder}, got "
                    + $"{depthSorting.CurrentSortingOrder}.");
                return false;
            }

            return true;
        }

        private static void FocusTargetFixture()
        {
            WallViewSystem wallViews =
                UnityEngine.Object.FindAnyObjectByType<WallViewSystem>();
            FixtureViewSystem fixtureViews =
                UnityEngine.Object.FindAnyObjectByType<FixtureViewSystem>();
            CameraController cameraController =
                UnityEngine.Object.FindAnyObjectByType<CameraController>();
            Camera sceneCamera = Camera.main;

            wallViews?.TrySetDisplayMode(WallDisplayMode.Cutaway);

            if (fixtureViews == null
                || cameraController == null
                || sceneCamera == null
                || !fixtureViews.TryGetRenderers(
                    TargetFixtureId,
                    out IReadOnlyList<SpriteRenderer> renderers)
                || renderers.Count == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;

            for (int index = 1; index < renderers.Count; index++)
            {
                if (renderers[index] != null)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }
            }

            cameraController.SetWorldCenter(bounds.center);
            sceneCamera.orthographicSize = Mathf.Max(
                3.2f,
                bounds.extents.y + 1.7f,
                bounds.extents.x / Mathf.Max(sceneCamera.aspect, 0.01f)
                    + 1.7f);
            cameraController.ClampCurrentView();
        }

        private static bool TryCapture(string relativePath)
        {
            Camera sceneCamera = Camera.main;

            if (sceneCamera == null)
            {
                Fail("Founder stocking capture could not find the camera.");
                return false;
            }

            RenderTexture captureTexture = new RenderTexture(
                1600,
                900,
                24,
                RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(
                captureTexture.width,
                captureTexture.height,
                TextureFormat.RGBA32,
                mipChain: false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = sceneCamera.targetTexture;

            try
            {
                captureTexture.Create();
                sceneCamera.targetTexture = captureTexture;
                sceneCamera.Render();
                RenderTexture.active = captureTexture;
                image.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        captureTexture.width,
                        captureTexture.height),
                    0,
                    0);
                image.Apply();
                File.WriteAllBytes(
                    GetAbsolutePath(relativePath),
                    image.EncodeToPNG());
                return true;
            }
            catch (Exception exception)
            {
                Fail(
                    "Founder stocking screenshot failed: "
                    + exception.Message);
                return false;
            }
            finally
            {
                sceneCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                captureTexture.Release();
                UnityEngine.Object.DestroyImmediate(captureTexture);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void StopPlayMode()
        {
            EditorApplication.update -= HandlePlayModeUpdate;
            Time.timeScale = 1f;
            EditorApplication.isPlaying = false;
        }

        private static void CompleteCapture()
        {
            EditorApplication.update -= HandlePlayModeUpdate;
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            SessionState.SetBool(SessionKey, false);

            bool failed = SessionState.GetBool(
                SessionKey + ".Failed",
                false);
            bool exitEditor = SessionState.GetBool(
                SessionKey + ".ExitEditor",
                false);
            SessionState.SetBool(SessionKey + ".Failed", false);
            SessionState.SetBool(SessionKey + ".ExitEditor", false);
            DevelopmentSessionBootstrap.ClearRequest();

            if (exitEditor)
            {
                bool artifactsExist =
                    File.Exists(GetAbsolutePath(WorkingCapturePath))
                    && File.Exists(GetAbsolutePath(CompleteCapturePath))
                    && File.Exists(GetAbsolutePath(SmokeResultPath));
                EditorApplication.Exit(
                    !failed && artifactsExist
                        ? 0
                        : 3);
            }
        }

        private static void Fail(string message)
        {
            if (!SessionState.GetBool(SessionKey + ".Failed", false))
            {
                SessionState.SetBool(SessionKey + ".Failed", true);
                Debug.LogError(message);
            }
        }

        private static void DeleteIfPresent(string relativePath)
        {
            string absolutePath = GetAbsolutePath(relativePath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }

        private static string GetAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    relativePath));
        }
    }
}
