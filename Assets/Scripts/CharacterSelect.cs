using UnityEngine;
using UnityEngine.EventSystems;

public class NewMonoBehaviourScript : MonoBehaviour, IPointerClickHandler
{
    public GameObject dialogPanel;//대화창 스크립트 패널
    public CharactorCamera charactorCamera;

    void Start()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //charactorCamera.MoveCameraToTarget();
        dialogPanel.SetActive(true);
    }
}
