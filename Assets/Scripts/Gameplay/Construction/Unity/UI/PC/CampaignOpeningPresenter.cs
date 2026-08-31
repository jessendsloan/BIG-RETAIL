using System.Collections;
using BigRetail.Core.Session;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using BigRetail.Work.Unity;
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
        private const string FrankObjectiveTitle = "Put Away the Delivery";
        private const string FrankObjectiveDescription =
            "Choose Merchandise, then click the supplier pallet. The Founder "
            + "will carry all four Ridgeway chip cases from Receiving into "
            + "the stockroom racks.";
        private const string FrankCompletedObjectiveTitle =
            "Chips Stocked";
        private const string FrankCompletedObjectiveDescription =
            "The chip fixture is full with 45 bags, and the final 3 bags "
            + "remain in storage.";
        private const string FrankSalesFloorObjectiveTitle =
            "Stock the Chip Fixture";
        private const string FrankSalesFloorObjectiveDescription =
            "Choose Merchandise, click the glowing Ridgeway chip fixture, "
            + "then choose Have Founder Stock. Frank will carry cases from "
            + "storage and fill all 15 slots.";
        private const int FrankOpeningRidgewayReceivedUnitCount = 48;
        private const int FrankOpeningRidgewayDisplayUnitCount = 45;
        private const int FrankOpeningRidgewayCaseCount = 4;
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
        private FixtureMerchandisingOverlayViewSystem
            merchandisingOverlayViewSystem;
        private FounderStockTaskController founderStockTaskController;
        private FixtureBackstockService subscribedBackstock;
        private FixtureDisplayInventoryService subscribedDisplayInventory;
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
            merchandisingOverlayViewSystem =
                FindAnyObjectByType<
                    FixtureMerchandisingOverlayViewSystem>(
                        FindObjectsInactive.Include);
            founderStockTaskController =
                FindAnyObjectByType<FounderStockTaskController>(
                    FindObjectsInactive.Include);
            merchandisingOverlayViewSystem
                ?.SetObjectiveHighlightEnabled(false);

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
                AttachDisplayInventory();
            }

            if (founderStockTaskController != null)
            {
                founderStockTaskController.StatusChanged +=
                    HandleFounderStatusChanged;
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
            DetachDisplayInventory();

            if (founderStockTaskController != null)
            {
                founderStockTaskController.StatusChanged -=
                    HandleFounderStatusChanged;
            }

            merchandisingOverlayViewSystem
                ?.SetObjectiveHighlightEnabled(false);

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
            AttachDisplayInventory();
            Refresh();
        }

        private void HandleBackstockContentsChanged()
        {
            Refresh();
        }

        private void HandleDisplayStockChanged(
            FixtureInstanceId _)
        {
            Refresh();
        }

        private void HandleFounderStatusChanged()
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
                merchandisingOverlayViewSystem
                    ?.SetObjectiveHighlightEnabled(false);
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

            merchandisingOverlayViewSystem
                ?.SetObjectiveHighlightEnabled(false);

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

            merchandisingOverlayViewSystem
                ?.SetObjectiveHighlightEnabled(false);
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
                SetFrankObjective();
                boundView.SetObjectiveVisible(true);
                RestoreSimulationSpeed();
                return;
            }

            merchandisingOverlayViewSystem
                ?.SetObjectiveHighlightEnabled(false);
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
            SetFrankObjective();
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

        private void SetFrankObjective()
        {
            int backstockUnitCount =
                subscribedBackstock?.GetAvailableQuantity(
                    FrankOpeningRidgewayProductId)
                ?? 0;
            int displayedUnitCount =
                subscribedDisplayInventory?.GetDisplayedQuantity(
                    FrankOpeningRidgewayProductId)
                ?? 0;
            FrankRoadsideOpeningObjective objective =
                FrankRoadsideOpeningProgress.ResolveStockingObjective(
                    backstockUnitCount,
                    displayedUnitCount,
                    FrankOpeningRidgewayReceivedUnitCount,
                    FrankOpeningRidgewayDisplayUnitCount);

            merchandisingOverlayViewSystem
                ?.SetObjectiveHighlightEnabled(
                    objective
                    == FrankRoadsideOpeningObjective.StockSalesFloor);

            switch (objective)
            {
                case FrankRoadsideOpeningObjective
                    .MoveReceivingToStockroom:
                {
                    string receivingDescription =
                        FrankObjectiveDescription;

                    if (founderStockTaskController?.ActivePutAwayWork
                        != null)
                    {
                        int placedCaseCount = Mathf.Clamp(
                            founderStockTaskController
                                .ActivePutAwayWork.PlacedCaseCount,
                            0,
                            FrankOpeningRidgewayCaseCount);
                        receivingDescription =
                            founderStockTaskController.StatusMessage
                            + $" ({placedCaseCount}/"
                            + $"{FrankOpeningRidgewayCaseCount} cases stored)";
                    }

                    boundView.SetObjective(
                        FrankObjectiveTitle,
                        receivingDescription);
                    break;
                }

                case FrankRoadsideOpeningObjective.StockSalesFloor:
                    boundView.SetObjective(
                        FrankSalesFloorObjectiveTitle,
                        FrankSalesFloorObjectiveDescription
                        + $" ({displayedUnitCount}/"
                        + $"{FrankOpeningRidgewayDisplayUnitCount} stocked)");
                    break;

                case FrankRoadsideOpeningObjective.Complete:
                    boundView.SetObjective(
                        FrankCompletedObjectiveTitle,
                        FrankCompletedObjectiveDescription);
                    break;
            }
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

        private void AttachDisplayInventory()
        {
            FixtureDisplayInventoryService nextDisplayInventory =
                planogramRuntimeHost != null
                && planogramRuntimeHost.IsInitialized
                    ? planogramRuntimeHost.DisplayInventory
                    : null;

            if (subscribedDisplayInventory == nextDisplayInventory)
            {
                return;
            }

            DetachDisplayInventory();
            subscribedDisplayInventory = nextDisplayInventory;

            if (subscribedDisplayInventory != null)
            {
                subscribedDisplayInventory.FixtureStockChanged +=
                    HandleDisplayStockChanged;
            }
        }

        private void DetachDisplayInventory()
        {
            if (subscribedDisplayInventory == null)
            {
                return;
            }

            subscribedDisplayInventory.FixtureStockChanged -=
                HandleDisplayStockChanged;
            subscribedDisplayInventory = null;
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
