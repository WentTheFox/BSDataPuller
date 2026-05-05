using System.Linq;
using System.Reflection;
using DataPuller.Multiplayer;
using HarmonyLib;

#nullable enable
namespace DataPuller.Harmony
{
    [HarmonyPatch]
    internal class MultiplayerSessionManagerStartPatch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod() =>
            AccessTools.GetDeclaredMethods(typeof(BeatSaberMultiplayerSessionManager))
                .First(m => m.Name.EndsWith(".StartSession"));

        [HarmonyPostfix]
        public static void Postfix(BeatSaberConnectedPlayerManager connectedPlayerManager)
            => MultiplayerSources.Vanilla.Activate(connectedPlayerManager);
    }

    [HarmonyPatch(typeof(BeatSaberMultiplayerSessionManager), nameof(BeatSaberMultiplayerSessionManager.SetMaxPlayerCount))]
    internal class MultiplayerSessionManagerPatch
    {
        [HarmonyPostfix]
        public static void SetMaxPlayerCount_PostFix(int maxPlayerCount)
            => MultiplayerSources.Vanilla.SetMaxPlayerCount(maxPlayerCount);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeatSaberMultiplayerSessionManager), nameof(BeatSaberMultiplayerSessionManager.EndSession))]
        public static void EndSession_PostFix()
            => MultiplayerSources.Vanilla.Deactivate();
    }
}
