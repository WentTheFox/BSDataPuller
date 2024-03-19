using DataPuller.Core;
using DataPuller.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace DataPuller.Harmony
{
    [HarmonyPatch(typeof(LocalLeaderboardViewController), "SetContent")]
    internal class LocalLeaderboardViewControllerPatch
    {
        [HarmonyPostfix]
        public static void SetContent_PostFix(ref string leaderboardID, ref LocalLeaderboardsModel.LeaderboardType leaderboardType, LocalLeaderboardViewController __instance)
        {
            // Update data whenever the display on the UI changes
            LocalLeaderboardEvents.UpdatePartyData(__instance, leaderboardID, leaderboardType);
        }
    }
}
