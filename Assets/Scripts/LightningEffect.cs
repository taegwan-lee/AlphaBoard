using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LightningEffect : MonoBehaviour
{
    public GameObject Lightning;    
    public Image FlashPanel;    

    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 10f;
    public int shakeCount = 6;
    public int flashCount = 2;
    public float flashInterval = 0.1f;

    public void PlayEffect()
    {
        StartCoroutine(PlayObjectionSequence());
    }

    IEnumerator PlayObjectionSequence()
    {
        Lightning.SetActive(true);
        Vector3 originalPos = Lightning.transform.localPosition;

        
        Coroutine flash = StartCoroutine(FlashAllWhite());
        Coroutine shake = StartCoroutine(ShakeLightning(originalPos));

        yield return flash;
        yield return shake;

        Lightning.transform.localPosition = originalPos;
        Lightning.SetActive(false);
    }

    IEnumerator FlashAllWhite()
    {
        for (int i = 0; i < flashCount; i++)
        {
            yield return FlashWhite();
            yield return new WaitForSeconds(flashInterval);
        }
    }

    IEnumerator FlashWhite()
    {
        FlashPanel.color = new Color(1f, 1f, 1f, 1f);  
        FlashPanel.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.05f);

        FlashPanel.color = new Color(1f, 1f, 1f, 0f); 
        FlashPanel.gameObject.SetActive(false);
    }

    IEnumerator ShakeLightning(Vector3 originalPos)
    {
        for (int i = 0; i < shakeCount; i++)
        {
            float offsetX = (i % 2 == 0 ? 1 : -1) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * (shakeMagnitude * 0.5f);
            Lightning.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            yield return new WaitForSeconds(shakeDuration / shakeCount);
        }
    }
}
