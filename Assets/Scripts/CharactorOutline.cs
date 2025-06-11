using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CharactorOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   public GameObject glowEffect;
    public Image baseImage;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (glowEffect != null)
            glowEffect.SetActive(true);

        //마우스 올리면 투명화시키기
        Color color = baseImage.color;
        color.a = 0f;
        baseImage.color = color;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (glowEffect != null)
            glowEffect.SetActive(false);

        if (baseImage != null)
        {
            Color color = baseImage.color;
            color.a = 1f; 
            baseImage.color = color;
        }
    }
}
