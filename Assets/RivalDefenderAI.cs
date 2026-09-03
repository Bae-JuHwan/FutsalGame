using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class RivalDefenderAI : MonoBehaviour
{
    [Header("Defending")]
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float minX = -8.5f;
    [SerializeField] private float maxX = 8.5f;
    [SerializeField] private float minZ = -2f;
    [SerializeField] private float maxZ = 13.5f;
    [SerializeField] private float guardDistance = 0.8f;

    [Header("Clearance")]
    [SerializeField] private float clearanceRange = 1.25f;
    [SerializeField] private float clearancePower = 5.2f;
    [SerializeField] private float clearanceCooldown = 0.9f;

    private Rigidbody defenderRigidbody;
    private BallController ball;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float nextClearanceTime;
    private bool paused;

    private void Awake()
    {
        defenderRigidbody = GetComponent<Rigidbody>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void Start()
    {
        FindBall();
    }

    private void FixedUpdate()
    {
        if (paused)
            return;

        if (ball == null)
        {
            FindBall();
            return;
        }

        Vector3 targetPosition = ChooseTargetPosition();
        Vector3 nextPosition = Vector3.MoveTowards(
            defenderRigidbody.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime);
        defenderRigidbody.MovePosition(nextPosition);

        FaceBall(nextPosition);
        TryClearBall(nextPosition);
    }

    public void SetPaused(bool value)
    {
        paused = value;
    }

    public void ResetToHome()
    {
        defenderRigidbody.position = homePosition;
        defenderRigidbody.rotation = homeRotation;
        nextClearanceTime = Time.time + 0.5f;
    }

    private Vector3 ChooseTargetPosition()
    {
        // Stay in formation while the ball is deep in the player's half.
        if (ball.transform.position.z < minZ)
            return homePosition;

        // Stand slightly goal-side of the ball instead of blindly running past it.
        float targetX = Mathf.Clamp(ball.transform.position.x, minX, maxX);
        float targetZ = Mathf.Clamp(ball.transform.position.z + guardDistance, minZ, maxZ);
        return new Vector3(targetX, homePosition.y, targetZ);
    }

    private void FaceBall(Vector3 currentPosition)
    {
        Vector3 lookDirection = ball.transform.position - currentPosition;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            defenderRigidbody.MoveRotation(
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up));
        }
    }

    private void TryClearBall(Vector3 currentPosition)
    {
        if (Time.time < nextClearanceTime)
            return;

        Vector3 toBall = ball.transform.position - currentPosition;
        toBall.y = 0f;
        if (toBall.sqrMagnitude > clearanceRange * clearanceRange)
            return;

        Vector3 southGoal = new Vector3(0f, ball.transform.position.y, -17.5f);
        Vector3 clearanceDirection = southGoal - ball.transform.position;
        clearanceDirection.y = 0f;
        clearanceDirection = (clearanceDirection.normalized + Vector3.up * 0.08f).normalized;

        FindFirstObjectByType<PlayerDribbleController>()?.ReleaseControl(0.65f);
        ball.Rigidbody.AddForce(clearanceDirection * clearancePower, ForceMode.Impulse);
        nextClearanceTime = Time.time + clearanceCooldown;
    }

    private void FindBall()
    {
        ball = FindFirstObjectByType<BallController>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireCube(
            new Vector3((minX + maxX) * 0.5f, center.y, (minZ + maxZ) * 0.5f),
            new Vector3(maxX - minX, 0.1f, maxZ - minZ));
    }
}
