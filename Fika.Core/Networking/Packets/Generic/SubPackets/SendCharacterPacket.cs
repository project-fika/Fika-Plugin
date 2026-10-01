using EFT;
using Fika.Core.Main.Components;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct SendCharacterPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.SendCharacter;

    public SendCharacterPacket(PlayerInfoPacket playerInfoPacket, bool isAlive, bool isAi, Vector3 position, int netId)
    {
        PlayerInfoPacket = playerInfoPacket;
        IsAlive = isAlive;
        IsAI = isAi;
        Position = position;
        NetId = netId;
    }

    public SendCharacterPacket(NetDataReader reader)
    {
        PlayerInfoPacket = reader.GetPlayerInfoPacket();
        IsAlive = reader.GetBool();
        IsAI = reader.GetBool();
        Position = reader.GetUnmanaged<Vector3>();
        NetId = reader.GetInt();
    }

    public readonly PlayerInfoPacket PlayerInfoPacket;
    public readonly bool IsAlive;
    public readonly bool IsAI;
    public readonly Vector3 Position;
    public readonly int NetId;

    public readonly void Execute()
    {
        if (CoopHandler.TryGetCoopHandler(out var handler))
        {
            handler.QueueProfile(PlayerInfoPacket.Profile, PlayerInfoPacket.HealthByteArray, Position, NetId, IsAlive, IsAI,
                PlayerInfoPacket.ControllerId, PlayerInfoPacket.FirstOperationId, PlayerInfoPacket.IsZombie,
                PlayerInfoPacket.ItemId, PlayerInfoPacket.ControllerType);
        }
    }



    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutPlayerInfoPacket(PlayerInfoPacket);
        writer.Put(IsAlive);
        writer.Put(IsAI);
        writer.PutUnmanaged(Position);
        writer.Put(NetId);
    }
}

public struct PlayerInfoPacket
{
    public Profile Profile;
    public MongoID ControllerId;
    public MongoID? ItemId;

    public byte[] HealthByteArray;

    public ushort FirstOperationId;
    public EHandsControllerType ControllerType;

    public bool IsStationary;
    public bool IsZombie;
}
