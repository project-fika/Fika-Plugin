using EFT;
using Fika.Core.Coop.GameMode;
using HarmonyLib;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Overrides
{
	public class GetProfileAtEndOfRaidPatch_Override : ModulePatch
	{
		public static string Profile { get; private set; }
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(CoopGame), nameof(CoopGame.Stop));
		}

		[PatchPrefix]
		public static void PatchPrefix(CoopGame __instance)
		{
			Profile = __instance.Profile_0.ToJson();
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
		public static void PatchPrefix(ref ISession ___iSession)
		{
			Profile profile = GetProfileAtEndOfRaidPatch_Override.Profile.ParseJsonTo<Profile>();

			if (profile.Side != EPlayerSide.Savage)
			{
				return;
			}

			ProfileEndpointFactoryAbstractClass session = (ProfileEndpointFactoryAbstractClass)___iSession;
			session.AllProfiles =
			[
				session.AllProfiles.First(x => x.Side != EPlayerSide.Savage),
				profile
			];
			session.ProfileOfPet.LearnAll();

			// make a request to the server, so it knows of the items we might transfer
			RequestHandler.PutJson("/raid/profile/scavsave", new
			{
				profile = session.ProfileOfPet
			}.ToJson());
		}
	}
}
