using TMPro;
using UnityEngine;

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
}
