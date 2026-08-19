using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the shared UI document to the authoritative simulation clock.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(356)]
    public sealed class SimulationClockPresenter : MonoBehaviour
    {
        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private SimulationTimeRuntimeHost timeHost;

        private SimulationClockView boundView;
        private SimulationClock subscribedClock;
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

            referencesAreValid =
                ValidateReferences();
        }

        private void OnEnable()
        {
            if (!referencesAreValid)
            {
                return;
            }

            documentHost.SimulationClockViewReady +=
                HandleViewReady;
            timeHost.Initialized +=
                HandleTimeInitialized;

            SubscribeToClock();

            if (documentHost.HasSimulationClockView)
            {
                BindView(
                    documentHost.SimulationClockView);
            }
        }

        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.SimulationClockViewReady -=
                    HandleViewReady;
            }

            if (timeHost != null)
            {
                timeHost.Initialized -=
                    HandleTimeInitialized;
            }

            UnsubscribeFromClock();
            UnbindView();
        }


        private void HandleViewReady(
            SimulationClockView view)
        {
            BindView(view);
        }

        private void HandleTimeInitialized()
        {
            SubscribeToClock();
            RefreshView();
        }

        private void HandleTimeChanged(
            SimulationDateTime dateTime)
        {
            boundView?.SetDateTime(dateTime);
        }

        private void HandleSpeedChanged(
            SimulationSpeed speed)
        {
            boundView?.SetSpeed(speed);
        }

        private void HandleSpeedRequested(
            SimulationSpeed speed)
        {
            timeHost.SetSpeed(speed);
        }

        private void BindView(
            SimulationClockView view)
        {
            UnbindView();
            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.SpeedRequested +=
                HandleSpeedRequested;
            RefreshView();
        }

        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SpeedRequested -=
                HandleSpeedRequested;
            boundView = null;
        }

        private void SubscribeToClock()
        {
            SimulationClock clock =
                timeHost.IsInitialized
                    ? timeHost.Clock
                    : null;

            if (ReferenceEquals(clock, subscribedClock))
            {
                return;
            }

            UnsubscribeFromClock();
            subscribedClock = clock;

            if (subscribedClock != null)
            {
                subscribedClock.TimeChanged +=
                    HandleTimeChanged;
                subscribedClock.SpeedChanged +=
                    HandleSpeedChanged;
            }
        }

        private void UnsubscribeFromClock()
        {
            if (subscribedClock != null)
            {
                subscribedClock.TimeChanged -=
                    HandleTimeChanged;
                subscribedClock.SpeedChanged -=
                    HandleSpeedChanged;
            }

            subscribedClock = null;
        }

        private void RefreshView()
        {
            if (boundView == null
                || subscribedClock == null)
            {
                return;
            }

            boundView.SetDateTime(
                subscribedClock.CurrentTime);
            boundView.SetSpeed(
                subscribedClock.Speed);
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "SimulationClockPresenter has no document host assigned.",
                    this);
                isValid = false;
            }

            if (timeHost == null)
            {
                Debug.LogError(
                    "SimulationClockPresenter has no time host assigned.",
                    this);
                isValid = false;
            }

            return isValid;
        }
    }
}
