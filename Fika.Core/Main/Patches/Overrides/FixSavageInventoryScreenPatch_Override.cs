using System.Linq;
using System.Reflection;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Common.Http;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Overrides;

public class GetProfileAtEndOfRaidPatch_Override : ModulePatch
{
    public static ProfileDescriptor ProfileDescriptor { get; private set; }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CoopGame), nameof(CoopGame.Stop));
    }

    [PatchPrefix]
    public static void PatchPrefix(CoopGame __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        ProfileDescriptor = new ProfileDescriptor(__instance.Profile, FikaGlobals.SearchControllerSerializer);
    }
}
/// <summary>
/// Get profile from other patch (GetProfileAtEndOfRaidPatch)
/// if our profile is savage Create new Session.AllProfiles and pass in our own profile to allow us to use the ScavengerInventoryScreen
/// </summary>
public class FixSavageInventoryScreenPatch_Override : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SessionResultShowOperation), nameof(SessionResultShowOperation.Init));
    }

    [PatchPrefix]
    public static void PatchPrefix(EFT.SessionResultShowOperation __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        Profile profile = new(GetProfileAtEndOfRaidPatch_Override.ProfileDescriptor);

        if (profile.Side != EPlayerSide.Savage)
        {
            return;
        }

        var session = (ClientBackendSession)__instance._session;
        session.AllProfiles = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Profile>(
        [
            session.AllProfiles.First(x => x.Side != EPlayerSide.Savage),
            profile
        ]);
        session.ProfileOfPet.LearnAll();

        // make a request to the server, so it knows of the items we might transfer
        RequestHandler.PutJson("/raid/profile/scavsave",
            GetProfileAtEndOfRaidPatch_Override.ProfileDescriptor.ToJson());
    }
}
