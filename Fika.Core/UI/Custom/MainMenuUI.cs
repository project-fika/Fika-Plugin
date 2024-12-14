using Fika.Core.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
	[SerializeField]
	public Button RefreshButton;
	[SerializeField]
	public TextMeshProUGUI Label;
	[SerializeField]
	public GameObject PlayerTemplate;

	public void UpdateLabel(int amount)
	{
		Label.text = string.Format(LocaleUtils.UI_MMUI_ONLINE_PLAYERS.Localized(), amount);
	}
}
