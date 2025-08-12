using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player;

public class StationaryPacket : IPoolSubPacket
{
    private StationaryPacket()
    {

    }

    public static StationaryPacket CreateInstance()
    {
        return new();
    }

    public static StationaryPacket FromValue(EStationaryCommand command, string id = null)
    {
        StationaryPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<StationaryPacket>(ECommonSubPacketType.Stationary);
        packet.Command = command;
        packet.Id = id;
        return packet;
    }

    public EStationaryCommand Command;
    public string Id;

    public void Execute(FikaPlayer player)
    {
        StationaryWeapon stationaryWeapon = (Command == EStationaryCommand.Occupy)
            ? Singleton<GameWorld>.Instance.FindStationaryWeapon(Id) : null;
        player.ObservedStationaryInteract(stationaryWeapon, (StationaryPacketStruct.EStationaryCommand)Command);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(Command);
        if (Command == EStationaryCommand.Occupy)
        {
            writer.Put(Id);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Command = reader.GetEnum<EStationaryCommand>();
        if (Command == EStationaryCommand.Occupy)
        {
            Id = reader.GetString();
        }
    }

    public void Dispose()
    {
        Command = default;
        Id = null;
    }
}
