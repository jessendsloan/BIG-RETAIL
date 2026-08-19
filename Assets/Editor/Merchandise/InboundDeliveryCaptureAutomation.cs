using System;
using System.Collections.Generic;
using System.IO;
using BigRetail.CameraControl;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BigRetail.Editor.Merchandise
{
    /// <summary>
    /// Stages one BIG and one Central order in Gameplay, advances both to
    /// receiving, and captures their separate curbside pallet views.
    /// </summary>
    [InitializeOnLoad]
    public static class InboundDeliveryCaptureAutomation
    {
        private const string GameplayScenePath =
            "Assets/Scenes/Gameplay.unity";
        private const string CapturePath =
            "Logs/InboundSupplierPallets.png";
        private const string SessionKey =
            "BigRetail.InboundDeliveryCapture.Active";
        private const int StageFrame = 10;
        private const int FocusFrame = 20;
        private const int CaptureFrame = 35;
        private const int ExitFrame = 70;

        private static int playFrame;
        private static bool ordersStaged;
        private static bool cameraFocused;
        private static bool screenshotCaptured;


        static InboundDeliveryCaptureAutomation()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                RegisterCallbacks();
            }
        }


        [MenuItem("Big Retail/Merchandise/Capture Inbound Supplier Pallets")]
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
            string absoluteCapturePath = GetAbsoluteCapturePath();

            if (File.Exists(absoluteCapturePath))
            {
                File.Delete(absoluteCapturePath);
            }

            SessionState.SetBool(SessionKey, true);
            SessionState.SetBool(
                SessionKey + ".ExitEditor",
                exitEditorWhenComplete);
            SessionState.SetBool(SessionKey + ".Failed", false);
            EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);
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
                playFrame = 0;
                ordersStaged = false;
                cameraFocused = false;
                screenshotCaptured = false;
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

            playFrame++;

            if (!ordersStaged && playFrame >= StageFrame)
            {
                ordersStaged = TryStageReviewOrders();
            }

            if (ordersStaged
                && !cameraFocused
                && playFrame >= FocusFrame)
            {
                cameraFocused = TryFocusArrivals();
            }

            if (cameraFocused
                && !screenshotCaptured
                && playFrame >= CaptureFrame)
            {
                screenshotCaptured = TryCaptureCamera();
            }

            if (playFrame >= ExitFrame)
            {
                if (!ordersStaged
                    || !cameraFocused
                    || !screenshotCaptured)
                {
                    Fail(
                        "Inbound supplier pallet capture did not complete "
                        + "its stage, focus, and screenshot checks.");
                }

                EditorApplication.update -= HandlePlayModeUpdate;
                EditorApplication.isPlaying = false;
            }
        }

        private static bool TryStageReviewOrders()
        {
            PurchasingRuntimeHost purchasingRuntime =
                UnityEngine.Object.FindAnyObjectByType<
                    PurchasingRuntimeHost>();
            SimulationTimeRuntimeHost timeRuntime =
                UnityEngine.Object.FindAnyObjectByType<
                    SimulationTimeRuntimeHost>();

            if (purchasingRuntime == null
                || timeRuntime == null
                || !purchasingRuntime.TryInitialize())
            {
                return false;
            }

            SetSupplierDraft(
                purchasingRuntime,
                new SupplierId("BIG"),
                requestedPackCount: 2);
            SetSupplierDraft(
                purchasingRuntime,
                new SupplierId("CENTRAL"),
                requestedPackCount: 5);

            if (!purchasingRuntime.TryPlaceDrafts(
                    out IReadOnlyList<PlacedPurchaseOrder> placedOrders,
                    out string error))
            {
                Fail(
                    "Inbound supplier pallet capture could not place its "
                    + $"review orders: {error}");
                return false;
            }

            if (placedOrders.Count != 2)
            {
                Fail(
                    "Inbound supplier pallet capture expected exactly two "
                    + "supplier purchase orders.");
                return false;
            }

            long reviewTime =
                SimulationDateTime.FromCalendar(
                    dayNumber: 2,
                    hour: 12,
                    minute: 0)
                .TotalGameSeconds;
            timeRuntime.RestoreState(
                new SimulationClockState(
                    reviewTime,
                    fractionalGameSecond: 0d,
                    SimulationSpeed.Paused));

            return true;
        }

        private static void SetSupplierDraft(
            PurchasingRuntimeHost purchasingRuntime,
            SupplierId supplierId,
            int requestedPackCount)
        {
            SupplierDefinition supplier =
                purchasingRuntime.Catalog.Suppliers
                    .GetRequired(supplierId);
            SupplierOfferDefinition selectedOffer = null;

            foreach (
                SupplierOfferDefinition offer
                in purchasingRuntime.Catalog.Offers
                    .EnumerateForSupplier(supplierId))
            {
                if (offer.IsAvailable)
                {
                    selectedOffer = offer;
                    break;
                }
            }

            if (selectedOffer == null)
            {
                throw new InvalidOperationException(
                    $"Supplier '{supplier.DisplayName}' has no available offer.");
            }

            int minimumPackCount =
                supplier.MinimumOrderCents <= 0
                    ? 1
                    : checked((int)(
                        (supplier.MinimumOrderCents
                            + selectedOffer.PackPriceCents - 1)
                        / selectedOffer.PackPriceCents));
            int packCount = Mathf.Max(
                requestedPackCount,
                minimumPackCount);
            purchasingRuntime.Purchasing.SetPurchasePackCount(
                selectedOffer.Id,
                packCount);
        }

        private static bool TryFocusArrivals()
        {
            InboundDeliveryLoadView[] loadViews =
                UnityEngine.Object.FindObjectsByType<
                    InboundDeliveryLoadView>();

            if (loadViews.Length != 2)
            {
                return false;
            }

            if (loadViews[0].OrderNumber == loadViews[1].OrderNumber
                || loadViews[0].SupplierId == loadViews[1].SupplierId
                || loadViews[0].StagingCell == loadViews[1].StagingCell)
            {
                Fail(
                    "Two supplier orders did not produce two distinct "
                    + "curbside pallet loads.");
                return false;
            }

            Bounds bounds = CalculateCombinedBounds(loadViews);
            CameraController cameraController =
                UnityEngine.Object.FindAnyObjectByType<
                    CameraController>();
            Camera sceneCamera = Camera.main;

            if (cameraController == null || sceneCamera == null)
            {
                Fail(
                    "Inbound supplier pallet capture could not find the "
                    + "Gameplay camera.");
                return false;
            }

            cameraController.SetWorldCenter(bounds.center);
            sceneCamera.orthographicSize = Mathf.Max(
                3.25f,
                bounds.extents.y + 1.4f,
                bounds.extents.x / Mathf.Max(sceneCamera.aspect, 0.01f)
                    + 1.4f);
            cameraController.ClampCurrentView();
            return true;
        }

        private static Bounds CalculateCombinedBounds(
            IReadOnlyList<InboundDeliveryLoadView> loadViews)
        {
            Renderer[] firstRenderers =
                loadViews[0].GetComponentsInChildren<Renderer>();
            Bounds bounds = firstRenderers[0].bounds;

            for (int loadIndex = 0;
                 loadIndex < loadViews.Count;
                 loadIndex++)
            {
                Renderer[] renderers =
                    loadViews[loadIndex]
                        .GetComponentsInChildren<Renderer>();

                for (int rendererIndex = 0;
                     rendererIndex < renderers.Length;
                     rendererIndex++)
                {
                    bounds.Encapsulate(renderers[rendererIndex].bounds);
                }
            }

            return bounds;
        }

        private static bool TryCaptureCamera()
        {
            Camera sceneCamera = Camera.main;

            if (sceneCamera == null)
            {
                Fail(
                    "Inbound supplier pallet capture lost the Gameplay camera.");
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
                    GetAbsoluteCapturePath(),
                    image.EncodeToPNG());
                Debug.Log(
                    $"Captured inbound supplier pallet review image at "
                    + $"'{GetAbsoluteCapturePath()}'.");
                return true;
            }
            catch (Exception exception)
            {
                Fail(
                    "Inbound supplier pallet screenshot failed: "
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

            if (exitEditor)
            {
                EditorApplication.Exit(
                    !failed && File.Exists(GetAbsoluteCapturePath())
                        ? 0
                        : 3);
            }
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(SessionKey + ".Failed", true);
            Debug.LogError(message);
        }

        private static string GetAbsoluteCapturePath()
        {
            return Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    CapturePath));
        }
    }
}
