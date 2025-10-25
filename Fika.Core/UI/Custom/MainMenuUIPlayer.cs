using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;
using static Fika.Core.UI.FikaUIGlobals;

public class MainMenuUIPlayer : MonoBehaviour
{
    public TextMeshProUGUI PlayerName;
    public TextMeshProUGUI PlayerLevel;
    public TextMeshProUGUI PlayerStatus;
    public Image StatusImage;

    public void SetActivity(string nickname, int level, EFikaPlayerPresence presence)
    {
        PlayerName.text = nickname;
        PlayerLevel.text = $"({level})";
        PlayerStatus.text = presence switch
        {
            EFikaPlayerPresence.IN_MENU => LocaleUtils.UI_MMUI_IN_MENU.Localized(),
            EFikaPlayerPresence.IN_RAID => LocaleUtils.UI_MMUI_IN_RAID.Localized(),
            EFikaPlayerPresence.IN_STASH => LocaleUtils.UI_MMUI_IN_STASH.Localized(),
            EFikaPlayerPresence.IN_HIDEOUT => LocaleUtils.UI_MMUI_IN_HIDEOUT.Localized(),
            EFikaPlayerPresence.IN_FLEA => LocaleUtils.UI_MMUI_IN_FLEA.Localized(),
            _ => LocaleUtils.UI_MMUI_IN_MENU.Localized(),
        };
        SetImageColor(presence);
    }

    private void SetImageColor(EFikaPlayerPresence presence)
    {
        StatusImage.color = presence switch
        {
            EFikaPlayerPresence.IN_MENU => Color.green,
            EFikaPlayerPresence.IN_RAID => Color.red,
            EFikaPlayerPresence.IN_STASH or EFikaPlayerPresence.IN_HIDEOUT or EFikaPlayerPresence.IN_FLEA => Color.yellow,
            _ => Color.green,
        };
    }
}
