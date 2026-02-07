using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ScopeStatesPacket : IPoolSubPacket
{
    private ScopeStatesPacket()
    {

    }
    public static ScopeStatesPacket FromValue(int amount, FirearmScopeStateStruct[] states)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ScopeStatesPacket>(EFirearmSubPacketType.ToggleScopeStates);
        packet.Amount = amount;
        packet.States = states;
        return packet;
    }

    public static ScopeStatesPacket CreateInstance()
    {
        return new();
    }

    public int Amount;
    public FirearmScopeStateStruct[] States;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetScopeMode(States);
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
                writer.Put(States[i].ScopeMode);
                writer.Put(States[i].ScopeIndexInsideSight);
                writer.Put(States[i].ScopeCalibrationIndex);
            }
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Amount = reader.GetInt();
        if (Amount > 0)
        {
            States = new FirearmScopeStateStruct[Amount];
            for (var i = 0; i < Amount; i++)
            {
                States[i] = new()
                {
                    Id = reader.GetString(),
                    ScopeMode = reader.GetInt(),
                    ScopeIndexInsideSight = reader.GetInt(),
                    ScopeCalibrationIndex = reader.GetInt()
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
