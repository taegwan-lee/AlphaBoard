using UnityEngine;
using System.Collections;
public class CameraCharacter : MonoBehaviour
{
    public Transform targetLookAt;
    public float rotateDuration = 0.4f;
    public float moveCloserDistance = 3f; 
    public float returnDelay = 0.2f;

    private Quaternion originalRotation;
    private Vector3 originalPosition;

    public void PlayCameraTurnAndMove()
    {
        originalRotation = transform.rotation;
        originalPosition = transform.position;
        StartCoroutine(RotateAndMoveTowardTarget());
    }

    IEnumerator RotateAndMoveTowardTarget()
    {
        //로테이션
        Vector3 directionToTarget = (targetLookAt.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        //포지션션
        Vector3 targetPosition = transform.position + directionToTarget * moveCloserDistance;

        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, t);
            transform.position = Vector3.Lerp(originalPosition, targetPosition, t);

            yield return null;
        }

        transform.rotation = targetRotation;
        transform.position = targetPosition;
    }

    public void ReturnToOriginal()
    {
        StartCoroutine(RotateAndMoveBack());
    }

    IEnumerator RotateAndMoveBack()
    {
        yield return new WaitForSeconds(returnDelay);

        float elapsed = 0f;
        float duration = rotateDuration;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);
            transform.position = Vector3.Lerp(startPos, originalPosition, t);

            yield return null;
        }

        transform.rotation = originalRotation;
        transform.position = originalPosition;
    }

}
