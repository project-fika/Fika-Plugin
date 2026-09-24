using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct StationaryPacket
{
    public StationaryPacket(EStationaryCommand command, string id = null)
    {
        Command = command;
        Id = id;
    }

    public StationaryPacket(NetDataReader reader)
    {
        Command = reader.GetEnum<EStationaryCommand>();
        if (Command == EStationaryCommand.Occupy)
        {
            Id = reader.GetString();
        }
    }

    public readonly EStationaryCommand Command;
    public readonly string Id;

    public readonly void Execute(FikaPlayer player)
    {
        var stationaryWeapon = Command == EStationaryCommand.Occupy
            ? Singleton<GameWorld>.Instance.FindStationaryWeapon(Id) : null;
        player.ObservedStationaryInteract(stationaryWeapon, (StationaryWeaponPacket.EStationaryCommand)Command);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(Command);
        if (Command == EStationaryCommand.Occupy)
        {
            writer.Put(Id);
        }
    }
}
