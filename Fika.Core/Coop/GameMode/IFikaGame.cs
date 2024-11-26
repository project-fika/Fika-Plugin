using EFT;
using System.Collections.Generic;

namespace Fika.Core.Coop.GameMode
{
	/// <summary>
	/// Interface for the <see cref="CoopGame"/>
	/// </summary>
	public interface IFikaGame
	{
		public List<int> ExtractedPlayers { get; }

		ExitStatus ExitStatus { get; set; }

		string ExitLocation { get; set; }

		public void Stop(string profileId, ExitStatus exitStatus, string exitName, float delay = 0f);

		public ESeason Season { get; set; }

		public SeasonsSettingsClass SeasonsSettings { get; set; }
	}
}
