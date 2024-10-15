using TMPro;
using UnityEngine;
using static Fika.Core.UI.FikaUIGlobals;

public class MainMenuUIPlayer : MonoBehaviour
{
	[SerializeField]
	public TextMeshProUGUI PlayerName;
	[SerializeField]
	public TextMeshProUGUI PlayerLevel;
	[SerializeField]
	public TextMeshProUGUI PlayerStatus;

	public void SetStatus(string name, int level, bool inRaid)
	{
		PlayerName.text = name;
		PlayerLevel.text = $"({level})";
		PlayerStatus.text = inRaid ? "In Raid" : "In Menu";
	}

	public void SetActivity(string nickname, int level, EFikaPlayerPresence presence)
	{
		PlayerName.text = nickname;
		PlayerLevel.text = $"({level})";
		string status = presence switch
		{
			EFikaPlayerPresence.IN_MENU => "In Menu",
			EFikaPlayerPresence.IN_RAID => "In Raid",
			EFikaPlayerPresence.IN_STASH => "In Stash",
			EFikaPlayerPresence.IN_HIDEOUT => "In Hideout",
			_ => "In Menu",
		};
		PlayerStatus.text = status;
	}
}
