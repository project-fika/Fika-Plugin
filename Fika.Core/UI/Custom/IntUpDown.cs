#pragma warning disable CS0169
#pragma warning disable CS0649
using TMPro;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

public class IntUpDown : MonoBehaviour
{
    public IntUpDown(IntPtr pointer) : base(pointer)
    {
    }

    Button _increaseButton;
    Button _decreaseButton;
    int _amountOfPlayers = 1;
    TextMeshProUGUI _amountText;

    public void IncreaseAmount()
    {
        _amountOfPlayers++;
        _amountOfPlayers = Mathf.Clamp(_amountOfPlayers, 1, 32);

        _amountText.SetText("{0}", _amountOfPlayers);
    }

    public void DecreaseAmount()
    {
        _amountOfPlayers--;
        _amountOfPlayers = Mathf.Clamp(_amountOfPlayers, 1, 32);

        _amountText.SetText("{0}", _amountOfPlayers);
    }
}