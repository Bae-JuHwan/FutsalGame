using UnityEngine;

public enum GoalSide
{
    North,
    South
}

[RequireComponent(typeof(BoxCollider))]
public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private GoalSide defendedSide;
    private BallController ball;
    private SphereCollider ballShape;
    private float lineEdge;
    private float mouthPlane;
    private float leftEdge;
    private float rightEdge;
    private float crossbarHeight;
    private float groundHeight;
    private Vector3 previousCenter;
    private bool tracking;
    private bool enteredThroughMouth;
    private float Direction => defendedSide == GoalSide.North ? 1f : -1f;

    public void SetDefendedSide(GoalSide side)
    {
        defendedSide = side;
    }

    private void Start()
    {
        string end = defendedSide == GoalSide.North ? "North" : "South";
        Transform goal = GameObject.Find(end + " Goal")?.transform;
        Renderer line = GameObject.Find(end + " Goal Line")?.GetComponent<Renderer>();
        if (goal == null || line == null) { enabled = false; return; }
        lineEdge = Direction > 0 ? line.bounds.max.z : -line.bounds.min.z;
        mouthPlane = goal.position.z * Direction;
        leftEdge = goal.Find("Left Post").GetComponent<Collider>().bounds.max.x;
        rightEdge = goal.Find("Right Post").GetComponent<Collider>().bounds.min.x;
        crossbarHeight = goal.Find("Crossbar").GetComponent<Collider>().bounds.min.y;
        groundHeight = goal.position.y;
        GoalNetReaction net = goal.GetComponent<GoalNetReaction>();
        if (net == null) net = goal.gameObject.AddComponent<GoalNetReaction>();
        net.ScoringTrigger = this;
        ball = FindFirstObjectByType<BallController>();
        if (ball != null) ballShape = ball.GetComponent<SphereCollider>();
        tracking = false;
        enteredThroughMouth = false;
    }

    private void FixedUpdate()
    {
        if (ball == null || ballShape == null) return;
        Vector3 center = ball.Rigidbody.position + ball.transform.TransformVector(ballShape.center);
        float radius = BallRadius();
        var match = FutsalGameManager.Instance;
        if (match == null || !match.IsPlaying)
        {
            previousCenter = center;
            tracking = false;
            enteredThroughMouth = false;
            return;
        }
        if (!tracking) enteredThroughMouth = false;
        if (tracking && CrossedGoalLine(previousCenter, center, radius))
            match.RegisterGoal(defendedSide);
        previousCenter = center;
        tracking = true;
    }

    public void CheckNetContact(BallController candidate, Vector3 point, Vector3 normal)
    {
        var match = FutsalGameManager.Instance;
        if (candidate != ball || !tracking || match == null || !match.IsPlaying) return;
        float radius = BallRadius();
        // A fast ball can cross the line and bounce off the net within a
        // single physics step. Include the contact center in its swept path.
        if (CrossedGoalLine(previousCenter, point + normal * radius, radius))
            match.RegisterGoal(defendedSide);
    }

    private float BallRadius()
    {
        Vector3 scale = ball.transform.lossyScale;
        return ballShape.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private bool CrossedGoalLine(Vector3 from, Vector3 to, float radius)
    {
        float fromDepth = from.z * Direction;
        float toDepth = to.z * Direction;
        if (toDepth + radius < mouthPlane)
            enteredThroughMouth = false;

        // Validate entry at the actual frame. Once the ball clears the frame,
        // it may move toward a side net or rise above the crossbar's underside.
        // Re-testing the frame bounds at the later full-line crossing rejects
        // those legitimate goals and cannot recover on subsequent frames.
        if (!enteredThroughMouth && fromDepth <= mouthPlane && toDepth > mouthPlane)
        {
            Vector3 entry = Vector3.Lerp(from, to, (mouthPlane - fromDepth) / (toDepth - fromDepth));
            const float contactTolerance = 0.02f; // PhysX contact margin at posts/bar.
            enteredThroughMouth = entry.x - radius >= leftEdge - contactTolerance &&
                entry.x + radius <= rightEdge + contactTolerance &&
                entry.y + radius <= crossbarHeight + contactTolerance &&
                entry.y - radius >= groundHeight - 0.03f;
        }

        // Keep this pending over multiple physics steps, including net contacts.
        // There is no scoring tolerance here: the entire ball must clear the line.
        return enteredThroughMouth && toDepth - radius > lineEdge;
    }
}
