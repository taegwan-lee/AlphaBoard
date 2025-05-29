using UnityEngine;
using System.Collections;

public class CharactorCamera : MonoBehaviour
{
    public Transform target;             // 이동할 캐릭터의 Transform
    public Vector3 offset = new Vector3(0, 1.5f, -2f); // 캐릭터 기준 카메라 위치
    public float moveTime = 1.0f;       
    public AnimationCurve moveCurve;     

    private bool isMoving = false;

    public void MoveCameraToTarget()
    {
        if (!isMoving)
            StartCoroutine(MoveToPosition());
    }

    IEnumerator MoveToPosition()
    {
        isMoving = true;

        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        Vector3 endPos = target.position + target.TransformDirection(offset);
        Quaternion endRot = Quaternion.LookRotation(target.position - endPos);

        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            float t = elapsed / moveTime;
            if (moveCurve != null)
                t = moveCurve.Evaluate(t);

            Camera.main.transform.position = Vector3.Lerp(startPos, endPos, t);
            Camera.main.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = endPos;
        Camera.main.transform.rotation = endRot;
        isMoving = false;
    }
}
