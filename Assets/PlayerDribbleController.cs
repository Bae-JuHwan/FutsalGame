using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(Rigidbody))]
public class PlayerDribbleController : MonoBehaviour
{
    [Header("Possession")]
    [SerializeField] private float acquireRadius = 1.45f;
    [SerializeField] private float breakDistance = 1.9f;
    [SerializeField] private float maxAcquireBallSpeed = 11f;

    [Header("Ball Control")]
    [SerializeField] private float movingBallDistance = 0.95f;
    [SerializeField] private float standingBallDistance = 0.82f;
    [SerializeField] private float controlStrength = 24f;
    [SerializeField] private float velocityDamping = 7f;
    [SerializeField] private float maxControlAcceleration = 34f;
    [SerializeField] private float forwardCarrySpeed = 0.7f;

    [Header("Sharp Turn Assist")]
    [SerializeField] private float sharpTurnAngle = 55f;
    [SerializeField] private float turnAssistDuration = 0.32f;
    [SerializeField] private float turnBreakDistance = 2.65f;
    [SerializeField] private float turnBallDistance = 0.72f;
    [SerializeField] private float turnOrbitSpeed = 620f;
    [SerializeField] private float turnControlMultiplier = 1.65f;

    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private BallController ball;
    private bool hasPossession;
    private float releasedUntil;
    private float turnAssistUntil;
    private Vector3 previousMoveDirection;

    public bool HasPossession => GetComponent<FutsalPlayer>() is FutsalPlayer member ? member.HasBall : hasPossession;
    public bool CanAcquire => Time.time >= releasedUntil;

    public void ResetControl()
    {
        ReleaseControl(0f);
        releasedUntil = 0f;
        previousMoveDirection = Vector3.zero;
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        FindBall();
    }

    private void FixedUpdate()
    {
        if (!playerController.enabled)
        {
            hasPossession = false;
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
                UpdateTurnAssist();
                ControlBall();
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
            bool ballIsReachable = distance <= acquireRadius && Mathf.Abs(toBall.y) < 0.8f;
            bool ballIsControllable = ballSpeed <= maxAcquireBallSpeed;
            if (!ballIsReachable || !ballIsControllable)
                return;

            hasPossession = true;
        }

        UpdateTurnAssist();
        float activeBreakDistance = IsTurnAssistActive ? turnBreakDistance : breakDistance;

        if (distance > activeBreakDistance || Mathf.Abs(toBall.y) > 1f)
        {
            hasPossession = false;
            return;
        }

        ControlBall();
    }

    public void ReleaseControl(float duration = 0.45f)
    {
        GetComponent<FutsalPlayer>()?.Match?.NotifyRelease(this);
        hasPossession = false;
        releasedUntil = Mathf.Max(releasedUntil, Time.time + duration);
        turnAssistUntil = 0f;
    }

    private bool IsTurnAssistActive => Time.time < turnAssistUntil;

    private void UpdateTurnAssist()
    {
        Vector3 moveDirection = playerController.MoveDirection;
        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            previousMoveDirection = Vector3.zero;
            return;
        }

        moveDirection.Normalize();
        Vector3 facingDirection = transform.forward;
        facingDirection.y = 0f;
        facingDirection.Normalize();

        float inputTurnAngle = previousMoveDirection.sqrMagnitude > 0.01f
            ? Vector3.Angle(previousMoveDirection, moveDirection)
            : 0f;
        float facingTurnAngle = Vector3.Angle(facingDirection, moveDirection);

        if (inputTurnAngle >= sharpTurnAngle || facingTurnAngle >= sharpTurnAngle)
            turnAssistUntil = Time.time + turnAssistDuration;

        previousMoveDirection = moveDirection;
    }

    private void ControlBall()
    {
        Vector3 moveDirection = playerController.MoveDirection;
        moveDirection.y = 0f;
        bool isMoving = moveDirection.sqrMagnitude > 0.01f;

        // Using the smoothed facing direction makes sharp turns curve instead of snapping the ball.
        Vector3 facingDirection = transform.forward;
        facingDirection.y = 0f;
        facingDirection.Normalize();

        Vector3 targetDirection = facingDirection;
        float targetDistance = isMoving ? movingBallDistance : standingBallDistance;

        if (IsTurnAssistActive && isMoving)
        {
            moveDirection.Normalize();
            Vector3 radialBallDirection = ball.transform.position - playerRigidbody.position;
            radialBallDirection.y = 0f;
            if (radialBallDirection.sqrMagnitude < 0.01f)
                radialBallDirection = facingDirection;
            else
                radialBallDirection.Normalize();

            float signedAngle = Vector3.SignedAngle(
                radialBallDirection,
                moveDirection,
                Vector3.up);
            float orbitStep = Mathf.Clamp(
                signedAngle,
                -turnOrbitSpeed * Time.fixedDeltaTime,
                turnOrbitSpeed * Time.fixedDeltaTime);
            targetDirection = Quaternion.AngleAxis(orbitStep, Vector3.up) * radialBallDirection;
            targetDistance = turnBallDistance;
        }

        Vector3 targetPosition = playerRigidbody.position + targetDirection * targetDistance;
        targetPosition.y = ball.transform.position.y;

        Vector3 playerVelocity = playerRigidbody.linearVelocity;
        Vector3 targetVelocity = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
        if (isMoving)
            targetVelocity += targetDirection * forwardCarrySpeed;

        Vector3 ballVelocity = ball.Rigidbody.linearVelocity;
        Vector3 horizontalBallVelocity = new Vector3(ballVelocity.x, 0f, ballVelocity.z);
        Vector3 positionError = targetPosition - ball.transform.position;
        positionError.y = 0f;

        float activeControlStrength = IsTurnAssistActive
            ? controlStrength * turnControlMultiplier
            : controlStrength;
        float activeMaxAcceleration = IsTurnAssistActive
            ? maxControlAcceleration * turnControlMultiplier
            : maxControlAcceleration;

        Vector3 controlAcceleration = positionError * activeControlStrength +
                                      (targetVelocity - horizontalBallVelocity) * velocityDamping;
        controlAcceleration = Vector3.ClampMagnitude(controlAcceleration, activeMaxAcceleration);

        ball.Rigidbody.AddForce(controlAcceleration, ForceMode.Acceleration);
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
