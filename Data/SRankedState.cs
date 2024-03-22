using Newtonsoft.Json;
using System.ComponentModel;

namespace DataPuller.Data
{
    public struct SRankedState
    {
        /// <summary>Is map ranked on any leaderboards</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="false"/>.</value>
        [JsonProperty]
        public bool Ranked => BeatleaderRanked || ScoresaberRanked;

        /// <summary>Is map qualified on any leaderboards</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="false"/>.</value>
        [JsonProperty]
        public bool Qualified => BeatleaderQualified || ScoresaberQualified;

        /// <summary>Is map qualified on BeatLeader</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="false"/>.</value>
        [DefaultValue(false)]
        public bool BeatleaderQualified { get; internal set; }

        /// <summary>Is map qualified on ScoreSaber</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="false"/>.</value>
        [DefaultValue(false)]
        public bool ScoresaberQualified { get; internal set; }

        /// <summary>Is map ranked on BeatLeader</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="false"/>.</value>
        [DefaultValue(false)]
        public bool BeatleaderRanked { get; internal set; }

        /// <summary>Is map ranked on ScoreSaber</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="false"/>.</value>
        [DefaultValue(false)]
        public bool ScoresaberRanked { get; internal set; }

        /// <summary>BeatLeader stars</summary>
        /// <remarks><see href="0"/> if the value was undetermined.</remarks>
        /// <value>Default is <see href="0"/>.</value>
        [DefaultValue(0)]
        public double BeatleaderStars { get; internal set; }

        /// <summary>ScoreSaber stars</summary>
        /// <remarks><see href="0"/> if the value was undetermined.</remarks>
        /// <value>Default is <see href="0"/>.</value>
        [DefaultValue(0)]
        public double ScoresaberStars { get; internal set; }
    }
}
