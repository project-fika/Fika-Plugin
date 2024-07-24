using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace Fika.Core.Utils
{
    public enum Colors
    {
        WHITE,
        BLACK,
        GREEN,
        BROWN,
        BLUE,
        RED
    }

    class ColorUtils
    {
        private static readonly Dictionary<Colors, string> keyValuePairs = new Dictionary<Colors, string>
        {
            { Colors.WHITE, "ffffff" },
            { Colors.BLACK, "000000" },
            { Colors.GREEN, "32a852" },
            { Colors.BROWN, "a87332" },
            { Colors.BLUE, "51c6db" },
            { Colors.RED, "a83232" }
        };

        private static string GetHexByColor(Colors color)
        {
            return keyValuePairs.TryGetValue(color, out var value) ? value : "ffffff";
        }

        public static string ColorizeText(Colors color, string text)
        {
            return $"<color=#{GetHexByColor(color)}>{text}</color>";
        }
    }

}
