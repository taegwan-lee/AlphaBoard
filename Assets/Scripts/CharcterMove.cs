using UnityEngine;

public class CharcterMove : MonoBehaviour
{
    public float floatAmplitude = 0.5f; // 얼마나 움직일지
    public float floatFrequency = 1f;   // 움직이는 속도

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.localPosition = startPos + new Vector3(0, yOffset, 0);
    }
}
