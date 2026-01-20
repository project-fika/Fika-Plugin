using System.Reflection;
using Audio.SpatialSystem;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Utils;
using RootMotion.FinalIK;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.PlayerPatches;

/// <summary>
/// Fixes a bug with BSG's <see cref="SpatialAudioSystem"/> causing and endless spam of nullrefs, reducing the framerate for every player
/// </summary>
public class Player_SpawnInHands_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.SpawnInHands));
    }

    [PatchPrefix]
    public static bool Prefix(Player __instance, Item item, string parentBone, ref GameObject ____spawnedKey, LimbIK[] ____limbs)
    {
        if (__instance.IsYourPlayer)
        {
            return true;
        }

        ____spawnedKey = Singleton<PoolManagerClass>.Instance.CreateItem(item, Player.GetVisibleToCamera(__instance), __instance, true);
        var transform = ____spawnedKey.transform.FindTransform("pivot");
        var transform2 = ____limbs[0].solver.bone3.transform.FindTransform(parentBone);
        ____spawnedKey.transform.SetParent(transform2, false);
        ____spawnedKey.transform.localRotation = Quaternion.identity;
        ____spawnedKey.transform.localPosition = Vector3.zero;
        ____spawnedKey.SetActive(true);
        if (transform != null)
        {
            var quaternion = Quaternion.Inverse(transform.rotation) * transform2.rotation;
            ____spawnedKey.transform.localRotation *= quaternion;
            var vector = transform2.position - transform.position;
            ____spawnedKey.transform.position += vector;
        }
        else
        {
#if DEBUG
            FikaGlobals.LogError($"pivot not found in, {____spawnedKey}, for keyId = {item.Id}");
#endif
        }

        return false;
    }
}
