using BigRetail.Core.Session;
using UnityEngine;

namespace BigRetail.UI.StartScreen
{
    /// <summary>
    /// Receives button presses from the start screen.
    /// </summary>
    public sealed class StartScreenController : MonoBehaviour
    {
        [SerializeField]
        private GameSessionHost sessionHost;

        public void StartStoryMode()
        {
            StartMode(GameMode.Story);
        }

        public void StartSandboxMode()
        {
            StartMode(GameMode.Sandbox);
        }

        public void OpenOptions()
        {
            Debug.Log("Options screen is not implemented yet.");
        }

        private void StartMode(GameMode mode)
        {
            if (sessionHost == null)
            {
                Debug.LogError(
                    "StartScreenController has no GameSessionHost assigned."
                );

                return;
            }

            sessionHost.StartNewSession(mode);
        }
    }
}