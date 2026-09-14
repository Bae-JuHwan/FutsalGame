using UnityEngine;

public enum FutsalTeam { Home, Away }
public enum FutsalRole { FieldPlayer, Goalkeeper }

public class FutsalPlayer : MonoBehaviour
{
    public FutsalTeam Team { get; private set; }
    public FutsalRole Role { get; private set; }
    public FutsalTeamMatch Match { get; private set; }
    public PlayerController Motor { get; private set; }
    public PlayerDribbleController Dribble { get; private set; }
    public Rigidbody Body { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public bool IsHuman => Match != null && Match.ControlledPlayer == this;
    public bool HasBall => Match != null && Match.Possessor == this;
    public float AttackDirection => Team == FutsalTeam.Home ? 1f : -1f;
    private GameObject indicator;
    private GameObject passIndicator;

    public void Initialize(FutsalTeamMatch match, FutsalTeam team, FutsalRole role, Vector3 home, int shirtNumber = 7)
    {
        Match = match;
        Team = team;
        Role = role;
        HomePosition = home;
        Motor = GetComponent<PlayerController>();
        Dribble = GetComponent<PlayerDribbleController>();
        Body = GetComponent<Rigidbody>();
        Body.isKinematic = false;
        Color color = team == FutsalTeam.Home ? new Color(0.08f, 0.5f, 1f) : new Color(0.95f, 0.15f, 0.12f);
        if (role == FutsalRole.Goalkeeper)
            color = team == FutsalTeam.Home ? new Color(0.1f, 0.85f, 0.7f) : new Color(1f, 0.65f, 0.1f);
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        GetComponent<Renderer>().SetPropertyBlock(properties);
        gameObject.AddComponent<FutsalPlayerVisual>().Initialize(GetComponent<Renderer>().sharedMaterial, team, role, shirtNumber);

        indicator = new GameObject("Controlled Player Ring", typeof(LineRenderer));
        indicator.transform.SetParent(transform, false);
        LineRenderer line = indicator.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.09f;
        line.sharedMaterial = match.IndicatorMaterial;
        line.positionCount = 40;
        for (int i = 0; i < 40; i++)
        {
            float angle = i * Mathf.PI * 2f / 40;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.72f, -0.94f, Mathf.Sin(angle) * 0.72f));
        }
        indicator.SetActive(false);

        passIndicator = new GameObject("Pass Target Ring", typeof(LineRenderer));
        passIndicator.transform.SetParent(transform, false);
        LineRenderer passLine = passIndicator.GetComponent<LineRenderer>();
        passLine.useWorldSpace = false;
        passLine.loop = true;
        passLine.widthMultiplier = 0.075f;
        passLine.sharedMaterial = match.PassIndicatorMaterial;
        passLine.positionCount = 40;
        for (int i = 0; i < 40; i++)
        {
            float angle = i * Mathf.PI * 2f / 40;
            passLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.58f, -0.93f, Mathf.Sin(angle) * 0.58f));
        }
        passIndicator.SetActive(false);
        ResetToHome();
    }

    public void ShowControl(bool value)
    {
        indicator.SetActive(value);
        Motor.ClearInput();
        if (!value)
            GetComponent<PlayerKickController>()?.CancelCharge();
    }

    public void ShowPassTarget(bool value) => passIndicator.SetActive(value);

    public void ResetToHome()
    {
        Body.position = HomePosition;
        Body.rotation = Quaternion.LookRotation(Vector3.forward * AttackDirection);
        StopMotion();
        Dribble.ResetControl();
    }

    public void StopMotion()
    {
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Motor.ClearInput();
        GetComponent<PlayerKickController>()?.CancelCharge();
        GetComponent<FutsalPlayerVisual>()?.ResetPose();
    }
}
