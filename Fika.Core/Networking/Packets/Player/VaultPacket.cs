using EFT.Vaulting;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player;

public class VaultPacket : IPoolSubPacket
{
    private VaultPacket()
    {

    }

    public static VaultPacket CreateInstance()
    {
        return new();
    }

    public static VaultPacket FromValue(EVaultingStrategy vaultingStrategy, Vector3 vaultingPoint, float vaultingHeight, float vaultingLength, float vaultingSpeed, float behindObstacleHeight, float absoluteForwardVelocity)
    {
        VaultPacket packet = CommonSubPacketPoolManager.Instance.GetPacket<VaultPacket>(ECommonSubPacketType.Vault);
        packet.VaultingStrategy = vaultingStrategy;
        packet.VaultingPoint = vaultingPoint;
        packet.VaultingHeight = vaultingHeight;
        packet.VaultingLength = vaultingLength;
        packet.VaultingSpeed = vaultingSpeed;
        packet.BehindObstacleHeight = behindObstacleHeight;
        packet.AbsoluteForwardVelocity = absoluteForwardVelocity;
        return packet;
    }

    public EVaultingStrategy VaultingStrategy;
    public Vector3 VaultingPoint;
    public float VaultingHeight;
    public float VaultingLength;
    public float VaultingSpeed;
    public float BehindObstacleHeight;
    public float AbsoluteForwardVelocity;

    public void Execute(FikaPlayer player)
    {
        // A headless client can get stuck in permanent high-velocity states due to vaulting, skip it
        if (!FikaBackendUtils.IsHeadless)
        {
            player.DoObservedVault(this);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(VaultingStrategy);
        writer.PutUnmanaged(VaultingPoint);
        writer.Put(VaultingHeight);
        writer.Put(VaultingLength);
        writer.Put(VaultingSpeed);
        writer.Put(BehindObstacleHeight);
        writer.Put(AbsoluteForwardVelocity);
    }

    public void Deserialize(NetDataReader reader)
    {
        VaultingStrategy = reader.GetEnum<EVaultingStrategy>();
        VaultingPoint = reader.GetUnmanaged<Vector3>();
        VaultingHeight = reader.GetFloat();
        VaultingLength = reader.GetFloat();
        VaultingSpeed = reader.GetFloat();
        BehindObstacleHeight = reader.GetFloat();
        AbsoluteForwardVelocity = reader.GetFloat();
    }

    public void Dispose()
    {
        VaultingStrategy = default;
        VaultingPoint = default;
        VaultingHeight = 0f;
        VaultingLength = 0f;
        VaultingSpeed = 0f;
        BehindObstacleHeight = 0f;
        AbsoluteForwardVelocity = 0f;
    }
}
