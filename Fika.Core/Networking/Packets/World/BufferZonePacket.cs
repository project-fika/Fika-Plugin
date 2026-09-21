using EFT.BufferZone;

namespace Fika.Core.Networking.Packets.World;

public struct BufferZonePacket(EBufferZoneData status) : INetSerializable
{
    public EBufferZoneData Status = status;
    public bool Available;
    public int PlayerRaidId;

    public void Deserialize(NetDataReader reader)
    {
        Status = (EBufferZoneData)reader.GetByte();
        switch (Status)
        {
            case EBufferZoneData.Availability:
            case EBufferZoneData.DisableByZryachiyDead:
            case EBufferZoneData.DisableByPlayerDead:
                {
                    Available = reader.GetBool();
                }
                break;
            case EBufferZoneData.PlayerAccessStatus:
                {
                    Available = reader.GetBool();
                    PlayerRaidId = reader.GetInt();
                }
                break;
            case EBufferZoneData.PlayerInZoneStatusChange:
                {
                    Available = reader.GetBool();
                    PlayerRaidId = reader.GetInt();
                }
                break;
            default:
                break;
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Status);
        switch (Status)
        {
            case EBufferZoneData.Availability:
            case EBufferZoneData.DisableByZryachiyDead:
            case EBufferZoneData.DisableByPlayerDead:
                {
                    writer.Put(Available);
                }
                break;
            case EBufferZoneData.PlayerAccessStatus:
                {
                    writer.Put(Available);
                    writer.Put(PlayerRaidId);
                }
                break;
            case EBufferZoneData.PlayerInZoneStatusChange:
                {
                    writer.Put(Available);
                    writer.Put(PlayerRaidId);
                }
                break;
            default:
                break;
        }
    }
}
