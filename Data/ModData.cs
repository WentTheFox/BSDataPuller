using DataPuller.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataPuller.Data
{
    internal class ModData : AData
    {
        #region Singleton
        /// <summary>
        /// The singleton instance that DataPuller writes to.
        /// </summary>
        [JsonIgnore] public static readonly ModData Instance = new();
        #endregion

        #region Properties
        /// <summary>List of metadata for all enabled mods</summary>
        /// <remarks></remarks>
        /// <value>Default is an empty list.</value>
        [DefaultValueT<List<SPluginMetadata>>]
        public List<SPluginMetadata> EnabledPlugins { get; internal set; }
        #endregion
    }
}
