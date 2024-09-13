using EFT;
using EFT.Interactive;

namespace Fika.Core.Coop.GameMode
{
	/// <summary>
	/// <see cref="World"/> used for the client to synchronize game logic
	/// </summary>
	public class FikaWorld : World
	{
		/// <summary>
		/// Sets up all the <see cref="BorderZone"/>s on the map
		/// </summary>
		public override void SubscribeToBorderZones(BorderZone[] zones)
		{
			foreach (BorderZone borderZone in zones)
			{
				borderZone.RemoveAuthority();
			}
		}
	}
}
