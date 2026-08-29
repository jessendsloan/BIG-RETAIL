using System.Collections;
using BigRetail.Core.Session;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
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
        private const string FrankRoadsideMapId =
            "bigretail.map.frank_roadside";
        private const string FrankSpeakerName = "FRANK";
        private const string FrankObjectiveTitle = "Stock the Back Room";
        private const string FrankObjectiveDescription =
            "Choose Merchandise, click the supplier pallet to take one "
            + "Ridgeway chip case, then click a storage rack. Repeat until "
            + "all four cases are stored.";
        private const string FrankCompletedObjectiveTitle =
            "Back Room Stocked";
        private const string FrankCompletedObjectiveDescription =
            "All four Ridgeway chip cases are safely stored on the racks.";
        private const int FrankOpeningRidgewayUnitCount = 48;
        private const int FrankDialoguePageCount = 3;
        private const float FrankRevealDurationSeconds = 1.2f;

        private const string SpeakerName = "MILTON BIG";
        private const string ObjectiveTitle = "Make It a Store";
        private const string ObjectiveDescription =
            "Build a store shell with an entrance and room for merchandise.";
        private const int DialoguePageCount = 3;

        private static readonly ProductId FrankOpeningRidgewayProductId =
            new ProductId("RIDGEWAY-ORIGINAL-CHIPS-SINGLE");

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private SimulationTimeRuntimeHost timeHost;

        [SerializeField]
        private GridMapHost mapHost;

        private FixturePlanogramRuntimeHost planogramRuntimeHost;
        private FixtureBackstockService subscribedBackstock;
        private CampaignOpeningView boundView;
        private Coroutine frankRevealCoroutine;
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
            mapHost =
                FindAnyObjectByType<GridMapHost>();
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

            if (mapHost == null)
            {
                mapHost =
                    FindAnyObjectByType<GridMapHost>();
            }

            planogramRuntimeHost =
                FindAnyObjectByType<FixturePlanogramRuntimeHost>(
                    FindObjectsInactive.Include);

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

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized +=
                    HandlePlanogramInitialized;
                AttachBackstock();
            }

            if (documentHost.HasCampaignOpeningView)
            {
                BindView(documentHost.CampaignOpeningView);
            }
        }

        private void OnDisable()
        {
            StopFrankReveal();

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

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -=
                    HandlePlanogramInitialized;
            }

            DetachBackstock();

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

        private void HandlePlanogramInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            AttachBackstock();
            Refresh();
        }

        private void HandleBackstockContentsChanged()
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

            if (IsFrankRoadside())
            {
                HandleFrankContinueRequested(session);
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

            if (IsFrankRoadside())
            {
                session.FrankRoadsideOpening.Skip();
                StopFrankReveal();
                Refresh();
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

            boundView.SetOpeningOpacity(1f);
            boundView.SetDialogueControlsEnabled(true);
            boundView.SetFrankOpeningStyle(IsFrankRoadside());

            GameSession session = TryGetCampaignSession();
            if (session == null)
            {
                boundView.SetDialogueVisible(false);
                boundView.SetObjectiveVisible(false);
                RestoreSimulationSpeed();
                return;
            }

            if (IsFrankRoadside())
            {
                RefreshFrankOpening(session);
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

        private void HandleFrankContinueRequested(GameSession session)
        {
            FrankRoadsideOpeningProgress progress =
                session.FrankRoadsideOpening;

            progress.Advance();

            if (!progress.IsComplete)
            {
                Refresh();
                return;
            }

            StopFrankReveal();
            boundView.SetDialogueControlsEnabled(false);
            frankRevealCoroutine =
                StartCoroutine(RevealFrankRoadside());
        }

        private void RefreshFrankOpening(GameSession session)
        {
            FrankRoadsideOpeningProgress progress =
                session.FrankRoadsideOpening;

            if (progress.IsComplete)
            {
                boundView.SetDialogueVisible(false);

                if (IsFrankBackroomStocked())
                {
                    boundView.SetObjective(
                        FrankCompletedObjectiveTitle,
                        FrankCompletedObjectiveDescription);
                }
                else
                {
                    boundView.SetObjective(
                        FrankObjectiveTitle,
                        FrankObjectiveDescription);
                }

                boundView.SetObjectiveVisible(true);
                RestoreSimulationSpeed();
                return;
            }

            boundView.SetObjectiveVisible(false);
            SetFrankDialogueForBeat(progress.CurrentBeat);
            boundView.SetDialogueVisible(true);
            PauseSimulation();
        }

        private void SetFrankDialogueForBeat(
            FrankRoadsideOpeningBeat beat)
        {
            string dialogue;
            int pageNumber;

            switch (beat)
            {
                case FrankRoadsideOpeningBeat.WakeUp:
                    pageNumber = 1;
                    dialogue = "Kid... Wake up.";
                    break;

                case FrankRoadsideOpeningBeat.CoverTheStore:
                    pageNumber = 2;
                    dialogue =
                        "I need you to cover the store this morning. "
                        + "I'll be in later.";
                    break;

                case FrankRoadsideOpeningBeat.MoveReceivingToStockroom:
                    pageNumber = 3;
                    dialogue =
                        "Start by getting those chip cases out of Receiving "
                        + "and into the stockroom, nephew.";
                    break;

                default:
                    return;
            }

            bool isFinalPage =
                pageNumber == FrankDialoguePageCount;

            boundView.SetDialogue(
                FrankSpeakerName,
                dialogue,
                pageNumber,
                FrankDialoguePageCount,
                isFinalPage,
                isFinalPage ? "Get to Work" : "Continue");
        }

        private IEnumerator RevealFrankRoadside()
        {
            float elapsedSeconds = 0f;

            while (elapsedSeconds < FrankRevealDurationSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsedSeconds / FrankRevealDurationSeconds);
                boundView?.SetOpeningOpacity(1f - progress);
                yield return null;
            }

            frankRevealCoroutine = null;

            if (boundView == null)
            {
                RestoreSimulationSpeed();
                yield break;
            }

            boundView.SetDialogueVisible(false);
            boundView.SetOpeningOpacity(1f);
            boundView.SetDialogueControlsEnabled(true);
            boundView.SetObjective(
                FrankObjectiveTitle,
                FrankObjectiveDescription);
            boundView.SetObjectiveVisible(true);
            RestoreSimulationSpeed();
        }

        private void StopFrankReveal()
        {
            if (frankRevealCoroutine != null)
            {
                StopCoroutine(frankRevealCoroutine);
                frankRevealCoroutine = null;
            }

            if (boundView != null)
            {
                boundView.SetOpeningOpacity(1f);
                boundView.SetDialogueControlsEnabled(true);
            }
        }

        private bool IsFrankRoadside()
        {
            return mapHost != null
                && mapHost.IsInitialized
                && mapHost.MapDefinition != null
                && mapHost.MapDefinition.MapId == FrankRoadsideMapId;
        }

        private bool IsFrankBackroomStocked()
        {
            return subscribedBackstock != null
                && subscribedBackstock.GetAvailableQuantity(
                    FrankOpeningRidgewayProductId)
                    >= FrankOpeningRidgewayUnitCount;
        }

        private void AttachBackstock()
        {
            FixtureBackstockService nextBackstock =
                planogramRuntimeHost != null
                && planogramRuntimeHost.IsInitialized
                    ? planogramRuntimeHost.Backstock
                    : null;

            if (subscribedBackstock == nextBackstock)
            {
                return;
            }

            DetachBackstock();
            subscribedBackstock = nextBackstock;

            if (subscribedBackstock != null)
            {
                subscribedBackstock.ContentsChanged +=
                    HandleBackstockContentsChanged;
            }
        }

        private void DetachBackstock()
        {
            if (subscribedBackstock == null)
            {
                return;
            }

            subscribedBackstock.ContentsChanged -=
                HandleBackstockContentsChanged;
            subscribedBackstock = null;
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

            if (mapHost == null)
            {
                Debug.LogError(
                    "CampaignOpeningPresenter could not find the active map host.",
                    this);
                isValid = false;
            }

            return isValid;
        }
    }
}
