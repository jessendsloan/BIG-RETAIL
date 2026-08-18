using System;
using BigRetail.Simulation.Time.Domain;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Presentation-only wrapper for the simulation calendar and speed HUD.
    /// </summary>
    public sealed class SimulationClockView : IDisposable
    {
        public const string DayLabelName = "simulation-clock-day";
        public const string TimeLabelName = "simulation-clock-time";
        public const string PauseButtonName = "simulation-speed-pause";
        public const string OneTimesButtonName = "simulation-speed-one";
        public const string TwoTimesButtonName = "simulation-speed-two";
        public const string FourTimesButtonName = "simulation-speed-four";

        private const string SelectedClassName = "is-selected";

        private readonly Label dayLabel;
        private readonly Label timeLabel;
        private readonly Button pauseButton;
        private readonly Button oneTimesButton;
        private readonly Button twoTimesButton;
        private readonly Button fourTimesButton;
        private bool isDisposed;


        public SimulationClockView(
            VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            dayLabel = Require<Label>(root, DayLabelName);
            timeLabel = Require<Label>(root, TimeLabelName);
            pauseButton = Require<Button>(root, PauseButtonName);
            oneTimesButton = Require<Button>(root, OneTimesButtonName);
            twoTimesButton = Require<Button>(root, TwoTimesButtonName);
            fourTimesButton = Require<Button>(root, FourTimesButtonName);

            pauseButton.clicked += HandlePauseRequested;
            oneTimesButton.clicked += HandleOneTimesRequested;
            twoTimesButton.clicked += HandleTwoTimesRequested;
            fourTimesButton.clicked += HandleFourTimesRequested;
        }


        public event Action<SimulationSpeed> SpeedRequested;


        public void SetDateTime(
            SimulationDateTime dateTime)
        {
            dayLabel.text =
                $"{dateTime.DayOfWeek.ToString().ToUpperInvariant()}  •  DAY {dateTime.DayNumber}";

            int displayHour = dateTime.Hour % 12;
            if (displayHour == 0)
            {
                displayHour = 12;
            }

            string meridiem =
                dateTime.Hour < 12
                    ? "AM"
                    : "PM";

            timeLabel.text =
                $"{displayHour}:{dateTime.Minute:00} {meridiem}";
        }

        public void SetSpeed(
            SimulationSpeed speed)
        {
            pauseButton.EnableInClassList(
                SelectedClassName,
                speed == SimulationSpeed.Paused);
            oneTimesButton.EnableInClassList(
                SelectedClassName,
                speed == SimulationSpeed.OneTimes);
            twoTimesButton.EnableInClassList(
                SelectedClassName,
                speed == SimulationSpeed.TwoTimes);
            fourTimesButton.EnableInClassList(
                SelectedClassName,
                speed == SimulationSpeed.FourTimes);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            pauseButton.clicked -= HandlePauseRequested;
            oneTimesButton.clicked -= HandleOneTimesRequested;
            twoTimesButton.clicked -= HandleTwoTimesRequested;
            fourTimesButton.clicked -= HandleFourTimesRequested;
            isDisposed = true;
        }


        private void HandlePauseRequested()
        {
            SpeedRequested?.Invoke(
                SimulationSpeed.Paused);
        }

        private void HandleOneTimesRequested()
        {
            SpeedRequested?.Invoke(
                SimulationSpeed.OneTimes);
        }

        private void HandleTwoTimesRequested()
        {
            SpeedRequested?.Invoke(
                SimulationSpeed.TwoTimes);
        }

        private void HandleFourTimesRequested()
        {
            SpeedRequested?.Invoke(
                SimulationSpeed.FourTimes);
        }

        private static T Require<T>(
            VisualElement root,
            string elementName)
            where T : VisualElement
        {
            T element = root.Q<T>(elementName);
            if (element != null)
            {
                return element;
            }

            throw new InvalidOperationException(
                $"Simulation clock is missing required element '{elementName}'.");
        }
    }
}
