using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPuller.Data
{
    public struct SPluginMetadata
    {
        /// <summary>Mod name</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string Name { get; internal set; }
        /// <summary>Mod version</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string Version { get; internal set; }
        /// <summary>Mod author</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string Author { get; internal set; }
        /// <summary>Mod description</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string Description { get; internal set; }
        /// <summary>Mod homepage URL</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string HomeLink { get; internal set; }
        /// <summary>Mod source code URL</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string SourceLink { get; internal set; }
        /// <summary>Mod donation URL</summary>
        /// <remarks></remarks>
        /// <value>Default is <see cref="string.Empty"/>.</value>
        public string DonateLink { get; internal set; }
    }
}
