using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchMakerUI : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI serversNumberAmount;
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

    public void SetServerNumberText(string text)
    {
        serversNumberAmount.SetText(text);
    }
}
