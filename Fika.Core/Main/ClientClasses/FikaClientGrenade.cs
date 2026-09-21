using EFT;
using System.Collections;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientGrenade : ObservedGrenade
{
    public FikaClientGrenade(IntPtr pointer) : base(pointer)
    {
    }

    private const float _smoothSpeed = 10f;

    private GrenadeSyncPacket _packet;
    private bool _hasPacket;

    private Coroutine _interpolationRoutine;

    public override void ApplyNetPacket(GrenadeSyncPacket packet, float remoteTime)
    {
        _packet = packet;
        _hasPacket = true;

        CollisionNumber = _packet.CollisionNumber;

        _interpolationRoutine ??= StartCoroutine((SmoothingCoroutine()).ToIl2Cpp());

        if (_packet.Done)
        {
            transform.SetPositionAndRotation(_packet.Position, _packet.Rotation);
            this.SetNetVelocity(_packet);
            OnDoneFromNet();

            _hasPacket = false;
        }
    }

    private IEnumerator SmoothingCoroutine()
    {
        var wait = new WaitForFixedUpdate();
        while (_hasPacket)
        {
            yield return wait;

            var t = 1f - Mathf.Exp(-_smoothSpeed * Time.fixedDeltaTime);

            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, _packet.Position, t),
                Quaternion.Lerp(transform.rotation, _packet.Rotation, t));

            if (Rigidbody != null)
            {
                if (CollisionNumber == _packet.CollisionNumber)
                {
                    Rigidbody.velocity = Vector3.Lerp(Rigidbody.velocity, _packet.Velocity, t);

                    Rigidbody.angularVelocity = Vector3.Lerp(Rigidbody.angularVelocity, _packet.AngularVelocity, t);
                }
                else
                {
                    this.SetNetVelocity(_packet);
                }
            }
        }
    }
}
