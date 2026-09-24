using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct HeadLightsPacket
{
    public HeadLightsPacket(int amount, bool isSilent, LightsState[] lightStates)
    {
        Amount = amount;
        IsSilent = isSilent;
        LightStates = lightStates;
    }

    public HeadLightsPacket(NetDataReader reader)
    {
        Amount = reader.GetInt();
        IsSilent = reader.GetBool();
        if (Amount > 0)
        {
            LightStates = new LightsState[Amount];
            for (var i = 0; i < Amount; i++)
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

    public readonly int Amount;
    public readonly bool IsSilent;
    public readonly LightsState[] LightStates;

    public readonly void Execute(FikaPlayer player)
    {
        player.HandleHeadLightsPacket(this);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Amount);
        writer.Put(IsSilent);
        if (Amount > 0)
        {
            for (var i = 0; i < Amount; i++)
            {
                writer.Put(LightStates[i].Id);
                writer.Put(LightStates[i].IsActive);
                writer.Put(LightStates[i].LightMode);
            }
        }
    }
}
