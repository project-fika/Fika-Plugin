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
        RaidsText.SetText(LocaleUtils.UI_MM_RAIDSHEADER.Localized());
        JoinText.SetText(LocaleUtils.UI_MM_JOIN_BUTTON.Localized());
        HostRaidText.SetText(LocaleUtils.UI_MM_HOST_BUTTON.Localized());
        SelectAmountText.SetText(LocaleUtils.UI_MM_SESSION_SETTINGS_HEADER.Localized());
        UseDedicatedHostText.SetText(LocaleUtils.UI_MM_USE_DEDICATED_HOST.Localized());
        StartText.SetText(LocaleUtils.UI_MM_START_BUTTON.Localized());
        LoadingScreenHeaderText.SetText(LocaleUtils.UI_MM_LOADING_HEADER.Localized());
        LoadingScreenInfoText.SetText(LocaleUtils.UI_MM_LOADING_DESCRIPTION.Localized());

        JoinAsSpectatorText.SetText(LocaleUtils.UI_MM_JOIN_AS_SPECTATOR.Localized().ToUpper());

        var spectateTooltip = SpectatorToggle.gameObject.AddComponent<HoverTooltipArea>();
        spectateTooltip.SetMessageText(LocaleUtils.UI_MM_JOIN_AS_SPECTATOR_DESCRIPTION.Localized());
    }
}
