namespace BigRetail.Core.Session
{
    /// <summary>
    /// Represents one current Big Retail playthrough.
    /// </summary>
    public sealed class GameSession
    {
        public GameMode Mode { get; }

        public GameSession(GameMode mode)
        {
            Mode = mode;
        }
    }
}