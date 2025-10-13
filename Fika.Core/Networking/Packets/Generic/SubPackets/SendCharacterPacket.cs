using Comfort.Common;
using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class SendCharacterPacket : IPoolSubPacket
{
    private SendCharacterPacket() { }

    public static SendCharacterPacket CreateInstance()
    {
        return new();
    }

    public static SendCharacterPacket FromValue(PlayerInfoPacket playerInfoPacket, bool isAlive, bool isAi, Vector3 position, int netId)
    {
        SendCharacterPacket packet = GenericSubPacketPoolManager.Instance.GetPacket<SendCharacterPacket>(EGenericSubPacketType.SendCharacter);
        packet.PlayerInfoPacket = playerInfoPacket;
        packet.IsAlive = isAlive;
        packet.IsAI = isAi;
        packet.Position = position;
        packet.NetId = netId;
        return packet;
    }

    public PlayerInfoPacket PlayerInfoPacket;
    public bool IsAlive;
    public bool IsAI;
    public Vector3 Position;
    public int NetId;

    public void Execute(FikaPlayer player = null)
    {
        CoopHandler handler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (handler != null)
        {
            handler.QueueProfile(PlayerInfoPacket.Profile, PlayerInfoPacket.HealthByteArray, Position, NetId, IsAlive, IsAI,
                PlayerInfoPacket.ControllerId, PlayerInfoPacket.FirstOperationId, PlayerInfoPacket.IsZombie,
                PlayerInfoPacket.ItemId, PlayerInfoPacket.ControllerType);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerInfoPacket = reader.GetPlayerInfoPacket();
        IsAlive = reader.GetBool();
        IsAI = reader.GetBool();
        Position = reader.GetUnmanaged<Vector3>();
        NetId = reader.GetInt();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutPlayerInfoPacket(PlayerInfoPacket);
        writer.Put(IsAlive);
        writer.Put(IsAI);
        writer.PutUnmanaged(Position);
        writer.Put(NetId);
    }

    public void Dispose()
    {
        PlayerInfoPacket = default;
        IsAlive = false;
        IsAI = false;
        Position = default;
        NetId = 0;
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
