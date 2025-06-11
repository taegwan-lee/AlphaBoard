using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class ClickToStart : MonoBehaviour
{
    public string nextScene = "the last revelation";
    public AudioSource clickSound;
    public RectTransform titleTransform;
    public float transitionDelay = 3.0f;
    public CanvasGroup titlePanel; // 검은 페이드 패널 (처음에는 alpha 0)

    private bool isClicked = false;

    void Update()
    {
        if (!isClicked && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            isClicked = true;
            StartCoroutine(StartTransition());
        }
    }

    IEnumerator StartTransition()
    {
        // 1. 효과음 재생
        if (clickSound != null)
            clickSound.Play();

        // 2. 타이틀 일렁임 (0.2 * 2 = 0.4초 애니메이션)
        if (titleTransform != null)
        {
            yield return titleTransform
                .DOScale(1.1f, 0.2f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion(); // 기다려야 다음 단계로 넘어감
        }

        // 3. 페이드 인 (검은 화면)
        if (titlePanel != null)
        {
            yield return titlePanel
                .DOFade(1f, 2f)
                .WaitForCompletion(); // 페이드 인도 완료될 때까지 기다림
        }

        // 4. 씬 전환
        yield return new WaitForSeconds(0.2f); // 여유 조금 줌 (선택)
        SceneManager.LoadScene(nextScene);
    }
}