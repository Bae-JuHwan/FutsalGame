using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class GoalkeeperAI : MonoBehaviour
{
    [Header("Goalkeeping")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float horizontalRange = 2.45f;
    [SerializeField] private float predictionTime = 0.18f;

    private Rigidbody goalkeeperRigidbody;
    private BallController ball;
    private Vector3 homePosition;
    private Quaternion homeRotation;

    private void Awake()
    {
        goalkeeperRigidbody = GetComponent<Rigidbody>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void Start()
    {
        FindBall();
    }

    private void FixedUpdate()
    {
        if (ball == null)
        {
            FindBall();
            return;
        }

        float predictedBallX = ball.transform.position.x +
                               ball.Rigidbody.linearVelocity.x * predictionTime;
        float targetX = Mathf.Clamp(
            predictedBallX,
            homePosition.x - horizontalRange,
            homePosition.x + horizontalRange);

        Vector3 currentPosition = goalkeeperRigidbody.position;
        Vector3 targetPosition = new Vector3(targetX, homePosition.y, homePosition.z);
        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime);
        goalkeeperRigidbody.MovePosition(nextPosition);

        Vector3 lookDirection = ball.transform.position - currentPosition;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            goalkeeperRigidbody.MoveRotation(
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up));
        }
    }

    public void ResetToHome()
    {
        goalkeeperRigidbody.position = homePosition;
        goalkeeperRigidbody.rotation = homeRotation;
    }

    private void FindBall()
    {
        ball = FindFirstObjectByType<BallController>();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            center + Vector3.left * horizontalRange,
            center + Vector3.right * horizontalRange);
    }
}
