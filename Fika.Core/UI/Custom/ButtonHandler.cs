using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
	private Image handleImage;
	private TextMeshProUGUI handleText;

	[SerializeField]
#pragma warning disable CS0169 // Remove unused private members
	Sprite buttonSprite;
#pragma warning restore CS0169 // Remove unused private members

	void Start()
	{
		handleImage = GetComponent<Image>();
		handleText = GetComponentInChildren<TextMeshProUGUI>();
	}

	private IEnumerator FadeButton()
	{
		while (handleImage.color.a > 0)
		{
			yield return new WaitForFixedUpdate();
			handleImage.color = new Color(0.9059f, 0.898f, 0.8314f, handleImage.color.a - 0.15f);
			handleText.color = new Color(0, 0, 0, handleText.color.a - 0.15f);
		}
		while (handleText.color.a < 1)
		{
			yield return new WaitForFixedUpdate();
			handleText.color = new Color(0.9059f, 0.898f, 0.8314f, handleText.color.a + 0.15f);
		}
	}

	private IEnumerator ShowButton()
	{
		while (handleText.color.a > 0)
		{
			yield return new WaitForFixedUpdate();
			handleText.color = new Color(0.9059f, 0.898f, 0.8314f, handleText.color.a - 0.15f);
		}
		while (handleImage.color.a < 1)
		{
			yield return new WaitForFixedUpdate();
			handleImage.color = new Color(0.9059f, 0.898f, 0.8314f, handleImage.color.a + 0.15f);
			handleText.color = new Color(0, 0, 0, handleText.color.a + 0.15f);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		StopAllCoroutines();
		StartCoroutine(ShowButton());
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		StopAllCoroutines();
		StartCoroutine(FadeButton());
	}
}
