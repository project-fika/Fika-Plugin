using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ScopeStatesPacket : IFirearmPacket
{
    public ScopeStatesPacket(int amount, ScopeState[] states)
    {
        Amount = amount;
        States = states;
    }

    public ScopeStatesPacket(string zoomId, int zoomScopeIndex, float zoomValue)
    {
        IsZoom = true;
        ZoomId = zoomId;
        ZoomScopeIndex = zoomScopeIndex;
        ZoomValue = zoomValue;
    }

    public ScopeStatesPacket(NetDataReader reader)
    {
        IsZoom = reader.GetBool();
        if (IsZoom)
        {
            ZoomId = reader.GetString();
            ZoomScopeIndex = reader.GetInt();
            ZoomValue = reader.GetFloat();
            return;
        }

        Amount = reader.GetInt();
        if (Amount > 0)
        {
            States = new ScopeState[Amount];
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

    public readonly int Amount;
    public readonly ScopeState[] States;
    public readonly bool IsZoom;
    public readonly string ZoomId;
    public readonly int ZoomScopeIndex;
    public readonly float ZoomValue;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            if (IsZoom)
            {
                controller.SetScopeZoom(ZoomId, ZoomScopeIndex, ZoomValue);
                return;
            }

            controller.SetScopeMode(States);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(IsZoom);
        if (IsZoom)
        {
            writer.Put(ZoomId);
            writer.Put(ZoomScopeIndex);
            writer.Put(ZoomValue);
            return;
        }

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

    public EFirearmPacketType Type => EFirearmPacketType.ToggleScopeStates;
}
