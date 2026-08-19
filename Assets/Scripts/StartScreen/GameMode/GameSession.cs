namespace BigRetail.Core.Session
{
    /// <summary>
    /// Represents one current Big Retail playthrough.
    /// </summary>
    public sealed class GameSession
    {
        public GameMode Mode { get; }

        public CampaignOpeningProgress CampaignOpening { get; }

        public bool IsCampaign => Mode == GameMode.Campaign;

        public bool IsSandbox => Mode == GameMode.Sandbox;

        public GameSession(GameMode mode)
        {
            Mode = mode;
            CampaignOpening = new CampaignOpeningProgress();
        }
    }
}
