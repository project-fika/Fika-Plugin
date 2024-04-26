#pragma warning disable CS0169
#pragma warning disable CS0649
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntUpDown : MonoBehaviour
{
    [SerializeField]
    Button increaseButton;
    [SerializeField]
    Button decreaseButton;
    [SerializeField]
    int AmountOfPlayers = 1;
    [SerializeField]
    TextMeshProUGUI AmountText;

    public void IncreaseAmount()
    {
        AmountOfPlayers++;
        AmountOfPlayers = Mathf.Clamp(AmountOfPlayers, 1, 32);

        AmountText.text = AmountOfPlayers.ToString();
    }

    public void DecreaseAmount()
    {
        AmountOfPlayers--;
        AmountOfPlayers = Mathf.Clamp(AmountOfPlayers, 1, 32);

        AmountText.text = AmountOfPlayers.ToString();
    }
}