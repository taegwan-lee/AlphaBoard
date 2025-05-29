using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CharactorOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    private Image image;

    public Color hoverOutlineColor = new Color(1f, 0.8f, 0f); 
    public float outlineWidth = 2f;

    void Start()
    {
        image = GetComponent<Image>();
        outline = gameObject.AddComponent<Outline>();
        outline.effectColor = Color.clear;
        outline.effectDistance = new Vector2(outlineWidth, outlineWidth);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.effectColor = hoverOutlineColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.effectColor = Color.clear;
    }
}
