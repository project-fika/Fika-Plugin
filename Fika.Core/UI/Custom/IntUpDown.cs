#pragma warning disable CS0169
#pragma warning disable CS0649
using TMPro;
using UnityEngine.UI;

public class IntUpDown : MonoBehaviour
{
    [SerializeField]
    Button _increaseButton;
    [SerializeField]
    Button _decreaseButton;
    [SerializeField]
    int _amountOfPlayers = 1;
    [SerializeField]
    TextMeshProUGUI _amountText;

    public void IncreaseAmount()
    {
        _amountOfPlayers++;
        _amountOfPlayers = Mathf.Clamp(_amountOfPlayers, 1, 32);

        _amountText.text = _amountOfPlayers.ToString();
    }

    public void DecreaseAmount()
    {
        _amountOfPlayers--;
        _amountOfPlayers = Mathf.Clamp(_amountOfPlayers, 1, 32);

        _amountText.text = _amountOfPlayers.ToString();
    }
}