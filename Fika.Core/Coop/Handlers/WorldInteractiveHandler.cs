using EFT.Interactive;
using System.Collections.Generic;

namespace Fika.Core.Coop.Handlers
{
    public class WorldInteractiveHandler
    {
        public Dictionary<int, WorldInteractiveObject> InteractiveObjects { get; } = [];

        public int RegisterWorldInteractive(WorldInteractiveObject worldInteractiveObject)
        {
            if (InteractiveObjects.TryGetKey(worldInteractiveObject, out var key))
            {
                InteractiveObjects.Remove(key);
            }

            int hash = worldInteractiveObject.Id.GetHashCode();
            InteractiveObjects[hash] = worldInteractiveObject;

            return hash;
        }
    }
}
