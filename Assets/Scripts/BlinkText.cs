using UnityEngine;
using TMPro;

public class BlinkText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float blinkSpeed = 2f;

    void Update()
    {
        Color color = text.color;
        color.a = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        text.color = color;
    }
}
