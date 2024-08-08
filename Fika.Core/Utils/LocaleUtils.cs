using System.Collections.Generic;

namespace Fika.Core.Utils
{
	public static class LocaleUtils
	{
		private static readonly List<char> vowels = ['A', 'E', 'I', 'O', 'U'];

		public static string GetPrefix(string word)
		{
			char firstLetter = char.ToUpper(word[0]);

			if (vowels.Contains(firstLetter))
			{
				return "an";
			}

			return "a";
		}
	}
}
