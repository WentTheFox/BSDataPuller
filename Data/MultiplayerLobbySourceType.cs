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
        /// Any custom multiplayer server backed by the MultiplayerCore mod.
        /// BSIPA plugin ID: <c>MultiplayerCore</c>.
        /// The specific server or mod can be identified via <see cref="MapData.MultiplayerCoreLobbyMod"/>.
        /// </summary>
        public const string MultiplayerCore = "MultiplayerCore";

        /// <summary>
        /// BeatSaberPlus Multiplayer+ mod — fully custom multiplayer networking.
        /// BSIPA plugin ID: <c>BeatSaberPlus_Multiplayer</c>.
        /// </summary>
        public const string BeatSaberPlus_Multiplayer = "BeatSaberPlus_Multiplayer";
    }
}
