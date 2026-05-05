using DataPuller.Multiplayer;
using HarmonyLib;

#nullable enable
namespace DataPuller.Harmony
{
    [HarmonyPatch(typeof(global::MultiplayerSessionManager), nameof(global::MultiplayerSessionManager.StartSession))]
    internal class MultiplayerSessionManagerPatch
    {
        [HarmonyPostfix]
        public static void StartSession_PostFix(ref ConnectedPlayerManager connectedPlayerManager)
            => MultiplayerSources.Vanilla.Activate(connectedPlayerManager);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::MultiplayerSessionManager), nameof(global::MultiplayerSessionManager.SetMaxPlayerCount))]
        public static void SetMaxPlayerCount_PostFix(ref int maxPlayerCount)
            => MultiplayerSources.Vanilla.SetMaxPlayerCount(maxPlayerCount);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::MultiplayerSessionManager), nameof(global::MultiplayerSessionManager.EndSession))]
        public static void EndSession_PostFix()
            => MultiplayerSources.Vanilla.Deactivate();
    }
}
