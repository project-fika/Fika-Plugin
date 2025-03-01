using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    private Image handleImage;
    private TextMeshProUGUI handleText;
    private WaitForFixedUpdate fixedUpdateAwaiter;
    private float alphaModifier;

    [SerializeField]
    Sprite buttonSprite;

    protected void Awake()
    {
        fixedUpdateAwaiter = new();
        alphaModifier = 0.15f;
    }

    protected void Start()
    {
        handleImage = GetComponent<Image>();
        handleText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private IEnumerator FadeButton()
    {
        while (handleImage.color.a > 0)
        {
            yield return fixedUpdateAwaiter;
            handleImage.color = new Color(0.9059f, 0.898f, 0.8314f, handleImage.color.a - alphaModifier);
            handleText.color = new Color(0, 0, 0, handleText.color.a - alphaModifier);
        }
        while (handleText.color.a < 1)
        {
            yield return fixedUpdateAwaiter;
            handleText.color = new Color(0.9059f, 0.898f, 0.8314f, handleText.color.a + alphaModifier);
        }
    }

    private IEnumerator ShowButton()
    {
        while (handleText.color.a > 0)
        {
            yield return fixedUpdateAwaiter;
            handleText.color = new Color(0.9059f, 0.898f, 0.8314f, handleText.color.a - alphaModifier);
        }
        while (handleImage.color.a < 1)
        {
            yield return fixedUpdateAwaiter;
            handleImage.color = new Color(0.9059f, 0.898f, 0.8314f, handleImage.color.a + alphaModifier);
            handleText.color = new Color(0, 0, 0, handleText.color.a + alphaModifier);
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
