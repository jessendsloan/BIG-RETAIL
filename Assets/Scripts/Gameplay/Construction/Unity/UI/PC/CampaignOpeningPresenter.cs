using BigRetail.Core.Session;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Presents the campaign's opening transmission, temporarily pauses the
    /// authoritative clock, and leaves the first campaign objective visible.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(357)]
    public sealed class CampaignOpeningPresenter : MonoBehaviour
    {
        private const string SpeakerName = "MILTON BIG";
        private const string ObjectiveTitle = "Make It a Store";
        private const string ObjectiveDescription =
            "Build a store shell with an entrance and room for merchandise.";
        private const int DialoguePageCount = 3;

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private SimulationTimeRuntimeHost timeHost;

        private CampaignOpeningView boundView;
        private SimulationSpeed speedBeforeDialogue =
            SimulationSpeed.OneTimes;
        private bool ownsSimulationPause;
        private bool referencesAreValid;


        private void Reset()
        {
            documentHost =
                GetComponent<ConstructionToolbarDocumentHost>();
            timeHost =
                GetComponent<SimulationTimeRuntimeHost>();
        }

        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost =
                    GetComponent<ConstructionToolbarDocumentHost>();
            }

            if (timeHost == null)
            {
                timeHost =
                    GetComponent<SimulationTimeRuntimeHost>();
            }

            referencesAreValid = ValidateReferences();
        }

        private void OnEnable()
        {
            if (!referencesAreValid)
            {
                return;
            }

            documentHost.CampaignOpeningViewReady +=
                HandleViewReady;
            timeHost.Initialized +=
                HandleTimeInitialized;

            if (documentHost.HasCampaignOpeningView)
            {
                BindView(documentHost.CampaignOpeningView);
            }
        }

        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.CampaignOpeningViewReady -=
                    HandleViewReady;
            }

            if (timeHost != null)
            {
                timeHost.Initialized -=
                    HandleTimeInitialized;
            }

            RestoreSimulationSpeed();
            UnbindView();
        }


        private void HandleViewReady(CampaignOpeningView view)
        {
            BindView(view);
        }

        private void HandleTimeInitialized()
        {
            Refresh();
        }

        private void HandleContinueRequested()
        {
            GameSession session = TryGetCampaignSession();
            if (session == null)
            {
                return;
            }

            session.CampaignOpening.Advance();
            Refresh();
        }

        private void HandleSkipRequested()
        {
            GameSession session = TryGetCampaignSession();
            if (session == null)
            {
                return;
            }

            session.CampaignOpening.Skip();
            Refresh();
        }

        private void BindView(CampaignOpeningView view)
        {
            UnbindView();
            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.ContinueRequested +=
                HandleContinueRequested;
            boundView.SkipRequested +=
                HandleSkipRequested;
            Refresh();
        }

        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.ContinueRequested -=
                HandleContinueRequested;
            boundView.SkipRequested -=
                HandleSkipRequested;
            boundView = null;
        }

        private void Refresh()
        {
            if (boundView == null)
            {
                return;
            }

            GameSession session = TryGetCampaignSession();
            if (session == null)
            {
                boundView.SetDialogueVisible(false);
                boundView.SetObjectiveVisible(false);
                RestoreSimulationSpeed();
                return;
            }

            CampaignOpeningProgress progress =
                session.CampaignOpening;

            if (progress.IsComplete)
            {
                boundView.SetDialogueVisible(false);
                boundView.SetObjective(
                    ObjectiveTitle,
                    ObjectiveDescription);
                boundView.SetObjectiveVisible(true);
                RestoreSimulationSpeed();
                return;
            }

            boundView.SetObjectiveVisible(false);
            SetDialogueForBeat(progress.CurrentBeat);
            boundView.SetDialogueVisible(true);
            PauseSimulation();
        }

        private void SetDialogueForBeat(CampaignOpeningBeat beat)
        {
            string dialogue;
            int pageNumber;

            switch (beat)
            {
                case CampaignOpeningBeat.Opportunity:
                    pageNumber = 1;
                    dialogue =
                        "So, you want to open a store, huh? All right. You "
                        + "can use one of my lots. Let’s see what you make "
                        + "of it.";
                    break;

                case CampaignOpeningBeat.Financing:
                    pageNumber = 2;
                    dialogue =
                        "The property’s yours to operate. I handled the "
                        + "paperwork, and BIG Finance put $2,500 in your "
                        + "account to get you moving.";
                    break;

                case CampaignOpeningBeat.FirstAssignment:
                    pageNumber = 3;
                    dialogue =
                        "Build something capable of doing business. Give "
                        + "people a way in and somewhere to spend their "
                        + "money. We’ll talk merchandise once you’ve got "
                        + "shelves to put it on.";
                    break;

                default:
                    return;
            }

            boundView.SetDialogue(
                SpeakerName,
                dialogue,
                pageNumber,
                DialoguePageCount,
                pageNumber == DialoguePageCount);
        }

        private void PauseSimulation()
        {
            if (ownsSimulationPause
                || !timeHost.IsInitialized)
            {
                return;
            }

            speedBeforeDialogue = timeHost.Clock.Speed;
            ownsSimulationPause = true;
            timeHost.SetSpeed(SimulationSpeed.Paused);
        }

        private void RestoreSimulationSpeed()
        {
            if (!ownsSimulationPause)
            {
                return;
            }

            if (timeHost != null
                && timeHost.IsInitialized)
            {
                timeHost.SetSpeed(speedBeforeDialogue);
            }

            ownsSimulationPause = false;
        }

        private static GameSession TryGetCampaignSession()
        {
            if (!GameSessionHost.HasActiveSession)
            {
                return null;
            }

            GameSession session =
                GameSessionHost.Instance.CurrentSession;

            return session.IsCampaign
                ? session
                : null;
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "CampaignOpeningPresenter has no document host assigned.",
                    this);
                isValid = false;
            }

            if (timeHost == null)
            {
                Debug.LogError(
                    "CampaignOpeningPresenter has no time host assigned.",
                    this);
                isValid = false;
            }

            return isValid;
        }
    }
}
