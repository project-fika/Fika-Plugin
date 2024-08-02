using EFT;
using EFT.Interactive;

namespace Fika.Core.Coop.GameMode
{
    /// <summary>
    /// Currently used to keep track of interactable objects, in the future this will be used to sync reconnects
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
