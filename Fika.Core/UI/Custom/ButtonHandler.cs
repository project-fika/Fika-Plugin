using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    private Image _handleImage;
    private TextMeshProUGUI _handleText;
    private WaitForFixedUpdate _fixedUpdateAwaiter;
    private float _alphaModifier;

    [SerializeField]
#pragma warning disable CS0169 // Remove unused private members
    Sprite buttonSprite;
#pragma warning restore CS0169 // Remove unused private members

    protected void Awake()
    {
        _fixedUpdateAwaiter = new();
        _alphaModifier = 0.15f;
    }

    protected void Start()
    {
        _handleImage = GetComponent<Image>();
        _handleText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private IEnumerator FadeButton()
    {
        while (_handleImage.color.a > 0)
        {
            yield return _fixedUpdateAwaiter;
            _handleImage.color = new Color(0.9059f, 0.898f, 0.8314f, _handleImage.color.a - _alphaModifier);
            _handleText.color = new Color(0, 0, 0, _handleText.color.a - _alphaModifier);
        }
        while (_handleText.color.a < 1)
        {
            yield return _fixedUpdateAwaiter;
            _handleText.color = new Color(0.9059f, 0.898f, 0.8314f, _handleText.color.a + _alphaModifier);
        }
    }

    private IEnumerator ShowButton()
    {
        while (_handleText.color.a > 0)
        {
            yield return _fixedUpdateAwaiter;
            _handleText.color = new Color(0.9059f, 0.898f, 0.8314f, _handleText.color.a - _alphaModifier);
        }
        while (_handleImage.color.a < 1)
        {
            yield return _fixedUpdateAwaiter;
            _handleImage.color = new Color(0.9059f, 0.898f, 0.8314f, _handleImage.color.a + _alphaModifier);
            _handleText.color = new Color(0, 0, 0, _handleText.color.a + _alphaModifier);
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
