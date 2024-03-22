using DataPuller.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;

#nullable enable
namespace DataPuller.Data
{
    internal class PartyData : AData
    {
        #region Singleton
        /// <summary>
        /// The singleton instance that DataPuller writes to.
        /// </summary>
        [JsonIgnore] public static readonly PartyData Instance = new();
        #endregion

        #region Properties
        /// <summary>ID of the leaderboard</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        public string? LeaderboardID { get; internal set; }

        /// <summary>Type of the leaderboard</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        public string? LeaderboardType { get; internal set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>List of scores for the specific leaderboard</summary>
        /// <remarks></remarks>
        /// <value>Default is an empty list.</value>
        [DefaultValueT<List<SLocalLeaderboardScore>>]
        public List<SLocalLeaderboardScore> Scores { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        #endregion
    }
}
