using System.Reflection;
using DataPuller.Multiplayer;
using HarmonyLib;

#nullable enable
namespace DataPuller.Harmony
{
    [HarmonyPatch(typeof(global::MultiplayerSessionManager), nameof(global::MultiplayerSessionManager.StartSession))]
    internal class MultiplayerSessionManagerStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ConnectedPlayerManager connectedPlayerManager)
            => MultiplayerSources.Vanilla.Activate(connectedPlayerManager);
    }

    [HarmonyPatch(typeof(global::MultiplayerSessionManager), nameof(global::MultiplayerSessionManager.EndSession))]
    internal class MultiplayerSessionManagerPatch
    {
        [HarmonyPostfix]
        public static void EndSession_PostFix()
            => MultiplayerSources.Vanilla.Deactivate();
    }

    // SetMaxPlayerCount is only called by the game for the host (CreateParty) and
    // CreateOrConnectToDestinationParty paths. Clients joining via lobby code
    // (ConnectToParty) never trigger it, so maxPlayerCount stays 0. Patching
    // HandleMultiplayerSessionManagerConnected covers all join paths — by the time it
    // fires the GameLift API response has already populated the real configuration.
    [HarmonyPatch(typeof(MultiplayerLobbyConnectionController))]
    [HarmonyPatch("HandleMultiplayerSessionManagerConnected")]
    internal class MultiplayerLobbyConnectionControllerConnectedPatch
    {
        private static readonly FieldInfo? _unifiedNetworkPlayerModelField =
            AccessTools.Field(typeof(MultiplayerLobbyConnectionController), "_unifiedNetworkPlayerModel");

        [HarmonyPostfix]
        public static void Postfix(MultiplayerLobbyConnectionController __instance)
        {
            if (_unifiedNetworkPlayerModelField?.GetValue(__instance) is INetworkPlayerModel model)
                MultiplayerSources.Vanilla.SetMaxPlayerCount(model.configuration.maxPlayerCount);
        }
    }
}
