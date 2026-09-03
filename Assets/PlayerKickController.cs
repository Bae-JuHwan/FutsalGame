using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKickController : MonoBehaviour
{
    [Header("Kick")]
    [SerializeField] private float kickRange = 1.7f;
    [SerializeField] private float kickPower = 8.5f;
    [SerializeField] private float liftPower = 0.18f;
    [SerializeField] private float kickCooldown = 0.25f;

    private float nextKickTime;

    public void ConfigureKick(float power, float lift)
    {
        kickPower = power;
        liftPower = lift;
    }

    private void Update()
    {
        bool keyboardKick = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gamepadKick = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (keyboardKick || gamepadKick)
        {
            TryKick();
        }
    }

    // A mobile UI button can call this public method later.
    public void TryKick()
    {
        if (Time.time < nextKickTime)
            return;

        BallController ball = FindNearestBall();
        if (ball == null)
            return;

        Vector3 flatDirection = ball.transform.position - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.01f)
            flatDirection = transform.forward;
        else
            flatDirection.Normalize();

        Vector3 kickDirection = (flatDirection + Vector3.up * liftPower).normalized;
        GetComponent<PlayerDribbleController>()?.ReleaseControl(0.5f);
        ball.Rigidbody.AddForce(kickDirection * kickPower, ForceMode.Impulse);
        nextKickTime = Time.time + kickCooldown;
    }

    private BallController FindNearestBall()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            kickRange,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        BallController nearestBall = null;
        float nearestSqrDistance = float.MaxValue;

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            BallController candidate = nearbyCollider.GetComponentInParent<BallController>();
            if (candidate == null)
                continue;

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestBall = candidate;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearestBall;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, kickRange);
    }
}
