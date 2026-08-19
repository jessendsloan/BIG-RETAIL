using System;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Presentation-only wrapper for the opening campaign transmission and
    /// the objective card it leaves behind.
    /// </summary>
    public sealed class CampaignOpeningView : IDisposable
    {
        public const string OverlayName = "campaign-opening-overlay";
        public const string ObjectiveCardName = "campaign-objective-card";

        private readonly VisualElement overlay;
        private readonly VisualElement objectiveCard;
        private readonly Label speakerLabel;
        private readonly Label dialogueLabel;
        private readonly Label progressLabel;
        private readonly Label objectiveTitleLabel;
        private readonly Label objectiveDescriptionLabel;
        private readonly Button continueButton;
        private readonly Button skipButton;
        private bool isDisposed;


        public CampaignOpeningView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            overlay = Require<VisualElement>(root, OverlayName);
            objectiveCard = Require<VisualElement>(root, ObjectiveCardName);
            speakerLabel = Require<Label>(root, "campaign-opening-speaker");
            dialogueLabel = Require<Label>(root, "campaign-opening-dialogue");
            progressLabel = Require<Label>(root, "campaign-opening-progress");
            objectiveTitleLabel = Require<Label>(root, "campaign-objective-title");
            objectiveDescriptionLabel = Require<Label>(
                root,
                "campaign-objective-description");
            continueButton = Require<Button>(root, "campaign-opening-continue");
            skipButton = Require<Button>(root, "campaign-opening-skip");

            continueButton.clicked += HandleContinueRequested;
            skipButton.clicked += HandleSkipRequested;

            SetDialogueVisible(false);
            SetObjectiveVisible(false);
        }


        public event Action ContinueRequested;

        public event Action SkipRequested;


        public void SetDialogueVisible(bool isVisible)
        {
            overlay.style.display =
                isVisible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        public void SetDialogue(
            string speaker,
            string dialogue,
            int pageNumber,
            int pageCount,
            bool isFinalPage)
        {
            speakerLabel.text = speaker ?? string.Empty;
            dialogueLabel.text = dialogue ?? string.Empty;
            progressLabel.text = $"{pageNumber} / {pageCount}";
            continueButton.text =
                isFinalPage
                    ? "Start Building"
                    : "Continue";
        }

        public void SetObjectiveVisible(bool isVisible)
        {
            objectiveCard.style.display =
                isVisible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        public void SetObjective(
            string title,
            string description)
        {
            objectiveTitleLabel.text = title ?? string.Empty;
            objectiveDescriptionLabel.text = description ?? string.Empty;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            continueButton.clicked -= HandleContinueRequested;
            skipButton.clicked -= HandleSkipRequested;
            isDisposed = true;
        }


        private void HandleContinueRequested()
        {
            ContinueRequested?.Invoke();
        }

        private void HandleSkipRequested()
        {
            SkipRequested?.Invoke();
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
                $"Campaign opening UI is missing required element '{elementName}'.");
        }
    }
}
