using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform ballTarget;
    [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -12f);
    [SerializeField] private float positionSmoothTime = 0.32f;
    [SerializeField] private float lookSmoothness = 9f;
    [SerializeField, Range(0f, 1f)] private float ballFocusWeight = 0.72f;
    [SerializeField] private float lookHeight = 1f;

    private Vector3 positionVelocity;
    private Vector3 smoothedFocus;
    private Vector3 focusVelocity;
    private bool focusInitialized;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetMatchTargets(Transform controlledPlayer, Transform ball)
    {
        target = controlledPlayer;
        ballTarget = ball;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredFocus = ballTarget != null
            ? Vector3.Lerp(target.position, ballTarget.position, ballFocusWeight)
            : target.position;

        if (!focusInitialized)
        {
            smoothedFocus = desiredFocus;
            focusInitialized = true;
        }

        smoothedFocus = Vector3.SmoothDamp(
            smoothedFocus,
            desiredFocus,
            ref focusVelocity,
            positionSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);

        Vector3 desiredPosition = smoothedFocus + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);

        Vector3 lookDirection = smoothedFocus + Vector3.up * lookHeight - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float blend = 1f - Mathf.Exp(-lookSmoothness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }
    }
}
