using System;
using System.Collections.Generic;
using System.IO;
using BigRetail.CameraControl;
using BigRetail.Core.Session;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Navigation;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Work.Domain;
using BigRetail.Work.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigRetail.Editor.Merchandise
{
    /// <summary>
    /// Exercises the opening Founder Receiving task against Frank's real
    /// campaign scene. The smoke proof covers the roadside spawn, shared
    /// constructed-surface navigation, four visible case trips, and the
    /// resulting physical rack inventory.
    /// </summary>
    [InitializeOnLoad]
    public static class FounderPutAwayCaptureAutomation
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string ArrivalCapturePath =
            "Logs/FounderPutAwayArrival.png";

        private const string WorkingCapturePath =
            "Logs/FounderPutAwayWorking.png";

        private const string CompleteCapturePath =
            "Logs/FounderPutAwayComplete.png";

        private const string SmokeResultPath =
            "Logs/FounderPutAwaySmoke.txt";

        private const string SessionKey =
            "BigRetail.FounderPutAwayCapture.Active";

        private const string ProductValue =
            "RIDGEWAY-ORIGINAL-CHIPS-SINGLE";

        private const int ExpectedCaseCount = 4;
        private const int UnitsPerCase = 12;
        private const float AutomationTimeScale = 4f;
        private const double TimeoutSeconds = 90d;

        private static readonly ProductId ProductId =
            new ProductId(ProductValue);

        private static FounderStockTaskController controller;
        private static PurchasingRuntimeHost purchasingHost;
        private static GridNavigationSurfaceHost navigationHost;
        private static bool taskStarted;
        private static bool workingCaptureCreated;
        private static double startedAt;


        static FounderPutAwayCaptureAutomation()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                RegisterCallbacks();
            }
        }


        [MenuItem("Big Retail/Merchandise/Capture Founder Put Away Task")]
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
            DeleteIfPresent(ArrivalCapturePath);
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
                purchasingHost = null;
                navigationHost = null;
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
                Fail("Founder put-away timed out before completion.");
                StopPlayMode();
                return;
            }

            if (!taskStarted)
            {
                TryStartTask();
                return;
            }

            PutAwayDeliveryWorkOrder work =
                controller.ActivePutAwayWork;

            if (work == null)
            {
                Fail("Founder put-away lost its active work order.");
                StopPlayMode();
                return;
            }

            if (!workingCaptureCreated
                && work.Phase
                    == PutAwayDeliveryWorkPhase.TravelingToRack
                && work.CarriedUnitCount == UnitsPerCase)
            {
                FocusFounder();
                workingCaptureCreated =
                    TryCapture(WorkingCapturePath);
            }

            if (work.Phase == PutAwayDeliveryWorkPhase.Blocked
                || work.Phase == PutAwayDeliveryWorkPhase.Cancelled)
            {
                Fail(
                    "Founder put-away stopped early: "
                    + work.StatusMessage);
                StopPlayMode();
                return;
            }

            if (work.Phase != PutAwayDeliveryWorkPhase.Completed)
            {
                return;
            }

            FocusFounder();
            bool completeCaptureCreated =
                TryCapture(CompleteCapturePath);
            bool stateValidated = ValidateCompletedState(work);

            if (workingCaptureCreated
                && completeCaptureCreated
                && stateValidated)
            {
                string message =
                    "PASS: Founder spawned at the trailer, traversed only "
                    + "authored foundations and sidewalks, and put four "
                    + "12-unit Ridgeway cases into physical rack storage.";
                File.WriteAllText(
                    GetAbsolutePath(SmokeResultPath),
                    message);
                Debug.Log(message);
            }

            StopPlayMode();
        }


        private static void TryStartTask()
        {
            controller ??=
                UnityEngine.Object.FindAnyObjectByType<
                    FounderStockTaskController>();
            purchasingHost ??=
                UnityEngine.Object.FindAnyObjectByType<
                    PurchasingRuntimeHost>();
            navigationHost ??=
                UnityEngine.Object.FindAnyObjectByType<
                    GridNavigationSurfaceHost>();

            if (controller == null
                || purchasingHost == null
                || navigationHost == null
                || !purchasingHost.IsInitialized
                || purchasingHost.Fulfillment == null
                || !navigationHost.TryInitialize()
                || !controller.IsInitialized)
            {
                return;
            }

            GridPosition founderCell = controller.FounderCell;

            if (founderCell != new GridPosition(-20, 22, 0)
                || !navigationHost.CanStandAt(founderCell)
                || !navigationHost.CanStandAt(
                    new GridPosition(-20, 28, 0))
                || navigationHost.CanStandAt(
                    new GridPosition(-21, 22, 0)))
            {
                Fail(
                    "Founder or the opening navigation surface did not "
                    + "match the authored trailer path.");
                StopPlayMode();
                return;
            }

            InboundDeliveryLoad targetLoad = null;

            foreach (
                InboundDeliveryLoad load
                in purchasingHost.Fulfillment
                    .EnumerateReadyDeliveries())
            {
                targetLoad = load;
                break;
            }

            if (targetLoad == null
                || targetLoad.PurchasePackCount != ExpectedCaseCount
                || targetLoad.RemainingUnitCount
                    != ExpectedCaseCount * UnitsPerCase)
            {
                Fail("Opening Receiving did not contain four chip cases.");
                StopPlayMode();
                return;
            }

            FocusFounder();

            if (!TryCapture(ArrivalCapturePath))
            {
                StopPlayMode();
                return;
            }

            if (!controller.TryAssignReceivingLoad(
                    targetLoad.OrderNumber,
                    out string status))
            {
                Fail("Founder put-away could not start: " + status);
                StopPlayMode();
                return;
            }

            taskStarted = true;
        }


        private static bool ValidateCompletedState(
            PutAwayDeliveryWorkOrder work)
        {
            int backstockUnitCount =
                UnityEngine.Object.FindAnyObjectByType<
                        BigRetail.Map.Unity.Fixtures
                            .FixturePlanogramRuntimeHost>()
                    ?.Backstock.GetAvailableQuantity(ProductId)
                ?? -1;

            if (work.PlacedCaseCount != ExpectedCaseCount
                || work.PlacedUnitCount
                    != ExpectedCaseCount * UnitsPerCase
                || work.CarriedUnitCount != 0
                || purchasingHost.StagedReadyOrderCount != 0
                || purchasingHost.StagedReadyUnitCount != 0
                || backstockUnitCount
                    != ExpectedCaseCount * UnitsPerCase)
            {
                Fail(
                    "Founder put-away completed with unexpected state: "
                    + $"cases {work.PlacedCaseCount}, "
                    + $"units {work.PlacedUnitCount}, "
                    + $"carried {work.CarriedUnitCount}, "
                    + $"staged orders "
                    + $"{purchasingHost.StagedReadyOrderCount}, "
                    + $"staged units "
                    + $"{purchasingHost.StagedReadyUnitCount}, "
                    + $"backstock {backstockUnitCount}.");
                return false;
            }

            return true;
        }

        private static void FocusFounder()
        {
            CameraController cameraController =
                UnityEngine.Object.FindAnyObjectByType<
                    CameraController>();
            Camera sceneCamera = Camera.main;

            if (controller == null
                || controller.FounderTransform == null
                || cameraController == null
                || sceneCamera == null)
            {
                return;
            }

            cameraController.SetWorldCenter(
                controller.FounderTransform.position);
            sceneCamera.orthographicSize = 5.2f;
            cameraController.ClampCurrentView();
        }


        private static bool TryCapture(string relativePath)
        {
            Camera sceneCamera = Camera.main;

            if (sceneCamera == null)
            {
                Fail("Founder put-away capture could not find the camera.");
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
                    "Founder put-away screenshot failed: "
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
                    File.Exists(GetAbsolutePath(ArrivalCapturePath))
                    && File.Exists(GetAbsolutePath(WorkingCapturePath))
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
