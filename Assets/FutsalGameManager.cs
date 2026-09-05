using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FutsalGameManager : MonoBehaviour
{
    public static FutsalGameManager Instance { get; private set; }

    [Header("Match")]
    [SerializeField] private float matchDuration = 60f;
    [SerializeField] private float resetDelay = 1.1f;
    [SerializeField] private float kickoffCountdown = 3f;

    private enum MatchState { Countdown, Playing, GoalCelebration, Finished }
    private MatchState state = MatchState.Countdown;
    private BallController ball;
    public FutsalTeamMatch Teams { get; private set; }
    private float goalDelayRemaining;
    private float countdownRemaining;
    private float previousTimeScale;
    private bool restarting;

    public float TimeRemaining { get; private set; }
    public int PlayerScore { get; private set; }
    public int RivalScore { get; private set; }
    public bool IsPaused { get; private set; }
    public bool MatchEnded => state == MatchState.Finished;
    public bool IsPlaying => state == MatchState.Playing && !IsPaused && !restarting;
    public float StaminaNormalized => Teams != null && Teams.ControlledPlayer != null
        ? Teams.ControlledPlayer.Motor.StaminaNormalized : 0f;
    public string EventMessage { get; private set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        TimeRemaining = matchDuration;
    }

    private void Start()
    {
        ball = FindFirstObjectByType<BallController>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null || ball == null)
        {
            Debug.LogError("3v3 requires a player template and a ball in the scene.");
            enabled = false;
            return;
        }
        Teams = gameObject.AddComponent<FutsalTeamMatch>();
        Teams.Initialize(player, ball);

        gameObject.AddComponent<FutsalMatchUI>().Initialize(this);
        BeginCountdown();
    }

    private void Update()
    {
        if (restarting)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused) ResumeMatch();
            else PauseMatch();
        }

        if (MatchEnded)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                RestartMatch();
            return;
        }

        if (IsPaused)
            return;

        if (state == MatchState.Countdown)
        {
            countdownRemaining -= Time.unscaledDeltaTime;
            if (countdownRemaining <= 0f)
            {
                EventMessage = string.Empty;
                state = MatchState.Playing;
                ApplySimulationState();
            }
            else
            {
                EventMessage = Mathf.CeilToInt(countdownRemaining).ToString();
            }
            return;
        }

        if (state == MatchState.GoalCelebration)
        {
            // Physics is frozen, but the celebration still needs to finish.
            goalDelayRemaining -= Time.unscaledDeltaTime;
            if (goalDelayRemaining <= 0f)
            {
                ResetPositions();
                BeginCountdown();
            }
            return;
        }

        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        if (TimeRemaining <= 0f)
            EndMatch();
    }

    public void RegisterGoal(GoalSide defendedSide)
    {
        if (!IsPlaying)
            return;

        state = MatchState.GoalCelebration;
        Teams.NextKickoffTeam = defendedSide == GoalSide.South ? FutsalTeam.Home : FutsalTeam.Away;
        goalDelayRemaining = resetDelay;
        if (defendedSide == GoalSide.North)
        {
            PlayerScore++;
            EventMessage = "GOAL!";
        }
        else
        {
            RivalScore++;
            EventMessage = "RIVAL GOAL";
        }

        StopMovingObjects();
        ApplySimulationState();
    }

    public void PauseMatch()
    {
        if (MatchEnded || IsPaused || restarting)
            return;

        IsPaused = true;
        ApplySimulationState();
    }

    public void ResumeMatch()
    {
        if (!IsPaused || MatchEnded || restarting)
            return;

        IsPaused = false;
        ApplySimulationState();
    }

    private void ApplySimulationState()
    {
        Time.timeScale = IsPlaying ? 1f : 0f;
    }

    private void BeginCountdown()
    {
        state = MatchState.Countdown;
        countdownRemaining = kickoffCountdown;
        EventMessage = Mathf.CeilToInt(countdownRemaining).ToString();
        ApplySimulationState();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PauseMatch();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) PauseMatch();
    }

    private void StopMovingObjects()
    {
        Teams?.StopAll();
        if (ball != null)
        {
            ball.Rigidbody.linearVelocity = Vector3.zero;
            ball.Rigidbody.angularVelocity = Vector3.zero;
        }

    }

    private void ResetPositions()
    {
        Teams?.ResetFormation();
    }

    private void EndMatch()
    {
        state = MatchState.Finished;
        StopMovingObjects();
        ApplySimulationState();
        EventMessage = PlayerScore > RivalScore ? "YOU WIN!"
            : PlayerScore < RivalScore ? "YOU LOSE" : "DRAW";
    }

    public void RestartMatch()
    {
        if (restarting)
            return;

        restarting = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Time.timeScale = previousTimeScale;
        Instance = null;
    }
}
