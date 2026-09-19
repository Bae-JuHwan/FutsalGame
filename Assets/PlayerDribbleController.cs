using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Rigidbody))]
[DefaultExecutionOrder(100)]
public class PlayerDribbleController : MonoBehaviour
{
    [Header("Possession")]
    [SerializeField] private float acquireRadius = 1.45f;
    [SerializeField] private float breakDistance = 1.9f;
    [SerializeField] private float maxAcquireBallSpeed = 11f;

    [Header("Dribble Touches")]
    [SerializeField] private float movingBallDistance = 0.95f;
    [SerializeField] private float standingBallDistance = 0.82f;
    [SerializeField] private float sprintBallDistance = 1.18f;
    [SerializeField] private float walkingTouchInterval = 0.12f;
    [SerializeField] private float sprintTouchInterval = 0.16f;
    [SerializeField] private float turningTouchInterval = 0.06f;
    [SerializeField] private float turnOrbitSpeed = 540f;
    [SerializeField] private float maxRelativeTouchSpeed = 5f;

    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private BallController ball;
    private bool hasPossession;
    private float releasedUntil;
    private float nextTouchTime;
    private bool wasControlling;
    private bool wasTurning;
    private Vector3 lastTouchPlayerVelocity;

    public bool HasPossession => GetComponent<FutsalPlayer>() is FutsalPlayer member ? member.HasBall : hasPossession;
    public bool CanAcquire => Time.time >= releasedUntil;

    public void ResetControl()
    {
        ReleaseControl(0f);
        releasedUntil = 0f;
        nextTouchTime = 0f;
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
    }

    public bool IsBallWithinControlHeight(BallController candidate, float maximumHeight)
    {
        // Compare contact surfaces, not centers: a smaller grounded ball must
        // remain reachable regardless of its radius or the player's height.
        float feetHeight = playerCollider != null ? playerCollider.bounds.min.y : transform.position.y;
        float ballBottom = candidate.GetComponent<SphereCollider>().bounds.min.y;
        return Mathf.Abs(ballBottom - feetHeight) <= maximumHeight;
    }

    private void Start()
    {
        FindBall();
    }

    private void FixedUpdate()
    {
        if (!playerController.enabled ||
            (FutsalGameManager.Instance != null && !FutsalGameManager.Instance.IsPlaying))
        {
            hasPossession = false;
            wasControlling = false;
            return;
        }

        if (ball == null)
        {
            FindBall();
            return;
        }

        FutsalPlayer member = GetComponent<FutsalPlayer>();
        if (member != null)
        {
            hasPossession = member.HasBall;
            if (hasPossession)
            {
                ControlBall(Time.time, Time.fixedDeltaTime);
            }
            else
            {
                wasControlling = false;
            }
            return;
        }

        if (Time.time < releasedUntil)
        {
            hasPossession = false;
            return;
        }

        Vector3 toBall = ball.transform.position - transform.position;
        Vector3 flatToBall = new Vector3(toBall.x, 0f, toBall.z);
        float distance = flatToBall.magnitude;
        float ballSpeed = new Vector3(
            ball.Rigidbody.linearVelocity.x,
            0f,
            ball.Rigidbody.linearVelocity.z).magnitude;

        if (!hasPossession)
        {
            bool ballIsReachable = distance <= acquireRadius && IsBallWithinControlHeight(ball, 0.8f);
            bool ballIsControllable = ballSpeed <= maxAcquireBallSpeed;
            if (!ballIsReachable || !ballIsControllable)
                return;

            hasPossession = true;
        }

        if (distance > breakDistance || !IsBallWithinControlHeight(ball, 1f))
        {
            hasPossession = false;
            return;
        }

        ControlBall(Time.time, Time.fixedDeltaTime);
    }

    public void ReleaseControl(float duration = 0.45f)
    {
        GetComponent<FutsalPlayer>()?.Match?.NotifyRelease(this);
        hasPossession = false;
        releasedUntil = Mathf.Max(releasedUntil, Time.time + duration);
        wasControlling = false;
        wasTurning = false;
        nextTouchTime = 0f;
    }

    private void ControlBall(float now, float deltaTime)
    {
        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.y = 0f;
        Vector3 facing = playerController.FacingDirection;
        facing.y = 0f;
        facing.Normalize();
        Vector3 input = playerController.MoveDirection;
        bool moving = input.sqrMagnitude > 0.01f;
        bool turning = moving && (Vector3.Angle(facing, input) > 15f ||
            (velocity.sqrMagnitude > 1f && Vector3.Angle(velocity, input) > 20f));

        Vector3 radial = ball.Rigidbody.position - playerRigidbody.position;
        radial.y = 0f;
        Vector3 ballVelocity = ball.Rigidbody.linearVelocity;
        Vector3 relativeVelocity = new Vector3(ballVelocity.x, 0f, ballVelocity.z) - velocity;
        float interval = turning ? turningTouchInterval
            : playerController.IsSprinting ? sprintTouchInterval : walkingTouchInterval;
        interval = Mathf.Max(0.02f, interval);

        // The ball rolls freely between contacts. A new cut or acceleration may
        // request an early touch. Released balls bypass this method entirely.
        bool movementChanged = (velocity - lastTouchPlayerVelocity).sqrMagnitude > 1.5f * 1.5f;
        bool escaping = (radial + relativeVelocity * deltaTime).magnitude > 1.4f;
        bool touchDue = !wasControlling || now >= nextTouchTime ||
            (turning && !wasTurning) || movementChanged || escaping;
        wasTurning = turning;
        wasControlling = true;
        if (!touchDue)
            return;

        float speedRatio = Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, playerController.MoveSpeed));
        float distance = !moving || turning ? standingBallDistance
            : Mathf.Lerp(movingBallDistance, sprintBallDistance,
                playerController.IsSprinting ? speedRatio : 0f);
        Vector3 radialDirection = radial.sqrMagnitude > 0.01f ? radial.normalized : facing;
        // Use short arcs instead of pulling the ball through the player's body.
        float angle = Vector3.SignedAngle(radialDirection, facing, Vector3.up);
        float step = Mathf.Clamp(angle, -Mathf.Min(40f, turnOrbitSpeed * interval),
            Mathf.Min(40f, turnOrbitSpeed * interval));
        Vector3 touchDirection = Quaternion.AngleAxis(step, Vector3.up) * radialDirection;
        Vector3 offsetCorrection = (touchDirection * distance - radial) / interval;
        offsetCorrection = Vector3.ClampMagnitude(offsetCorrection, maxRelativeTouchSpeed);
        Vector3 touchVelocity = velocity + offsetCorrection;
        touchVelocity.y = ballVelocity.y;
        ball.Rigidbody.linearVelocity = touchVelocity;
        lastTouchPlayerVelocity = velocity;
        nextTouchTime = now + interval;
    }

    private void FindBall()
    {
        ball = FindFirstObjectByType<BallController>();
        hasPossession = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = hasPossession ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, acquireRadius);

        Vector3 target = transform.position + transform.forward * movingBallDistance;
        Gizmos.DrawWireSphere(target, 0.12f);
    }
}
