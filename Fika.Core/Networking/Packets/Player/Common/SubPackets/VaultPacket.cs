using EFT.Vaulting;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct VaultPacket
{
    public VaultPacket(EVaultingStrategy vaultingStrategy, Vector3 vaultingPoint, float vaultingHeight, float vaultingLength, float vaultingSpeed, float behindObstacleHeight, float absoluteForwardVelocity)
    {
        VaultingStrategy = vaultingStrategy;
        VaultingPoint = vaultingPoint;
        VaultingHeight = vaultingHeight;
        VaultingLength = vaultingLength;
        VaultingSpeed = vaultingSpeed;
        BehindObstacleHeight = behindObstacleHeight;
        AbsoluteForwardVelocity = absoluteForwardVelocity;
    }

    public readonly EVaultingStrategy VaultingStrategy;
    public readonly Vector3 VaultingPoint;
    public readonly float VaultingHeight;
    public readonly float VaultingLength;
    public readonly float VaultingSpeed;
    public readonly float BehindObstacleHeight;
    public readonly float AbsoluteForwardVelocity;

    public readonly void Execute(FikaPlayer player)
    {
        // a headless client can get stuck in permanent high-velocity states due to vaulting, skip it
        if (!FikaBackendUtils.IsHeadless)
        {
            player.DoObservedVault(this);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(VaultingStrategy);
        writer.PutUnmanaged(VaultingPoint);
        writer.Put(VaultingHeight);
        writer.Put(VaultingLength);
        writer.Put(VaultingSpeed);
        writer.Put(BehindObstacleHeight);
        writer.Put(AbsoluteForwardVelocity);
    }
}
