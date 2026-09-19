using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Decide possession and AI intent before the individual movement/dribble steps.
[DefaultExecutionOrder(-100)]
public class FutsalTeamMatch : MonoBehaviour
{
    [Header("Ball Actions")]
    [SerializeField] private float passSpeed = 14.5f;
    [SerializeField] private float passLeadTime = 0.24f;
    [SerializeField] private float tackleRange = 1.75f;
    [SerializeField] private float tackleAngle = 100f;
    [SerializeField] private float tackleCooldown = 0.75f;
    [SerializeField] private float minimumShotSpeed = 16f;
    [SerializeField] private float maximumShotSpeed = 32f;
    [SerializeField] private float minimumShotLift = 0.65f;
    [SerializeField] private float maximumShotLift = 1.45f;
    [SerializeField, Range(0f, 1f)] private float shotAimAssist = 0.82f;
    [SerializeField] private float shotAssistAngle = 95f;

    [Header("Team Shape")]
    [SerializeField] private float separationRadius = 1.8f;
    [SerializeField] private float separationStrength = 1.35f;

    private readonly List<FutsalPlayer> players = new List<FutsalPlayer>();
    public IReadOnlyList<FutsalPlayer> Players => players;
    public FutsalPlayer ControlledPlayer { get; private set; }
    public FutsalPlayer Possessor { get; private set; }
    public FutsalTeam NextKickoffTeam { get; set; }
    public Material IndicatorMaterial { get; private set; }
    public Material PassIndicatorMaterial { get; private set; }
    public FutsalPlayer PassTarget { get; private set; }
    private BallController ball;
    private CameraFollow followCamera;
    private FutsalPlayer receiver;
    private float receiveUntil;
    private float possessionSince;
    private float nextSwitchTime;
    private float nextActionTime;
    private float nextTackleTime;
    private int teamSize;
    private Bounds pitchBounds = new Bounds(Vector3.zero, new Vector3(22f, 0.2f, 36f));

    public void Initialize(PlayerController template, BallController matchBall, int playersPerTeam = 3)
    {
        teamSize = playersPerTeam == 5 ? 5 : 3;
        ball = matchBall;
        Collider field = GameObject.Find("Field")?.GetComponent<Collider>();
        if (field != null) pitchBounds = field.bounds;
        ball.SetMaxHorizontalSpeed(34f);
        followCamera = FindFirstObjectByType<CameraFollow>();
        foreach (RivalDefenderAI oldDefender in FindObjectsByType<RivalDefenderAI>(FindObjectsSortMode.None))
            oldDefender.gameObject.SetActive(false);
        foreach (GoalkeeperAI oldKeeper in FindObjectsByType<GoalkeeperAI>(FindObjectsSortMode.None))
            oldKeeper.gameObject.SetActive(false);

        // Reuse the prototype's material shader so it is also included in builds.
        IndicatorMaterial = new Material(template.GetComponent<Renderer>().sharedMaterial);
        IndicatorMaterial.SetColor("_BaseColor", Color.yellow);
        IndicatorMaterial.SetColor("_Color", Color.yellow);
        PassIndicatorMaterial = new Material(template.GetComponent<Renderer>().sharedMaterial);
        PassIndicatorMaterial.SetColor("_BaseColor", Color.cyan);
        PassIndicatorMaterial.SetColor("_Color", Color.cyan);
        var objects = new GameObject[teamSize * 2];
        objects[0] = template.gameObject;
        for (int i = 1; i < objects.Length; i++)
            objects[i] = Instantiate(template.gameObject);

        Vector3[] homes = teamSize == 5
            ? new[] { new Vector3(0f, 1f, -4f), new Vector3(-5f, 1f, -7f),
                new Vector3(5f, 1f, -7f), new Vector3(0f, 1f, -11f), new Vector3(0f, 1f, -16f) }
            : new[] { new Vector3(-3f, 1f, -5f), new Vector3(4f, 1f, -6f), new Vector3(0f, 1f, -16f) };
        for (int i = 0; i < objects.Length; i++)
        {
            FutsalTeam team = i < teamSize ? FutsalTeam.Home : FutsalTeam.Away;
            int slot = i % teamSize;
            FutsalRole role = slot == teamSize - 1 ? FutsalRole.Goalkeeper : FutsalRole.FieldPlayer;
            objects[i].name = $"{team} {(role == FutsalRole.Goalkeeper ? "GK" : (slot + 1).ToString())}";
            FutsalPlayer member = objects[i].AddComponent<FutsalPlayer>();
            Vector3 home = homes[slot];
            if (team == FutsalTeam.Away) { home.x = -home.x; home.z = -home.z; }
            member.Initialize(this, team, role, home, role == FutsalRole.Goalkeeper ? 1 : slot + 7);
            players.Add(member);
        }
        ResetFormation();
    }

    private void Update()
    {
        if (!CanPlay || ControlledPlayer == null)
            return;
        if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))
            TryPass(ControlledPlayer);
        if ((Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame))
            TryTackle(ControlledPlayer);
    }

    private bool CanPlay => ball != null && FutsalGameManager.Instance != null && FutsalGameManager.Instance.IsPlaying;

    private void FixedUpdate()
    {
        if (!CanPlay)
            return;
        ResolvePossession();
        SelectControlledPlayer();
        UpdatePassTarget();
        foreach (FutsalPlayer member in players)
        {
            if (member.IsHuman)
                continue;
            Vector3 target = ChooseAITarget(member);
            Vector3 separation = CalculateSeparation(member) * separationStrength;
            if (IsChasingBall(member))
            {
                // Keep a little sideways avoidance without letting formation
                // spacing cancel the designated defender's approach to the ball.
                Vector3 approach = Flat(target - member.transform.position).normalized;
                separation = Vector3.ProjectOnPlane(separation, approach) * 0.25f;
                target = ClampToPitch(member, target + separation);
            }
            else
                target += separation;
            Vector3 delta = Flat(target - member.transform.position);
            Vector3 direction = delta.magnitude > 0.2f ? Vector3.ClampMagnitude(delta * 1.5f, 0.78f) : Vector3.zero;
            member.Motor.SetAIInput(new Vector2(direction.x, direction.z));
            if (member.HasBall && Time.time >= nextActionTime && Time.time - possessionSince > 0.4f)
            {
                if (member.Role == FutsalRole.Goalkeeper || OpponentDistance(member) < 2.3f)
                    TryPass(member);
                else if (Mathf.Abs(17.5f * member.AttackDirection - member.transform.position.z) < 10f)
                    TryShoot(member);
            }
        }
    }

    private void ResolvePossession()
    {
        if (receiver != null && Time.time > receiveUntil)
            receiver = null;
        if (Possessor != null && (Distance(Possessor) > 2.65f ||
            !Possessor.Dribble.IsBallWithinControlHeight(ball, 1f)))
            Possessor.Dribble.ReleaseControl(0.35f);

        FutsalPlayer nearest = null;
        float best = float.MaxValue;
        foreach (FutsalPlayer member in players)
        {
            float distance = Distance(member);
            if (!member.Dribble.CanAcquire || distance > 1.45f || distance >= best ||
                !member.Dribble.IsBallWithinControlHeight(ball, 0.8f))
                continue;
            if (Possessor != null)
            {
                if (member.Team == Possessor.Team || Time.time - possessionSince < 0.6f ||
                    distance > 0.85f || distance + 0.15f >= Distance(Possessor))
                    continue;
            }
            else if (member != receiver && Flat(ball.Rigidbody.linearVelocity).magnitude > 11f)
                continue;
            nearest = member;
            best = distance;
        }
        if (nearest != null)
            AssignPossession(nearest);
    }

    private void AssignPossession(FutsalPlayer member)
    {
        if (Possessor != null && Possessor != member)
            Possessor.Dribble.ReleaseControl(0.65f);
        Possessor = member;
        receiver = null;
        possessionSince = Time.time;
        if (member.Team == FutsalTeam.Home)
            SetControlledPlayer(member);
    }

    public void NotifyRelease(PlayerDribbleController dribble)
    {
        if (Possessor != null && Possessor.Dribble == dribble)
            Possessor = null;
    }

    private void SelectControlledPlayer()
    {
        if (Possessor != null && Possessor.Team == FutsalTeam.Home)
        {
            SetControlledPlayer(Possessor);
            return;
        }
        // During a home pass the receiver approaches the ball under AI control;
        // hand over only after actual possession, including interceptions.
        if (receiver != null && receiver.Team == FutsalTeam.Home)
            return;
        FutsalPlayer nearest = NearestFieldPlayer(FutsalTeam.Home);
        if (nearest != null && (ControlledPlayer == null || ControlledPlayer.Role == FutsalRole.Goalkeeper ||
            (Time.time >= nextSwitchTime && Distance(nearest) + 1.2f < Distance(ControlledPlayer))))
            SetControlledPlayer(nearest);
    }

    private void SetControlledPlayer(FutsalPlayer member)
    {
        if (ControlledPlayer == member)
            return;
        if (ControlledPlayer != null) ControlledPlayer.ShowControl(false);
        ControlledPlayer = member;
        member.ShowControl(true);
        nextSwitchTime = Time.time + 0.6f;
        followCamera?.SetMatchTargets(member.transform, ball.transform);
    }

    private FutsalPlayer NearestFieldPlayer(FutsalTeam team)
    {
        FutsalPlayer result = null;
        float best = float.MaxValue;
        foreach (FutsalPlayer member in players)
        {
            if (member.Team != team || member.Role != FutsalRole.FieldPlayer)
                continue;
            float distance = Distance(member);
            if (distance < best) { result = member; best = distance; }
        }
        return result;
    }

    private Vector3 ChooseAITarget(FutsalPlayer member)
    {
        Vector3 ballPosition = ball.transform.position;
        float attack = member.AttackDirection;
        if (member.HasBall)
            return new Vector3(Mathf.Clamp(member.transform.position.x, -2f, 2f), 1f, 16f * attack);
        if (member == receiver)
            return ClampToPitch(member, ballPosition + Flat(ball.Rigidbody.linearVelocity) * 0.12f);
        if (member.Role == FutsalRole.Goalkeeper)
            return new Vector3(Mathf.Clamp(ballPosition.x + ball.Rigidbody.linearVelocity.x * 0.18f, -2.4f, 2.4f),
                1f, member.HomePosition.z);
        if (Possessor != null && Possessor.Team == member.Team)
        {
            if (teamSize == 5)
            {
                // Keep the diamond's wings, pivot and deeper outlet distinct.
                float depth = (member.HomePosition.z * attack + 7f) * 0.9f;
                return new Vector3(Mathf.Clamp(member.HomePosition.x + ballPosition.x * 0.15f, -8f, 8f),
                    1f, Mathf.Clamp(ballPosition.z + attack * depth, -13f, 13f));
            }
            // Offer a wide passing option, keeping clear of the ball carrier.
            float lane = Possessor.transform.position.x >= 0f ? -4.5f : 4.5f;
            return new Vector3(lane, 1f, Mathf.Clamp(ballPosition.z + attack * 3f, -13f, 13f));
        }
        if (NearestFieldPlayer(member.Team) == member)
            return ClampToPitch(member, ballPosition);
        if (teamSize == 5)
            return new Vector3(Mathf.Clamp(member.HomePosition.x * 0.8f + ballPosition.x * 0.3f, -8f, 8f),
                1f, Mathf.Clamp(member.HomePosition.z + ballPosition.z * 0.4f, -13f, 13f));
        return new Vector3(Mathf.Clamp(ballPosition.x * 0.55f + Mathf.Sign(member.HomePosition.x) * 2f, -8f, 8f),
            1f, Mathf.Clamp(ballPosition.z - attack * 4f, -13f, 13f));
    }

    private float OpponentDistance(FutsalPlayer member)
    {
        float nearest = float.MaxValue;
        foreach (FutsalPlayer opponent in players)
            if (opponent.Team != member.Team)
                nearest = Mathf.Min(nearest, Flat(opponent.transform.position - member.transform.position).magnitude);
        return nearest;
    }

    private bool IsChasingBall(FutsalPlayer member)
    {
        return member == receiver || (!member.HasBall && member.Role == FutsalRole.FieldPlayer &&
            (Possessor == null || Possessor.Team != member.Team) && NearestFieldPlayer(member.Team) == member);
    }

    private Vector3 ClampToPitch(FutsalPlayer member, Vector3 target)
    {
        // The center can approach the wall until the player's capsule, rather
        // than an arbitrary tactical boundary, runs out of room.
        Vector3 clearance = member.GetComponent<Collider>().bounds.extents + Vector3.one * 0.04f;
        return new Vector3(Mathf.Clamp(target.x, pitchBounds.min.x + clearance.x, pitchBounds.max.x - clearance.x),
            member.transform.position.y,
            Mathf.Clamp(target.z, pitchBounds.min.z + clearance.z, pitchBounds.max.z - clearance.z));
    }

    private Vector3 CalculateSeparation(FutsalPlayer member)
    {
        Vector3 separation = Vector3.zero;
        foreach (FutsalPlayer other in players)
        {
            if (other == member)
                continue;
            Vector3 away = Flat(member.transform.position - other.transform.position);
            float distance = away.magnitude;
            if (distance > 0.01f && distance < separationRadius)
                separation += away.normalized * (1f - distance / separationRadius);
        }
        return separation;
    }

    public void TryPass(FutsalPlayer passer)
    {
        if (!CanPlay || passer == null || Possessor != passer || Time.time < nextActionTime)
            return;
        FutsalPlayer target = FindPassTarget(passer);
        if (target == null)
            return;
        passer.GetComponent<PlayerKickController>()?.CancelCharge();
        passer.GetComponent<FutsalPlayerVisual>()?.PlayKick(0.45f);
        Vector3 destination = target.transform.position + Flat(target.Body.linearVelocity) * passLeadTime;
        Vector3 direction = Flat(destination - ball.transform.position).normalized;
        passer.Dribble.ReleaseControl(0.7f);
        receiver = target;
        receiveUntil = Time.time + 2.5f;
        ball.Rigidbody.linearVelocity = direction * passSpeed;
        ball.Rigidbody.angularVelocity = Vector3.zero;
        nextActionTime = Time.time + 0.3f;
    }

    private FutsalPlayer FindPassTarget(FutsalPlayer passer)
    {
        if (passer == null)
            return null;
        FutsalPlayer target = null;
        float best = float.MaxValue;
        Vector3 aimDirection = Flat(passer.Motor.MoveDirection);
        if (aimDirection.sqrMagnitude < 0.01f)
            aimDirection = Flat(passer.transform.forward);
        aimDirection.Normalize();
        foreach (FutsalPlayer teammate in players)
        {
            if (teammate == passer || teammate.Team != passer.Team)
                continue;
            Vector3 toTeammate = Flat(teammate.transform.position - passer.transform.position);
            float distance = toTeammate.magnitude;
            float angle = Vector3.Angle(aimDirection, toTeammate);
            float score = angle * 0.08f + distance * 0.05f;
            if (score < best) { best = score; target = teammate; }
        }
        return target;
    }

    private void UpdatePassTarget()
    {
        FutsalPlayer nextTarget = Possessor != null && Possessor == ControlledPlayer
            ? FindPassTarget(ControlledPlayer)
            : null;
        if (PassTarget == nextTarget)
            return;
        if (PassTarget != null) PassTarget.ShowPassTarget(false);
        PassTarget = nextTarget;
        if (PassTarget != null) PassTarget.ShowPassTarget(true);
    }

    public void TryTackle(FutsalPlayer defender)
    {
        if (!CanPlay || defender == null || defender.Team != FutsalTeam.Home ||
            Possessor == defender || Time.time < nextTackleTime)
            return;

        nextTackleTime = Time.time + tackleCooldown;
        Vector3 toBall = Flat(ball.transform.position - defender.transform.position);
        if (toBall.magnitude > tackleRange)
            return;

        Vector3 facing = Flat(defender.transform.forward).normalized;
        if (toBall.sqrMagnitude > 0.01f && Vector3.Angle(facing, toBall) > tackleAngle * 0.5f)
            return;

        if (Possessor != null && Possessor.Team == defender.Team)
            return;

        FutsalPlayer dispossessed = Possessor;
        if (dispossessed != null)
            dispossessed.Dribble.ReleaseControl(0.9f);

        ball.Rigidbody.linearVelocity *= 0.2f;
        ball.Rigidbody.angularVelocity *= 0.2f;
        AssignPossession(defender);
    }

    public void TryShoot(FutsalPlayer shooter, float charge = 0.65f)
    {
        if (!CanPlay || shooter == null || Possessor != shooter || Time.time < nextActionTime)
            return;
        Vector3 inputDirection = Flat(shooter.Motor.MoveDirection);
        bool hasInputDirection = inputDirection.sqrMagnitude >= 0.01f;
        if (!hasInputDirection)
            inputDirection = Flat(shooter.transform.forward);
        inputDirection.Normalize();

        float aimedGoalX = hasInputDirection ? Mathf.Clamp(inputDirection.x * 2.25f, -2.25f, 2.25f) : 0f;
        Vector3 goalTarget = new Vector3(aimedGoalX, ball.transform.position.y, shooter.AttackDirection * 17.7f);
        Vector3 goalDirection = Flat(goalTarget - ball.transform.position).normalized;
        Vector3 direction;
        if (!shooter.IsHuman || !hasInputDirection)
        {
            direction = goalDirection;
        }
        else
        {
            float angleToGoal = Vector3.Angle(inputDirection, goalDirection);
            float assist = angleToGoal < shotAssistAngle
                ? shotAimAssist * Mathf.Lerp(0.25f, 1f, 1f - angleToGoal / shotAssistAngle)
                : 0f;
            direction = Vector3.Slerp(inputDirection, goalDirection, assist).normalized;
        }
        shooter.Dribble.ReleaseControl(0.7f);
        shooter.GetComponent<FutsalPlayerVisual>()?.PlayKick(charge);
        receiver = null;
        float shotSpeed = Mathf.Lerp(minimumShotSpeed, maximumShotSpeed, Mathf.Clamp01(charge));
        float shotLift = Mathf.Lerp(minimumShotLift, maximumShotLift, Mathf.Clamp01(charge));
        ball.Rigidbody.linearVelocity = Flat(direction).normalized * shotSpeed + Vector3.up * shotLift;
        nextActionTime = Time.time + 0.35f;
    }

    public void StopAll()
    {
        Possessor = null;
        receiver = null;
        if (PassTarget != null) PassTarget.ShowPassTarget(false);
        PassTarget = null;
        foreach (FutsalPlayer member in players)
        {
            member.Dribble.ReleaseControl(0f);
            member.StopMotion();
        }
    }

    public void ResetFormation()
    {
        StopAll();
        foreach (FutsalPlayer member in players)
            member.ResetToHome();
        ball.Rigidbody.position = new Vector3(0f, 0.17f, 0f);
        ball.Rigidbody.linearVelocity = Vector3.zero;
        ball.Rigidbody.angularVelocity = Vector3.zero;
        FutsalPlayer kickoff = players[NextKickoffTeam == FutsalTeam.Home ? 0 : teamSize];
        kickoff.Body.position = new Vector3(0f, 1f, -kickoff.AttackDirection * 0.95f);
        nextActionTime = Time.time + 0.25f;
        nextTackleTime = 0f;
        AssignPossession(kickoff);
        if (NextKickoffTeam == FutsalTeam.Away)
            SetControlledPlayer(players[0]);
    }

    private float Distance(FutsalPlayer member) => Flat(member.transform.position - ball.transform.position).magnitude;
    private static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0f, value.z);

    private void OnDestroy()
    {
        if (IndicatorMaterial != null) Destroy(IndicatorMaterial);
        if (PassIndicatorMaterial != null) Destroy(PassIndicatorMaterial);
    }
}
