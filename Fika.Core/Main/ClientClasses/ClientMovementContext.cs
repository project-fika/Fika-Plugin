using System;
using EFT;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ClientClasses;

public class ClientMovementContext : MovementContext
{
    public ClientMovementContext(IntPtr pointer) : base(pointer)
    {
    }

    public ClientMovementContext() : base(Il2CppInjection.Allocate<ClientMovementContext>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<MovementContext>(this);
    }

    private bool _doGravity;

    public new static ClientMovementContext Create(Player player, Il2CppSystem.Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
    {
        var movementContext = Create<ClientMovementContext>(player, characterControllerGetter, groundMask);
        return movementContext;
    }

    public override void Init()
    {
        _doGravity = true;
        base.Init();
        if (!_player.IsYourPlayer && _player.IsAI && !FikaBackendUtils.IsHeadless)
        {
            // Fix base game bug where idle animations are not playing
            PlayerAnimator.SetIsThirdPerson(true);
            PlayerAnimator.SetLayerWeight(1, 1f);
        }
    }

    public override void ApplyGravity(ref Vector3 motion, float deltaTime, bool stickToGround)
    {
        if (!_doGravity)
        {
            return;
        }

        base.ApplyGravity(ref motion, deltaTime, stickToGround);
    }

    public void SetGravity(bool enabled)
    {
        _doGravity = enabled;
    }
}
