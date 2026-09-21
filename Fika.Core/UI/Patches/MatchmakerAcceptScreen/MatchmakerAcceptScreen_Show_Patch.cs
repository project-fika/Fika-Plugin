using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches.MatchmakerAcceptScreen;

public class MatchmakerAcceptScreen_Show_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchMakerAcceptScreen).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.Name == "Show" && x.GetParameters()[0].Name == "session");
    }

    [PatchPrefix]
    public static void Prefix(MatchMakerAcceptScreen __instance, ref RaidSettings raidSettings)
    {
        FikaBackendUtils.IsScav = raidSettings.IsScav;

        var newMatchMaker = __instance.gameObject.GetOrAddComponent<MatchMakerUIScript>();
        newMatchMaker.RaidSettings = raidSettings;
        newMatchMaker.AcceptButton = __instance._acceptButton;
        newMatchMaker.BackButton = __instance._backButton;
    }

    [PatchPostfix]
    public static void Postfix(ref IEftSession session, MatchMakerAcceptScreen __instance)
    {
        FikaBackendUtils.MatchMakerAcceptScreenInstance = __instance;
        FikaBackendUtils.Profile = session.Profile;
    }
}
