using EFT.UI;
using Fika.Core.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SendItemUI : MonoBehaviour
{
	[SerializeField]
	public Button CloseButton;
	[SerializeField]
	public Button SendButton;
	[SerializeField]
	public TMP_Dropdown PlayersDropdown;
	[SerializeField]
#pragma warning disable CS0649
	TextMeshProUGUI HeaderText;
	[SerializeField]
	TextMeshProUGUI SendText;
#pragma warning restore CS0649

	protected void Awake()
	{
		HeaderText.text = LocaleUtils.UI_SENDITEM_HEADER.Localized();
		SendText.text = LocaleUtils.UI_SENDITEM_BUTTON.Localized();
		GameObject gameObjectToAdd = gameObject.transform.GetChild(0).GetChild(0).gameObject;
		RectTransform rectTransform = gameObjectToAdd.RectTransform();
		gameObjectToAdd.AddComponent<UIDragComponent>().Init(rectTransform, true);
	}
}