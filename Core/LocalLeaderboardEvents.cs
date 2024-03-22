using System;
using System.Collections.Generic;
using DataPuller.Data;
using Zenject;

#nullable enable
namespace DataPuller.Core
{
    internal class LocalLeaderboardEvents : IInitializable, IDisposable
    {
        [InjectOptional] private LocalLeaderboardViewController? localLeaderboardViewController;

        public void Initialize()
        {
            PartyData.Instance.Reset();

            if (!DoRequiredObjectsExist(out List<string> missingObjects))
            {
                EarlyDispose($"{nameof(LocalLeaderboardEvents)} required objects not found. Missing: {string.Join(", ", missingObjects)}");
                return;
            }

            if (localLeaderboardViewController is not null)
            {
                localLeaderboardViewController.leaderboardsModel.newScoreWasAddedToLeaderboardEvent += UpdatePartyData;
            }
        }

        /// <param name="missingObjects">Empty when returning true</param>
        /// <returns>True if the object was found, otherwise false.</returns>
        private bool DoRequiredObjectsExist(out List<string> missingObjects)
        {
            missingObjects = new();

            if (localLeaderboardViewController is null) missingObjects.Add($"{nameof(LocalLeaderboardViewController)} not found");

            return missingObjects.Count == 0;
        }

        //This should be logged as an error as there is currently no reason as to why the script should stop early, unless required objects are not found.
        private void EarlyDispose(string reason)
        {
            Plugin.Logger.Error($"{nameof(LocalLeaderboardEvents)} quit early. Reason: {reason}");
            Dispose();
        }

        public void Dispose()
        {
            #region Unsubscribe from events
            if (localLeaderboardViewController is not null)
            {
                localLeaderboardViewController.leaderboardsModel.newScoreWasAddedToLeaderboardEvent -= UpdatePartyData;
            }
            #endregion

            PartyData.Instance.Reset();
            PartyData.Instance.Send();
        }

        internal static void UpdatePartyData(LocalLeaderboardViewController viewController, string leaderboardId, LocalLeaderboardsModel.LeaderboardType type)
        {
            if (viewController is null)
            {
                Plugin.Logger.Error("viewController missing in UpdatePartyData");
                return;
            }

            var model = viewController.leaderboardsModel;
            if (model is null)
            {
                Plugin.Logger.Error("leaderboardsModel missing in UpdatePartyData");
                return;
            }

            var scores = model.GetScores(leaderboardId, type);
            // Can be null if no data is saved for a leaderboard yet
            if (scores is null) {
                scores = new();
            }
            PartyData.Instance.LeaderboardID = leaderboardId;
            PartyData.Instance.LeaderboardType = type.ToString();
            PartyData.Instance.Scores = scores.ConvertAll(score => new SLocalLeaderboardScore
            {
                PlayerName = score._playerName,
                Score = score._score,
                Timestamp = score._timestamp,
                FullCombo = score._fullCombo,
            });
            PartyData.Instance.Send();
        }

        internal void UpdatePartyData(string leaderboardId, LocalLeaderboardsModel.LeaderboardType type)
        {
            if (localLeaderboardViewController is null) return;

            UpdatePartyData(localLeaderboardViewController, leaderboardId, type);
        }
    }
}
