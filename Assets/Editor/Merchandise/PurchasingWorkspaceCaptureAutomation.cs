using System.IO;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Purchasing.Unity;
using BigRetail.Purchasing.Unity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Editor.Merchandise
{
    /// <summary>
    /// Small review utility that captures isolated commercial UI labs after
    /// their runtime documents have had time to build.
    /// </summary>
    [InitializeOnLoad]
    public static class PurchasingWorkspaceCaptureAutomation
    {
        private const string PurchasingScenePath =
            "Assets/Scenes/Labs/PurchasingWorkspaceLab.unity";
        private const string PurchasingCapturePath =
            "Logs/PurchasingWorkspaceLab.png";
        private const string DirectoryScenePath =
            "Assets/Scenes/Labs/CommercialDirectoryLab.unity";
        private const string DirectoryBrandsCapturePath =
            "Logs/CommercialDirectoryBrands.png";
        private const string DirectorySuppliersCapturePath =
            "Logs/CommercialDirectorySuppliers.png";
        private const string GameplayScenePath =
            "Assets/Scenes/Gameplay.unity";
        private const string GameplayCapturePath =
            "Logs/PurchasingGameplay.png";
        private const string SessionKey =
            "BigRetail.PurchasingWorkspaceCapture.Active";
        private const int CaptureFrame = 30;
        private const int ExitFrame = 75;

        private static int playFrame;
        private static bool screenshotRequested;
        private static bool directorySectionApplied;
        private static RenderTexture captureTexture;
        private static PanelSettings runtimePanelSettings;


        static PurchasingWorkspaceCaptureAutomation()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                RegisterCallbacks();
            }
        }


        [MenuItem("Big Retail/Merchandise/Capture Purchasing Workspace Lab")]
        public static void CaptureFromMenu()
        {
            BeginCapture(
                PurchasingScenePath,
                PurchasingCapturePath,
                null,
                openGameplayOverlay: false,
                exitEditorWhenComplete: false);
        }

        public static void CaptureForAutomation()
        {
            BeginCapture(
                PurchasingScenePath,
                PurchasingCapturePath,
                null,
                openGameplayOverlay: false,
                exitEditorWhenComplete: true);
        }

        [MenuItem("Big Retail/Merchandise/Capture Commercial Directory Brands")]
        public static void CaptureDirectoryBrandsFromMenu()
        {
            BeginCapture(
                DirectoryScenePath,
                DirectoryBrandsCapturePath,
                CommercialDirectorySection.Brands,
                openGameplayOverlay: false,
                exitEditorWhenComplete: false);
        }

        [MenuItem("Big Retail/Merchandise/Capture Commercial Directory Suppliers")]
        public static void CaptureDirectorySuppliersFromMenu()
        {
            BeginCapture(
                DirectoryScenePath,
                DirectorySuppliersCapturePath,
                CommercialDirectorySection.Suppliers,
                openGameplayOverlay: false,
                exitEditorWhenComplete: false);
        }

        public static void CaptureDirectoryBrandsForAutomation()
        {
            BeginCapture(
                DirectoryScenePath,
                DirectoryBrandsCapturePath,
                CommercialDirectorySection.Brands,
                openGameplayOverlay: false,
                exitEditorWhenComplete: true);
        }

        public static void CaptureDirectorySuppliersForAutomation()
        {
            BeginCapture(
                DirectoryScenePath,
                DirectorySuppliersCapturePath,
                CommercialDirectorySection.Suppliers,
                openGameplayOverlay: false,
                exitEditorWhenComplete: true);
        }

        [MenuItem("Big Retail/Merchandise/Capture Purchasing In Gameplay")]
        public static void CaptureGameplayFromMenu()
        {
            BeginCapture(
                GameplayScenePath,
                GameplayCapturePath,
                null,
                openGameplayOverlay: true,
                exitEditorWhenComplete: false);
        }

        public static void CaptureGameplayForAutomation()
        {
            BeginCapture(
                GameplayScenePath,
                GameplayCapturePath,
                null,
                openGameplayOverlay: true,
                exitEditorWhenComplete: true);
        }


        private static void BeginCapture(
            string scenePath,
            string capturePath,
            CommercialDirectorySection? directorySection,
            bool openGameplayOverlay,
            bool exitEditorWhenComplete)
        {
            string absoluteCapturePath = GetAbsoluteCapturePath(capturePath);

            if (File.Exists(absoluteCapturePath))
            {
                File.Delete(absoluteCapturePath);
            }

            SessionState.SetBool(SessionKey, true);
            SessionState.SetBool(
                SessionKey + ".ExitEditor",
                exitEditorWhenComplete);
            SessionState.SetString(SessionKey + ".CapturePath", capturePath);
            SessionState.SetBool(
                SessionKey + ".OpenGameplayOverlay",
                openGameplayOverlay);
            SessionState.SetBool(SessionKey + ".Failed", false);
            SessionState.SetInt(
                SessionKey + ".DirectorySection",
                directorySection.HasValue
                    ? (int)directorySection.Value
                    : -1);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RegisterCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
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
                screenshotRequested = false;
                directorySectionApplied = false;

                if (!ShouldOpenGameplayOverlay())
                {
                    ConfigureCaptureTarget();
                }
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

            if (!directorySectionApplied && playFrame >= 10)
            {
                ApplyRequestedDirectorySection();
                ApplyRequestedGameplayOverlay();

                if (ShouldOpenGameplayOverlay())
                {
                    ConfigureCaptureTarget();
                }

                directorySectionApplied = true;
            }

            if (!screenshotRequested && playFrame >= CaptureFrame)
            {
                CaptureRenderTexture();
                screenshotRequested = true;
            }

            if (playFrame >= ExitFrame)
            {
                EditorApplication.update -= HandlePlayModeUpdate;
                EditorApplication.isPlaying = false;
            }
        }

        private static void CompleteCapture()
        {
            string absoluteCapturePath = GetActiveAbsoluteCapturePath();
            EditorApplication.update -= HandlePlayModeUpdate;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            SessionState.SetBool(SessionKey, false);
            ReleaseCaptureTarget();

            bool exitEditor =
                SessionState.GetBool(SessionKey + ".ExitEditor", false);
            SessionState.SetBool(SessionKey + ".ExitEditor", false);
            SessionState.EraseString(SessionKey + ".CapturePath");
            SessionState.EraseInt(SessionKey + ".DirectorySection");
            bool failed = SessionState.GetBool(
                SessionKey + ".Failed",
                false);
            SessionState.SetBool(SessionKey + ".Failed", false);
            SessionState.SetBool(
                SessionKey + ".OpenGameplayOverlay",
                false);

            if (exitEditor)
            {
                EditorApplication.Exit(
                    File.Exists(absoluteCapturePath) && !failed ? 0 : 3);
            }
        }

        private static string GetActiveAbsoluteCapturePath()
        {
            string capturePath = SessionState.GetString(
                SessionKey + ".CapturePath",
                PurchasingCapturePath);
            return GetAbsoluteCapturePath(capturePath);
        }

        private static string GetAbsoluteCapturePath(string capturePath)
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", capturePath));
        }

        private static void ConfigureCaptureTarget()
        {
            PanelRenderer panelRenderer;

            if (ShouldOpenGameplayOverlay())
            {
                GameObject workspace =
                    GameObject.Find("PurchasingWorkspaceUI");
                panelRenderer = workspace != null
                    ? workspace.GetComponent<PanelRenderer>()
                    : null;
            }
            else
            {
                panelRenderer =
                    Object.FindAnyObjectByType<PanelRenderer>();
            }

            if (panelRenderer == null || panelRenderer.panelSettings == null)
            {
                Debug.LogError(
                    "Purchasing capture could not find its configured PanelRenderer.");
                return;
            }

            captureTexture = new RenderTexture(
                1600,
                900,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "Purchasing Workspace Capture",
                hideFlags = HideFlags.HideAndDontSave
            };
            captureTexture.Create();

            runtimePanelSettings =
                Object.Instantiate(panelRenderer.panelSettings);
            runtimePanelSettings.name = "Purchasing Capture Panel Settings";
            runtimePanelSettings.hideFlags = HideFlags.HideAndDontSave;
            runtimePanelSettings.targetTexture = captureTexture;
            runtimePanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanelSettings.referenceResolution = new Vector2Int(1600, 900);
            panelRenderer.panelSettings = runtimePanelSettings;
        }

        private static void CaptureRenderTexture()
        {
            if (captureTexture == null || !captureTexture.IsCreated())
            {
                Debug.LogError(
                    "Purchasing capture render target was not available.");
                return;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = captureTexture;
            Texture2D image = new Texture2D(
                captureTexture.width,
                captureTexture.height,
                TextureFormat.RGBA32,
                false);
            image.ReadPixels(
                new Rect(0, 0, captureTexture.width, captureTexture.height),
                0,
                0);
            image.Apply();
            string absoluteCapturePath = GetActiveAbsoluteCapturePath();
            File.WriteAllBytes(
                absoluteCapturePath,
                image.EncodeToPNG());
            Debug.Log($"Captured commercial UI review image at '{absoluteCapturePath}'.");
            Object.DestroyImmediate(image);
            RenderTexture.active = previous;
        }

        private static void ApplyRequestedDirectorySection()
        {
            int sectionValue = SessionState.GetInt(
                SessionKey + ".DirectorySection",
                -1);

            if (sectionValue < 0)
            {
                return;
            }

            CommercialDirectoryPresenter presenter =
                Object.FindAnyObjectByType<CommercialDirectoryPresenter>();

            if (presenter == null)
            {
                Debug.LogError(
                    "Commercial Directory capture could not find its presenter.");
                return;
            }

            presenter.ShowSection((CommercialDirectorySection)sectionValue);
        }

        private static void ApplyRequestedGameplayOverlay()
        {
            if (!ShouldOpenGameplayOverlay())
            {
                return;
            }

            PurchasingGameplayOverlayController controller =
                Object.FindAnyObjectByType<
                    PurchasingGameplayOverlayController>();
            PurchasingRuntimeHost runtimeHost =
                Object.FindAnyObjectByType<PurchasingRuntimeHost>();

            if (controller == null
                || runtimeHost == null
                || !runtimeHost.TryInitialize()
                || runtimeHost.Catalog?.Products.Count != 12)
            {
                SessionState.SetBool(SessionKey + ".Failed", true);
                Debug.LogError(
                    "Gameplay Purchasing smoke check could not initialize the 12-product live runtime.");
                return;
            }

            controller.Open();

            if (!controller.IsOpen)
            {
                SessionState.SetBool(SessionKey + ".Failed", true);
                Debug.LogError(
                    "Gameplay Purchasing smoke check could not open the overlay.");
                return;
            }

            Debug.Log(
                "Gameplay Purchasing smoke check initialized the live 12-product runtime and opened its overlay.");
        }

        private static bool ShouldOpenGameplayOverlay()
        {
            return SessionState.GetBool(
                SessionKey + ".OpenGameplayOverlay",
                false);
        }

        private static void ReleaseCaptureTarget()
        {
            if (runtimePanelSettings != null)
            {
                Object.DestroyImmediate(runtimePanelSettings);
                runtimePanelSettings = null;
            }

            if (captureTexture != null)
            {
                captureTexture.Release();
                Object.DestroyImmediate(captureTexture);
                captureTexture = null;
            }
        }
    }
}
