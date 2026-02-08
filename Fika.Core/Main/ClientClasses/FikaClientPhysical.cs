using EFT;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientPhysical : PlayerPhysicalClass
{
    public override void Init(IPlayer player)
    {
        base.Init(player);

        var settings = FikaBackendUtils.CustomRaidSettings;
        if (settings.DisableArmStamina)
        {
            HandsStamina.ForceMode = true;
        }
        if (settings.DisableLegStamina)
        {
            Stamina.ForceMode = true;
        }
        if (settings.DisableOverload)
        {
            EncumberDisabled = true;
        }
    }
}
