using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CharactorOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   public GameObject glowEffect; // Inspector에서 연결

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (glowEffect != null)
            glowEffect.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (glowEffect != null)
            glowEffect.SetActive(false);
    }
}
