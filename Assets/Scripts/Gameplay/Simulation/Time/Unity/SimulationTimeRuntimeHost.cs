using System;
using BigRetail.Simulation.Time.Domain;
using UnityEngine;

namespace BigRetail.Simulation.Time.Unity
{
    /// <summary>
    /// Advances the pure simulation clock from Unity's unscaled frame time.
    /// The host is intentionally unaware of store hours and economic rules.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class SimulationTimeRuntimeHost : MonoBehaviour
    {
        [Header("Starting Calendar")]

        [SerializeField]
        [Min(1)]
        private int startingDay = 1;

        [SerializeField]
        [Range(0, 23)]
        private int startingHour = 8;

        [SerializeField]
        [Range(0, 59)]
        private int startingMinute;


        [Header("Clock Rate")]

        [SerializeField]
        [Min(0.01f)]
        private float gameMinutesPerRealSecond = 1f;

        [SerializeField]
        private SimulationSpeed initialSpeed =
            SimulationSpeed.OneTimes;


        public SimulationClock Clock
        {
            get;
            private set;
        }

        public bool IsInitialized =>
            Clock != null;


        public event Action Initialized;


        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (Clock == null)
            {
                return;
            }

            Clock.Advance(
                UnityEngine.Time.unscaledDeltaTime);
        }


        public void Initialize()
        {
            if (Clock != null)
            {
                return;
            }

            int safeStartingDay =
                Mathf.Max(1, startingDay);
            int safeStartingHour =
                Mathf.Clamp(startingHour, 0, 23);
            int safeStartingMinute =
                Mathf.Clamp(startingMinute, 0, 59);
            float safeGameMinutesPerRealSecond =
                Mathf.Max(0.01f, gameMinutesPerRealSecond);

            Clock =
                new SimulationClock(
                    safeStartingDay,
                    safeStartingHour,
                    safeStartingMinute,
                    initialSpeed,
                    safeGameMinutesPerRealSecond
                    * SimulationDateTime.SecondsPerMinute);

            Initialized?.Invoke();
        }

        public void SetSpeed(
            SimulationSpeed speed)
        {
            RequireClock().SetSpeed(speed);
        }

        public SimulationClockState CaptureState()
        {
            return RequireClock().CaptureState();
        }

        public void RestoreState(
            SimulationClockState state)
        {
            RequireClock().RestoreState(state);
        }


        private SimulationClock RequireClock()
        {
            if (Clock != null)
            {
                return Clock;
            }

            throw new InvalidOperationException(
                "Simulation time has not been initialized.");
        }
    }
}
