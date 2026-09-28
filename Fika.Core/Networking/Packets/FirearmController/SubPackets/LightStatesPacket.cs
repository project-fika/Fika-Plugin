using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct LightStatesPacket : IFirearmPacket
{
    public LightStatesPacket(int amount, LightsState[] states)
    {
        Amount = amount;
        States = states;
    }

    public LightStatesPacket(NetDataReader reader)
    {
        Amount = reader.GetInt();
        if (Amount > 0)
        {
            States = new LightsState[Amount];
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

    public readonly int Amount;
    public readonly LightsState[] States;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetLightsState(States, true);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
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

    public EFirearmPacketType Type => EFirearmPacketType.ToggleLightStates;
}
