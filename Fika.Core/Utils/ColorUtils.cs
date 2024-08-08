using System.Collections.Generic;

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

	public class ColorUtils
	{
		private static readonly Dictionary<Colors, string> keyValuePairs = new()
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
			return keyValuePairs.TryGetValue(color, out string value) ? value : "ffffff";
		}

		public static string ColorizeText(Colors color, string text)
		{
			return $"<color=#{GetHexByColor(color)}>{text}</color>";
		}
	}
}
