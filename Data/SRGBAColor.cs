using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweening;
using UnityEngine;

namespace DataPuller.Data
{
    public struct SRGBAColor
    {
        /// <summary>Hexadeciaml RGB color code including the # symbol</summary>
        public string HexCode { get { return $"#{Red:X2}{Green:X2}{Blue:X2}"; } }

        /// <summary><see href="0"/> to <see href="255"/></summary>
        public int Red { get; internal set; } = 0;

        /// <summary><see href="0"/> to <see href="255"/></summary>
        public int Green { get; internal set; } = 0;

        /// <summary><see href="0"/> to <see href="255"/></summary>
        public int Blue { get; internal set; } = 0;

        /// <summary><see href="0.0"/> to <see href="1.0"/></summary>
        public float Alpha { get; internal set; } = 0;

        public SRGBAColor(Color color) {
            Red = FloatToRgb(color.r);
            Green = FloatToRgb(color.g);
            Blue = FloatToRgb(color.b);
            Alpha = color.a;
        }

        internal static int FloatToRgb(float value)
        {
            return Convert.ToInt32(value * 255);
        }
    }
}
