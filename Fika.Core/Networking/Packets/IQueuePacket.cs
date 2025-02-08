using Fika.Core.Coop.Players;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
    public interface IQueuePacket : INetSerializable
    {
        public int NetId { get; set; }

        public void Execute(CoopPlayer player);
    }
}
