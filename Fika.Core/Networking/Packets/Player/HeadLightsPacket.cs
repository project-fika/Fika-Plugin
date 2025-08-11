using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player
{
    public class HeadLightsPacket : IPoolSubPacket
    {
        private HeadLightsPacket()
        {

        }

        public static HeadLightsPacket CreateInstance()
        {
            return new();
        }

        public static HeadLightsPacket FromValue(int amount, bool isSilent, FirearmLightStateStruct[] lightStates)
        {
            HeadLightsPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<HeadLightsPacket>(ECommonSubPacketType.HeadLights);
            packet.Amount = amount;
            packet.IsSilent = isSilent;
            packet.LightStates = lightStates;
            return packet;
        }

        public int Amount;
        public bool IsSilent;
        public FirearmLightStateStruct[] LightStates;

        public void Execute(FikaPlayer player)
        {
            player.HandleHeadLightsPacket(this);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Amount);
            writer.Put(IsSilent);
            if (Amount > 0)
            {
                for (int i = 0; i < Amount; i++)
                {
                    writer.Put(LightStates[i].Id);
                    writer.Put(LightStates[i].IsActive);
                    writer.Put(LightStates[i].LightMode);
                }
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Amount = reader.GetInt();
            IsSilent = reader.GetBool();
            if (Amount > 0)
            {
                LightStates = new FirearmLightStateStruct[Amount];
                for (int i = 0; i < Amount; i++)
                {
                    LightStates[i] = new()
                    {
                        Id = reader.GetString(),
                        IsActive = reader.GetBool(),
                        LightMode = reader.GetInt()
                    };
                }
            }
        }

        public void Dispose()
        {
            Amount = 0;
            IsSilent = false;
            LightStates = null;
        }
    }
}
