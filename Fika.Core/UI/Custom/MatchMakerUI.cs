using EFT.UI;
using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;

public class MatchMakerUI : MonoBehaviour
{
    public GameObject ServerBrowserPanel;
    public Button RefreshButton;
    public Button RaidGroupHostButton;
    public GameObject RaidGroupDefaultToClone;
    public GameObject DediSelection;
    public Button StartButton;
    public Button CloseButton;
    public Toggle DedicatedToggle;
    public GameObject LoadingScreen;
    public Image LoadingImage;
    public Toggle SpectatorToggle;

    public TextMeshProUGUI RaidsText;
    public TextMeshProUGUI JoinText;
    public TextMeshProUGUI HostRaidText;
    public TextMeshProUGUI SelectAmountText;
    public TextMeshProUGUI UseDedicatedHostText;
    public TextMeshProUGUI StartText;
    public TextMeshProUGUI LoadingAnimationText;
    public TextMeshProUGUI LoadingScreenHeaderText;
    public TextMeshProUGUI LoadingScreenInfoText;
    public TextMeshProUGUI JoinAsSpectatorText;
    public TMP_Dropdown HeadlessSelection;

    protected void Awake()
    {
        RaidsText.text = LocaleUtils.UI_MM_RAIDSHEADER.Localized();
        JoinText.text = LocaleUtils.UI_MM_JOIN_BUTTON.Localized();
        HostRaidText.text = LocaleUtils.UI_MM_HOST_BUTTON.Localized();
        SelectAmountText.text = LocaleUtils.UI_MM_SESSION_SETTINGS_HEADER.Localized();
        UseDedicatedHostText.text = LocaleUtils.UI_MM_USE_DEDICATED_HOST.Localized();
        StartText.text = LocaleUtils.UI_MM_START_BUTTON.Localized();
        LoadingScreenHeaderText.text = LocaleUtils.UI_MM_LOADING_HEADER.Localized();
        LoadingScreenInfoText.text = LocaleUtils.UI_MM_LOADING_DESCRIPTION.Localized();
        JoinAsSpectatorText.text = LocaleUtils.UI_MM_JOIN_AS_SPECTATOR.Localized().ToUpper();

        HoverTooltipArea spectateTooltip = SpectatorToggle.gameObject.AddComponent<HoverTooltipArea>();
        spectateTooltip.SetMessageText(LocaleUtils.UI_MM_JOIN_AS_SPECTATOR_DESCRIPTION.Localized());
    }
}
