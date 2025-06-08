using UnityEngine;

public class CharcterMove : MonoBehaviour
{
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 1f;

    private Vector3 startPos;
    private float randomOffset;

    void Start()
    {
        startPos = transform.localPosition;
        randomOffset = Random.Range(0f, Mathf.PI * 2f); // 0~2PI 랜덤 오프셋
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatFrequency + randomOffset) * floatAmplitude;
        transform.localPosition = startPos + new Vector3(0, yOffset, 0);
    }
}
