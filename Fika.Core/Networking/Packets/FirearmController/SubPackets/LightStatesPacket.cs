using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class LightStatesPacket : IPoolSubPacket
{
    private LightStatesPacket()
    {

    }

    public static LightStatesPacket FromValue(int amount, FirearmLightStateStruct[] states)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<LightStatesPacket>(EFirearmSubPacketType.ToggleLightStates);
        packet.Amount = amount;
        packet.States = states;
        return packet;
    }

    public static LightStatesPacket CreateInstance()
    {
        return new();
    }

    public int Amount;
    public FirearmLightStateStruct[] States;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetLightsState(States, true);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Amount);
        if (Amount > 0)
        {
            for (var i = 0; i < Amount; i++)
            {
                writer.Put(States[i].Id);
                writer.Put(States[i].IsActive);
                writer.Put(States[i].LightMode);
            }
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Amount = reader.GetInt();
        if (Amount > 0)
        {
            States = new FirearmLightStateStruct[Amount];
            for (var i = 0; i < Amount; i++)
            {
                States[i] = new()
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
        States = null;
    }
}
