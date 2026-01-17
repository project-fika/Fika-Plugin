using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button RefreshButton;
    public TextMeshProUGUI Label;
    public GameObject PlayerTemplate;

    public void UpdateLabel(int amount)
    {
        Label.SetText(LocaleUtils.UI_MMUI_ONLINE_PLAYERS.Localized(), amount);
    }
}
