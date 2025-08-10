using TMPro;
using UnityEngine.UI;

public class RaidAdminUI : MonoBehaviour
{
    public TMP_Dropdown ClientSelection;
    public Button CloseButton;

    public GameObject InfoPane;

    public TextMeshProUGUI HeaderText;
    public TextMeshProUGUI SentPacketsText;
    public TextMeshProUGUI ReceivedPacketsText;
    public TextMeshProUGUI SentDataText;
    public TextMeshProUGUI ReceivedDataText;
    public TextMeshProUGUI PacketLossText;
    public TextMeshProUGUI PacketLossPercentText;
    public Button KickButton;
}