using EFT;
using System.Collections.Generic;

namespace Fika.Core.Coop.GameMode
{
    public interface IFikaGame
    {
        public List<string> ExtractedPlayers { get; }

        ExitStatus MyExitStatus { get; set; }

        string MyExitLocation { get; set; }

        public void Stop(string profileId, ExitStatus exitStatus, string exitName, float delay = 0f);
    }
}
