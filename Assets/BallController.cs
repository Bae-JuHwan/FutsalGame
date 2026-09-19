using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class BallController : MonoBehaviour
{
    [SerializeField] private float resetHeight = -2f;
    [SerializeField] private float maxHorizontalSpeed = 34f;
    [SerializeField] private float maxVerticalSpeed = 4f;

    private Rigidbody ballRigidbody;
    private Vector3 spawnPosition;

    public Rigidbody Rigidbody => ballRigidbody;

    public void SetMaxHorizontalSpeed(float speed)
    {
        maxHorizontalSpeed = Mathf.Max(0f, speed);
    }

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
    }

    private void FixedUpdate()
    {
        LimitSpeed();

        if (transform.position.y < resetHeight)
        {
            ResetBall();
        }
    }

    private void LimitSpeed()
    {
        Vector3 velocity = ballRigidbody.linearVelocity;
        Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);

        if (horizontalVelocity.magnitude > maxHorizontalSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.y;
        }

        velocity.y = Mathf.Clamp(velocity.y, -maxVerticalSpeed, maxVerticalSpeed);
        ballRigidbody.linearVelocity = velocity;
    }

    public void ResetBall()
    {
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.position = spawnPosition;
        ballRigidbody.rotation = Quaternion.identity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GoalNetReaction net = collision.collider.GetComponentInParent<GoalNetReaction>();
        if (net == null || !collision.collider.name.StartsWith("Net ") || collision.contactCount == 0)
            return;
        ContactPoint contact = collision.GetContact(0);
        net.ScoringTrigger?.CheckNetContact(this, contact.point, contact.normal);
        net.React(contact.point, -contact.normal * collision.relativeVelocity.magnitude);
        // The net absorbs the shot rather than bouncing it like a rigid wall.
        ballRigidbody.linearVelocity *= 0.2f;
        ballRigidbody.angularVelocity *= 0.4f;
    }
}
