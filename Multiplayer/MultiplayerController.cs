using DataPuller.Data;
using IPA.Loader;

#nullable enable
namespace DataPuller.Multiplayer
{
    internal static class MultiplayerSources
    {
        internal static readonly VanillaMultiplayerSource Vanilla = new();
        internal static readonly BspMultiplayerSource Bsp = new();

        internal static void Init()
        {
            if (PluginManager.GetPluginFromId(MultiplayerLobbySourceType.BeatSaberPlus_Multiplayer) != null)
                Bsp.Activate();
        }

        internal static void Dispose()
        {
            Vanilla.Deactivate();
            Bsp.Deactivate();
        }

        // Called from MapEvents when entering a multiplayer level — restores any fields
        // wiped by MapData.Reset() during the level load transition.
        internal static void RestoreSessionFields()
        {
            Vanilla.RestoreSessionFields();
            Bsp.RestoreSessionFields();
        }
    }
}
