using System.Reflection;
using Comfort.Common;
using SPT.Reflection.Patching;
using Systems.Effects;

namespace Fika.Core.Main.Patches.Muzzle;

/// <summary>
/// This patch skips a LINQ allocation during <see cref="MuzzleManager.Shot(bool, float)"/>
/// </summary>
/// <remarks>Allocation found by <b>ifp</b></remarks>
public sealed class MuzzleManager_Shot_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager)
            .GetMethod(nameof(MuzzleManager.Shot));
    }

    public static bool Prefix(EMuzzleParticlePivot pivot, Transform pTransform)
    {
        var instance = Singleton<Effects>.Instance;
        var commonSystems = instance.MuzzleEffect.CommonSystems;
        instance.TryAddToMBOITParticleManager(commonSystems);
        for (var i = 0; i < commonSystems.Length; i++)
        {
            var container = commonSystems[i];
            if (container.Pivot == pivot)
            {
                var rootParticleSystem = container.RootParticleSystem;
                var t = rootParticleSystem.transform;
                t.position = pTransform.position;
                t.rotation = pTransform.rotation;

                rootParticleSystem.Stop(true);
                rootParticleSystem.Play(true);
                break;
            }
        }

        return false;
    }
}
