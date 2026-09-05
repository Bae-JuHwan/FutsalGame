using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKickController : MonoBehaviour
{
    [Header("Kick")]
    [SerializeField] private float kickRange = 1.7f;
    [SerializeField] private float kickPower = 8.5f;
    [SerializeField] private float liftPower = 0.18f;
    [SerializeField] private float kickCooldown = 0.25f;
    [SerializeField] private float fullChargeTime = 0.9f;

    private float nextKickTime;
    private float chargeStartedAt;
    private bool charging;

    public bool IsCharging => charging;
    public float ChargeNormalized => charging
        ? Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeTime)
        : 0f;

    public void ConfigureKick(float power, float lift)
    {
        kickPower = power;
        liftPower = lift;
    }

    private void Update()
    {
        FutsalPlayer member = GetComponent<FutsalPlayer>();
        if (member != null && (!member.IsHuman || !member.HasBall))
        {
            CancelCharge();
            return;
        }
        if (FutsalGameManager.Instance != null && !FutsalGameManager.Instance.IsPlaying)
        {
            CancelCharge();
            return;
        }

        bool kickPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                           (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        bool kickReleased = (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame) ||
                            (Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame);

        if (member == null)
        {
            if (kickPressed) TryKick();
            return;
        }

        if (kickPressed && Time.time >= nextKickTime)
        {
            charging = true;
            chargeStartedAt = Time.time;
        }

        if (kickReleased && charging)
        {
            float power = ChargeNormalized;
            charging = false;
            nextKickTime = Time.time + kickCooldown;
            member.Match.TryShoot(member, power);
        }
    }

    public void CancelCharge() => charging = false;

    // A mobile UI button can call this public method later.
    public void TryKick()
    {
        if (FutsalGameManager.Instance != null && !FutsalGameManager.Instance.IsPlaying)
            return;

        FutsalPlayer member = GetComponent<FutsalPlayer>();
        if (member != null)
        {
            if (member.IsHuman) member.Match.TryShoot(member, 0.25f);
            return;
        }

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
