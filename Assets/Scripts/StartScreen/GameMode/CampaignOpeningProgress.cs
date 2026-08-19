namespace BigRetail.Core.Session
{
    /// <summary>
    /// The small, session-owned state machine for the campaign's opening
    /// conversation. Presentation and simulation pausing remain Unity-side.
    /// </summary>
    public sealed class CampaignOpeningProgress
    {
        public CampaignOpeningBeat CurrentBeat { get; private set; } =
            CampaignOpeningBeat.Opportunity;

        public bool IsComplete =>
            CurrentBeat == CampaignOpeningBeat.Complete;


        public void Advance()
        {
            if (IsComplete)
            {
                return;
            }

            CurrentBeat++;
        }

        public void Skip()
        {
            CurrentBeat = CampaignOpeningBeat.Complete;
        }
    }

    public enum CampaignOpeningBeat
    {
        Opportunity = 0,
        Financing = 1,
        FirstAssignment = 2,
        Complete = 3
    }
}
