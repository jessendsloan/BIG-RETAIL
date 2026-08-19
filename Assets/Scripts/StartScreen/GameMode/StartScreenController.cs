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

        public void StartCampaignMode()
        {
            StartMode(GameMode.Campaign);
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
            GameSessionHost host = sessionHost != null
                ? sessionHost
                : GameSessionHost.Instance;

            if (host == null)
            {
                Debug.LogError(
                    "StartScreenController has no GameSessionHost assigned."
                );

                return;
            }

            host.StartNewSession(mode);
        }
    }
}
