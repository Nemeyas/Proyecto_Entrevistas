using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    public float hoverScale = 1.04f;
    public float pressedScale = 0.96f;
    public float transitionSpeed = 12f;

    private Image buttonImage;
    private Vector3 targetScale = Vector3.one;
    private Color targetColor = Color.white;

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        targetColor = normalColor;
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    void OnEnable()
    {
        targetScale = Vector3.one;
        targetColor = normalColor;
        transform.localScale = Vector3.one;
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    void Update()
    {
        // Smoothly interpolate scale and color for a highly responsive, modern feel
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);
        
        if (buttonImage != null)
        {
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = new Vector3(hoverScale, hoverScale, 1f);
        targetColor = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
        targetColor = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = new Vector3(pressedScale, pressedScale, 1f);
        targetColor = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = new Vector3(hoverScale, hoverScale, 1f);
        targetColor = hoverColor;
    }
}
