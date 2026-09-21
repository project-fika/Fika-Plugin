using EFT;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ScopeStatesPacket : IPoolSubPacket
{
    private ScopeStatesPacket()
    {

    }
    public static ScopeStatesPacket FromValue(int amount, ScopeState[] states)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ScopeStatesPacket>(EFirearmSubPacketType.ToggleScopeStates);
        packet.Amount = amount;
        packet.States = states;
        return packet;
    }

    public static ScopeStatesPacket FromZoom(string sightId, int scopeIndexInsideSight, float zoomValue)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ScopeStatesPacket>(EFirearmSubPacketType.ToggleScopeStates);
        packet.IsZoom = true;
        packet.ZoomId = sightId;
        packet.ZoomScopeIndex = scopeIndexInsideSight;
        packet.ZoomValue = zoomValue;
        return packet;
    }

    public static ScopeStatesPacket CreateInstance()
    {
        return new();
    }

    public int Amount;
    public ScopeState[] States;
    public bool IsZoom;
    public string ZoomId;
    public int ZoomScopeIndex;
    public float ZoomValue;

    public void Execute(FikaPlayer player)
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

    public void Serialize(NetDataWriter writer)
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

    public void Deserialize(NetDataReader reader)
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

    public void Dispose()
    {
        Amount = 0;
        States = null;
        IsZoom = false;
        ZoomId = null;
        ZoomScopeIndex = 0;
        ZoomValue = 0f;
    }
}
