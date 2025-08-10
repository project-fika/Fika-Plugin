using EFT.UI;
using Fika.Core.Utils;
using TMPro;
using UnityEngine.UI;

public class SendItemUI : MonoBehaviour
{
    public Button CloseButton;
    public Button SendButton;
    public TMP_Dropdown PlayersDropdown;
    [SerializeField]
#pragma warning disable CS0649
    TextMeshProUGUI _headerText;
    [SerializeField]
    TextMeshProUGUI _sendText;
#pragma warning restore CS0649

    protected void Awake()
    {
        _headerText.text = LocaleUtils.UI_SENDITEM_HEADER.Localized();
        _sendText.text = LocaleUtils.UI_SENDITEM_BUTTON.Localized();
        GameObject gameObjectToAdd = gameObject.transform.GetChild(0).GetChild(0).gameObject;
        RectTransform rectTransform = gameObjectToAdd.RectTransform();
        gameObjectToAdd.AddComponent<UIDragComponent>().Init(rectTransform, true);
    }
}