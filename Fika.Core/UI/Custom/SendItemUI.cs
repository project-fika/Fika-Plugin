using EFT.UI;
using Fika.Core.Main.Utils;
using Fika.Core.UI.Custom;
using TMPro;
using EFT;
using UnityEngine.UI;
using System;
using Il2CppInterop.Runtime.Injection;

public class SendItemUI : MonoBehaviour
{
    public SendItemUI(IntPtr pointer) : base(pointer)
    {
    }

    public Button CloseButton;
    public Button SendButton;
    public TMP_Dropdown PlayersDropdown;
    public TMP_InputField PlayersFilter;

    [PrefabPath("Canvas/Border/Frame/Group/Label")]
    private TextMeshProUGUI _headerText;

    [PrefabPath("Canvas/Border/Frame/Selection/SendButton/Text (TMP)")]
    private TextMeshProUGUI _sendText;

    /// <summary>
    /// Attaches and wires the component on an instantiated prefab
    /// </summary>
    /// <param name="root">The instantiated prefab</param>
    public static SendItemUI Create(GameObject root)
    {
        var ui = PrefabWiring.Attach<SendItemUI>(root);
        ui?.Init();
        return ui;
    }

    private void Init()
    {
        _headerText.SetText(LocaleUtils.UI_SENDITEM_HEADER.Localized());
        _sendText.SetText(LocaleUtils.UI_SENDITEM_BUTTON.Localized());
        var gameObjectToAdd = gameObject.transform.GetChild(0).GetChild(0).gameObject;
        var rectTransform = gameObjectToAdd.RectTransform();
        gameObjectToAdd.AddComponent<UIDragComponent>().Init(rectTransform, true);
    }
}
