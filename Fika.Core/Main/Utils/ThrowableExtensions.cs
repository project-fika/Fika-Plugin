using EFT;

namespace Fika.Core.Main.Utils;

public static class ThrowableExtensions
{
    // In 1.1 BSG apply the network state of this through their own snapshot interpolation
    public static void SetNetVelocity(this Throwable throwable, GrenadeSyncPacket packet)
    {
        var rigidbody = throwable.Rigidbody;
        if (rigidbody != null)
        {
            rigidbody.velocity = packet.Velocity;
            rigidbody.angularVelocity = packet.AngularVelocity;
        }
    }
}
