#nullable enable
namespace DataPuller.Data
{
    /// <summary>
    /// String constants for the possible values of <see cref="MapData.MultiplayerLobbySource"/>.
    /// </summary>
    public static class MultiplayerLobbySourceType
    {
        /// <summary>Standard Beat Saber online multiplayer with no additional multiplayer mod installed.</summary>
        public const string Vanilla = "Vanilla";

        /// <summary>
        /// BeatTogether mod — extends vanilla multiplayer with cross-platform and custom-song support.
        /// BSIPA plugin ID: <c>BeatTogether</c>.
        /// </summary>
        public const string BeatTogether = "BeatTogether";

        /// <summary>
        /// BeatSaberPlus Multiplayer+ mod — fully custom multiplayer networking.
        /// BSIPA plugin ID: <c>BeatSaberPlus_Multiplayer</c>.
        /// </summary>
        public const string BeatSaberPlus_Multiplayer = "BeatSaberPlus_Multiplayer";
    }
}
