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

    public void SetDefendedSide(GoalSide side)
    {
        defendedSide = side;
    }

    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponentInParent<BallController>();
        if (ball != null)
        {
            FutsalGameManager.Instance?.RegisterGoal(defendedSide);
        }
    }
}
