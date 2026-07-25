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

        [Header("Scene Configuration")]
        [SerializeField]
        private string gameplaySceneName = "Gameplay";

        public GameSession CurrentSession { get; private set; }

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
            if (CurrentSession != null)
            {
                Debug.LogWarning("A Big Retail session already exists.");
                return;
            }

            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError(
                    "GameSessionHost does not have a gameplay scene name."
                );

                return;
            }

            CurrentSession = new GameSession(mode);

            Debug.Log($"Starting Big Retail session in {mode} mode.");

            SceneManager.LoadScene(gameplaySceneName);
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