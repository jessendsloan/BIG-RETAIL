using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.Core.Session
{
    /// <summary>
    /// Unity-side host for the current GameSession.
    /// Creates the session and carries it between scenes.
    /// </summary>
    public sealed class GameSessionHost : MonoBehaviour
    {
        private static GameSessionHost existingHost;
        private bool isLoadingScene;

        [Header("Scene Configuration")]
        [SerializeField]
        private string campaignSceneName = "FrankRoadside";

        [SerializeField]
        private string gameplaySceneName = "Gameplay";

        public static GameSessionHost Instance => existingHost;

        public static bool HasActiveSession =>
            existingHost != null && existingHost.CurrentSession != null;

        /// <summary>
        /// Direct Gameplay scene launches remain a sandbox so the established
        /// construction and simulation workflow keeps working in the Editor.
        /// </summary>
        public static GameMode ActiveMode => HasActiveSession
            ? existingHost.CurrentSession.Mode
            : GameMode.Sandbox;

        public GameSession CurrentSession { get; private set; }

        public string GetStartingSceneName(GameMode mode)
        {
            return mode == GameMode.Campaign
                ? campaignSceneName
                : gameplaySceneName;
        }

        /// <summary>
        /// Creates a real session around an already-loaded play scene.
        /// This is used by controlled development launchers that intentionally
        /// skip the player-facing start screen.
        /// </summary>
        public static GameSessionHost StartSessionInLoadedScene(GameMode mode)
        {
            if (!IsKnownMode(mode))
            {
                Debug.LogError(
                    $"Cannot start an unknown Big Retail game mode: {mode}.");
                return null;
            }

            GameSessionHost host = existingHost;

            if (host == null)
            {
                GameObject hostObject = new GameObject(
                    "GameSessionHost (Quick Start)");
                host = hostObject.AddComponent<GameSessionHost>();

                // Awake runs immediately in Play Mode. Keeping this fallback
                // makes the ownership explicit for lifecycle-based tests and
                // unusual Editor configurations as well.
                existingHost ??= host;
            }

            host.CurrentSession = new GameSession(mode);
            host.isLoadingScene = false;

            Debug.Log(
                $"Quick-started Big Retail in {mode} mode with the loaded scene.",
                host);

            return host;
        }

        private void Awake()
        {
            // Prevent two session hosts from existing simultaneously.
            if (existingHost != null && existingHost != this)
            {
                Destroy(gameObject);
                return;
            }

            existingHost = this;

            // Keep the host and its session alive when the menu scene closes.
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewSession(GameMode mode)
        {
            if (isLoadingScene)
            {
                return;
            }

            if (!IsKnownMode(mode))
            {
                Debug.LogError($"Cannot start an unknown Big Retail game mode: {mode}.");
                return;
            }

            string startingSceneName = GetStartingSceneName(mode);

            if (string.IsNullOrWhiteSpace(startingSceneName))
            {
                Debug.LogError(
                    $"GameSessionHost does not have a starting scene "
                    + $"configured for {mode} mode."
                );

                return;
            }

            CurrentSession = new GameSession(mode);
            isLoadingScene = true;

            Debug.Log(
                $"Starting Big Retail session in {mode} mode at "
                + $"'{startingSceneName}'.");

            SceneManager.LoadScene(startingSceneName);
        }

        private static bool IsKnownMode(GameMode mode)
        {
            return System.Enum.IsDefined(typeof(GameMode), mode);
        }

        private void OnDestroy()
        {
            if (existingHost == this)
            {
                existingHost = null;
            }
        }
    }
}
