namespace BigRetail.Core.Session
{
    /// <summary>
    /// Identifies the editor-only Map Workshop session without creating a
    /// shipping game mode. Player builds always retain the default false value.
    /// </summary>
    public static class MapWorkshopSession
    {
        public static bool IsActive { get; private set; }


#if UNITY_EDITOR
        internal static void SetActive(
            bool isActive)
        {
            IsActive = isActive;
        }
#endif
    }
}
