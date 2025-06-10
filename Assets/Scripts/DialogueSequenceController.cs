using System;
using System.Collections;
using UnityEngine;

public class DialogueSequenceController : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public CameraCharacter cameraCharacter;
    public LightningEffect lightningEffect;

    public void PlayModelChangeSequence(DialogueSequence dialogue)
    {
        StartCoroutine(Sequence(dialogue));
    }

    IEnumerator Sequence(DialogueSequence dialogue)
    {
        // 1. 이펙트
        lightningEffect.PlayEffect();
        yield return new WaitForSeconds(0.5f);

        // 2. 카메라 이동
        cameraCharacter.PlayCameraTurnAndMove();
        yield return new WaitForSeconds(0.6f); // 카메라 도착 대기

        // 3. 대사 출력 (코루틴으로 대기)
        bool finished = false;
        dialogueManager.StartDialogue(dialogue);

        yield return new WaitUntil(() => finished);

        // 4. 카메라 복귀
        cameraCharacter.ReturnToOriginal();
    }
}