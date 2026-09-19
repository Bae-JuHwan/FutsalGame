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

    private enum MatchState { SelectingMode, Countdown, Playing, GoalCelebration, Finished }
    private MatchState state = MatchState.SelectingMode;
    private PlayerController playerTemplate;
    public int TeamSize { get; private set; } = 3;
    public bool IsSelectingMode => state == MatchState.SelectingMode;
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
    public bool IsGoalCelebration => state == MatchState.GoalCelebration;
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
        playerTemplate = FindFirstObjectByType<PlayerController>();
        if (playerTemplate == null || ball == null)
        {
            Debug.LogError("A match requires a player template and a ball in the scene.");
            enabled = false;
            return;
        }
        ApplySimulationState();
        gameObject.AddComponent<FutsalMatchUI>().Initialize(this);
    }

    public void StartMatch(int teamSize)
    {
        if (!IsSelectingMode || restarting || (teamSize != 3 && teamSize != 5))
            return;
        TeamSize = teamSize;
        Teams = gameObject.AddComponent<FutsalTeamMatch>();
        Teams.Initialize(playerTemplate, ball, TeamSize);
        BeginCountdown();
    }

    private void Update()
    {
        if (restarting || IsSelectingMode)
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
            // Keep ball/net physics running while player controls and the clock stop.
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
        goalDelayRemaining = Mathf.Max(resetDelay, 2f);
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

        Teams?.StopAll();
        ApplySimulationState();
    }

    public void PauseMatch()
    {
        if (IsSelectingMode || MatchEnded || IsPaused || restarting)
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
        Time.timeScale = !IsPaused && !restarting && (IsPlaying || IsGoalCelebration) ? 1f : 0f;
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
        if (restarting || IsSelectingMode || Teams == null)
            return;
        PlayerScore = RivalScore = 0;
        TimeRemaining = matchDuration;
        IsPaused = false;
        Teams.NextKickoffTeam = FutsalTeam.Home;
        foreach (FutsalPlayer member in Teams.Players)
            member.Motor.ResetStamina();
        Teams.ResetFormation();
        BeginCountdown();
    }

    public void ReturnToModeSelection()
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
