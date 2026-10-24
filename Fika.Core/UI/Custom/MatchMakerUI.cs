using EFT;
using EFT.UI;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;
using TMPro;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;

public class MatchMakerUI : MonoBehaviour
{
    public MatchMakerUI(IntPtr pointer) : base(pointer)
    {
    }
    
    [PrefabPath("ServerBrowserPanel")]
    public GameObject ServerBrowserPanel;

    public Button RefreshButton;

    [PrefabPath("ServerBrowserPanel/ServerBrowserPanel/HostButton")]
    public Button RaidGroupHostButton;

    [PrefabPath(RaidGroupPath)]
    public GameObject RaidGroupDefaultToClone;

    public GameObject DediSelection;
    public Button StartButton;
    public Button CloseButton;
    public Toggle DedicatedToggle;
    public GameObject LoadingScreen;
    public Image LoadingImage;

    [PrefabPath(SpectatorCheckPath)]
    public Toggle SpectatorToggle;

    [PrefabPath("ServerBrowserPanel/ServerBrowserPanel/Group/Label")]
    public TextMeshProUGUI RaidsText;

    [PrefabPath(RaidGroupPath + "/JoinButton/Text (TMP)")]
    public TextMeshProUGUI JoinText;

    [PrefabPath("ServerBrowserPanel/ServerBrowserPanel/HostButton/Text (TMP)")]
    public TextMeshProUGUI HostRaidText;

    [PrefabPath("DediSelection/DediSelectionFrame/Header/Label")]
    public TextMeshProUGUI SelectAmountText;

    [PrefabPath("DediSelection/DediSelectionFrame/DedicatedToggle/Label")]
    public TextMeshProUGUI UseDedicatedHostText;

    [PrefabPath("DediSelection/DediSelectionFrame/StartButton/Text (TMP)")]
    public TextMeshProUGUI StartText;

    [PrefabPath("LoadingScreen/LoadingScreenFrame/LoadingAnimation")]
    public TextMeshProUGUI LoadingAnimationText;

    [PrefabPath("LoadingScreen/LoadingScreenFrame/Header/Label")]
    public TextMeshProUGUI LoadingScreenHeaderText;

    [PrefabPath("LoadingScreen/LoadingScreenFrame/InfoText")]
    public TextMeshProUGUI LoadingScreenInfoText;

    [PrefabPath(SpectatorCheckPath + "/Label")]
    public TextMeshProUGUI JoinAsSpectatorText;

    public TMP_Dropdown HeadlessSelection;

    private const string RaidGroupPath = "ServerBrowserPanel/ServerBrowserPanel/Scroll View/Viewport/Content/RaidGroupDefault_CLONE_ME";
    private const string SpectatorCheckPath = "ServerBrowserPanel/ServerBrowserPanel/Group/SpectatorCheck";

    /// <summary>
    /// Attaches and wires the component on an instantiated prefab
    /// </summary>
    /// <param name="root">The instantiated prefab</param>
    public static MatchMakerUI Create(GameObject root)
    {
        var ui = PrefabWiring.Attach<MatchMakerUI>(root);
        ui?.Init();
        return ui;
    }

    private void Init()
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

        PrefabWiring.Attach<ButtonHandler>(RaidGroupHostButton.gameObject);
        PrefabWiring.Attach<ButtonHandler>(StartButton.gameObject);
    }
}
