using EFT.UI;
using Fika.Core.Main.Utils;
using TMPro;
using UnityEngine.UI;

public class SendItemUI : MonoBehaviour
{
    public Button CloseButton;
    public Button SendButton;
    public TMP_Dropdown PlayersDropdown;
    public TMP_InputField PlayersFilter;
    [SerializeField]
#pragma warning disable CS0649
    TextMeshProUGUI _headerText;
    [SerializeField]
    TextMeshProUGUI _sendText;
#pragma warning restore CS0649

    protected void Awake()
    {
        _headerText.SetText(LocaleUtils.UI_SENDITEM_HEADER.Localized());
        _sendText.SetText(LocaleUtils.UI_SENDITEM_BUTTON.Localized());
        var gameObjectToAdd = gameObject.transform.GetChild(0).GetChild(0).gameObject;
        var rectTransform = gameObjectToAdd.RectTransform();
        gameObjectToAdd.AddComponent<UIDragComponent>().Init(rectTransform, true);
    }
}