using Fika.Core.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchMakerUI : MonoBehaviour
{
	[SerializeField]
	public GameObject ServerBrowserPanel;
	[SerializeField]
	public Button RefreshButton;
	[SerializeField]
	public Button RaidGroupHostButton;
	[SerializeField]
	public GameObject RaidGroupDefaultToClone;
	[SerializeField]
	public GameObject PlayerAmountSelection;
	[SerializeField]
	public Button StartButton;
	[SerializeField]
	public Button CloseButton;
	[SerializeField]
	public TextMeshProUGUI PlayerAmountText;
	[SerializeField]
	public Toggle DedicatedToggle;
	[SerializeField]
	public GameObject LoadingScreen;
	[SerializeField]
	public Image LoadingImage;
	[SerializeField]
	public Toggle SpectatorToggle;

	[SerializeField]
	public TextMeshProUGUI RaidsText;
	[SerializeField]
	public TextMeshProUGUI JoinText;
	[SerializeField]
	public TextMeshProUGUI HostRaidText;
	[SerializeField]
	public TextMeshProUGUI SelectAmountText;
	[SerializeField]
	public TextMeshProUGUI HowManyPeopleText;
	[SerializeField]
	public TextMeshProUGUI UseDedicatedHostText;
	[SerializeField]
	public TextMeshProUGUI StartText;
	[SerializeField]
	public TextMeshProUGUI LoadingAnimationText;
	[SerializeField]
	public TextMeshProUGUI LoadingScreenHeaderText;
	[SerializeField]
	public TextMeshProUGUI LoadingScreenInfoText;

	protected void Awake()
	{
		RaidsText.text = LocaleUtils.UI_MM_RAIDSHEADER.Localized();
		JoinText.text = LocaleUtils.UI_MM_JOIN_BUTTON.Localized();
		HostRaidText.text = LocaleUtils.UI_MM_HOST_BUTTON.Localized();
		SelectAmountText.text = LocaleUtils.UI_MM_SELECT_AMOUNT_HEADER.Localized();
		HowManyPeopleText.text = LocaleUtils.UI_MM_SELECT_AMOUNT_DESCRIPTION.Localized();
		UseDedicatedHostText.text = LocaleUtils.UI_MM_USE_DEDICATED_HOST.Localized();
		StartText.text = LocaleUtils.UI_MM_START_BUTTON.Localized();
		LoadingScreenHeaderText.text = LocaleUtils.UI_MM_LOADING_HEADER.Localized();
		LoadingScreenInfoText.text = LocaleUtils.UI_MM_LOADING_DESCRIPTION.Localized();
	}
}
