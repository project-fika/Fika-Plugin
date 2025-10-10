using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Main.Patches.Overrides;

public class GetProfileAtEndOfRaidPatch_Override : ModulePatch
{
    public static CompleteProfileDescriptorClass ProfileDescriptor { get; private set; }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CoopGame), nameof(CoopGame.Stop));
    }

    [PatchPrefix]
    public static void PatchPrefix(CoopGame __instance)
    {
        ProfileDescriptor = new CompleteProfileDescriptorClass(__instance.Profile_0, FikaGlobals.SearchControllerSerializer);
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
        return AccessTools.Method(typeof(PostRaidHealthScreenClass), nameof(PostRaidHealthScreenClass.method_2));
    }

    [PatchPrefix]
    public static void PatchPrefix(ref ISession ___ISession)
    {
        Profile profile = new(GetProfileAtEndOfRaidPatch_Override.ProfileDescriptor);

        if (profile.Side != EPlayerSide.Savage)
        {
            return;
        }

        ProfileEndpointFactoryAbstractClass session = (ProfileEndpointFactoryAbstractClass)___ISession;
        session.AllProfiles =
        [
            session.AllProfiles.First(x => x.Side != EPlayerSide.Savage),
            profile
        ];
        session.ProfileOfPet.LearnAll();

        // make a request to the server, so it knows of the items we might transfer
        RequestHandler.PutJson("/raid/profile/scavsave",
            GetProfileAtEndOfRaidPatch_Override.ProfileDescriptor.ToUnparsedData([]).JObject.ToString());
    }
}
