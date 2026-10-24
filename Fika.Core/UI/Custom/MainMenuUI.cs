using EFT;
using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.UI.Custom;

public class MainMenuUI : MonoBehaviour
{
    public MainMenuUI(IntPtr pointer) : base(pointer)
    {
    }

    public Button RefreshButton;
    public TextMeshProUGUI Label;

    [PrefabPath("Canvas/Border/Frame/Selection/ActivePlayerTemplate")]
    public GameObject PlayerTemplate;

    public void UpdateLabel(int amount)
    {
        Label.SetText(LocaleUtils.UI_MMUI_ONLINE_PLAYERS.Localized(), amount);
    }
}
