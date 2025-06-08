using UnityEngine;
using TMPro;
using System.Collections;

public class TitleFade : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public float fadeDuration = 2f;

    void Start()
    {
        StartCoroutine(FadeInText());
    }

    IEnumerator FadeInText()
    {
        Color color = titleText.color;
        float timer = 0f;

        color.a = 0f;
        titleText.color = color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Clamp01(timer / fadeDuration);
            titleText.color = color;
            yield return null;
        }

    }
}
