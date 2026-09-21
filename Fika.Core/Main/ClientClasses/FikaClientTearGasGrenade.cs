using EFT;
using System;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientTearGasGrenade : ObservedTearGasGrenade
{
    public FikaClientTearGasGrenade(IntPtr pointer) : base(pointer)
    {
    }

    private const float _smoothSpeed = 10f;

    public override void ApplyNetPacket(GrenadeSyncPacket packet, float remoteTime)
    {
        var t = 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);

        CollisionNumber = packet.CollisionNumber;
        var vector = Vector3.Lerp(transform.position, packet.Position, t);
        var quaternion = Quaternion.Lerp(transform.rotation, packet.Rotation, t);
        transform.SetPositionAndRotation(vector, quaternion);
        if (CollisionNumber == packet.CollisionNumber)
        {
            if (Rigidbody != null)
            {
                Rigidbody.velocity = Vector3.Lerp(Rigidbody.velocity, packet.Velocity, t);
                Rigidbody.angularVelocity = Vector3.Lerp(Rigidbody.angularVelocity, packet.AngularVelocity, t);
            }
        }
        else
        {
            this.SetNetVelocity(packet);
        }

        if (packet.Done)
        {
            transform.SetPositionAndRotation(packet.Position, packet.Rotation);
            this.SetNetVelocity(packet);
            OnDoneFromNet();
        }
    }
}
