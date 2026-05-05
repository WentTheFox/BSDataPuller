using DataPuller.Data;
using DataPuller.Multiplayer;
using HarmonyLib;

#nullable enable
namespace DataPuller.Harmony
{
    /// <summary>
    /// Captures the lobby join code for vanilla and BeatTogether lobbies.
    /// BeatSaberPlus uses its own networking; see <see cref="BspMultiplayerSource"/> for that path.
    /// </summary>
    [HarmonyPatch(typeof(MultiplayerSettingsPanelController), nameof(MultiplayerSettingsPanelController.SetLobbyCode))]
    internal class MultiplayerLobbyCodePatch
    {
        [HarmonyPostfix]
        public static void SetLobbyCode_Postfix(string code)
        {
            if (MapData.Instance.MultiplayerLobbySource == MultiplayerLobbySourceType.BeatSaberPlus_Multiplayer) return;
            MultiplayerSources.Vanilla.SetLobbyCode(code);
        }
    }
}
