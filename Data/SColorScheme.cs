using DataPuller.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPuller.Data
{
    public struct SColorScheme
    {
        /// <summary>The color of the primary (typically left) saber, and by extension the notes.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? SaberAColor { get; internal set; }

        /// <summary>The color of the secondary (typically right) saber, and by extension the notes.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? SaberBColor { get; internal set; }

        /// <summary>The color of the walls.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? ObstaclesColor { get; internal set; }

        /// <summary>The primary enviornment color.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? EnvironmentColor0 { get; internal set; }

        /// <summary>The secondary enviornment color.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? EnvironmentColor1 { get; internal set; }

        /// <summary>The primary enviornment boost color, typically se to the same as the primary environment color.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? EnvironmentColor0Boost { get; internal set; }

        /// <summary>The secondary enviornment boost color, typically se to the same as the secondary environment color.</summary>
        /// <remarks></remarks>
        /// <value>Default is <see href="null"/>.</value>
        [DefaultValue(null)]
        public SRGBAColor? EnvironmentColor1Boost { get; internal set; }
    }
}
