using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPuller.Data
{
    public struct SLocalLeaderboardScore
    {
        /// <summary>Player name</summary>
        /// <remarks></remarks>
        public string PlayerName { get; internal set; }
        /// <summary>Player's score</summary>
        /// <remarks></remarks>
        public int Score { get; internal set; }
        /// <summary>UNIX timestamp (in seconds) when the score was recorded</summary>
        /// <remarks></remarks>
        public long Timestamp { get; internal set; }
        /// <summary>Whether the play-through had a full combo (no mistakes)</summary>
        /// <remarks></remarks>
        public bool FullCombo { get; internal set; }
    }
}
