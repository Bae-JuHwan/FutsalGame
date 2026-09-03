using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FutsalGameManager : MonoBehaviour
{
    public static FutsalGameManager Instance { get; private set; }

    [Header("Match")]
    [SerializeField] private float matchDuration = 60f;
    [SerializeField] private float resetDelay = 1.1f;

    private BallController ball;
    private PlayerController player;
    private Rigidbody playerRigidbody;
    private RivalDefenderAI rivalDefender;
    private Vector3 playerSpawnPosition;
    private Quaternion playerSpawnRotation;

    private float timeRemaining;
    private int playerScore;
    private int rivalScore;
    private bool acceptingGoals = true;
    private bool matchEnded;
    private string eventMessage = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        timeRemaining = matchDuration;
    }

    private void Start()
    {
        ball = FindFirstObjectByType<BallController>();
        player = FindFirstObjectByType<PlayerController>();
        rivalDefender = FindFirstObjectByType<RivalDefenderAI>();

        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
            playerSpawnPosition = player.transform.position;
            playerSpawnRotation = player.transform.rotation;
        }
    }

    private void Update()
    {
        if (matchEnded)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                RestartMatch();
            return;
        }

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        if (timeRemaining <= 0f)
            EndMatch();
    }

    public void RegisterGoal(GoalSide defendedSide)
    {
        if (!acceptingGoals || matchEnded)
            return;

        acceptingGoals = false;
        rivalDefender?.SetPaused(true);

        if (defendedSide == GoalSide.North)
        {
            playerScore++;
            eventMessage = "GOAL!";
        }
        else
        {
            rivalScore++;
            eventMessage = "OWN GOAL";
        }

        StopMovingObjects();
        StartCoroutine(ResetAfterGoal());
    }

    private IEnumerator ResetAfterGoal()
    {
        yield return new WaitForSeconds(resetDelay);

        if (matchEnded)
            yield break;

        ResetPositions();
        eventMessage = string.Empty;
        acceptingGoals = true;
        rivalDefender?.SetPaused(false);
    }

    private void StopMovingObjects()
    {
        if (ball != null)
        {
            ball.Rigidbody.linearVelocity = Vector3.zero;
            ball.Rigidbody.angularVelocity = Vector3.zero;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        rivalDefender?.ResetToHome();
    }

    private void ResetPositions()
    {
        ball?.ResetBall();

        if (playerRigidbody != null)
        {
            playerRigidbody.position = playerSpawnPosition;
            playerRigidbody.rotation = playerSpawnRotation;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void EndMatch()
    {
        matchEnded = true;
        acceptingGoals = false;
        rivalDefender?.SetPaused(true);
        StopMovingObjects();

        if (player != null)
            player.enabled = false;

        PlayerKickController kickController = player != null
            ? player.GetComponent<PlayerKickController>()
            : null;
        if (kickController != null)
            kickController.enabled = false;

        if (playerScore > rivalScore)
            eventMessage = "YOU WIN!";
        else if (playerScore < rivalScore)
            eventMessage = "YOU LOSE";
        else
            eventMessage = "DRAW";
    }

    private void RestartMatch()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        float scale = Mathf.Clamp(Screen.width / 1280f, 0.75f, 1.4f);

        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(30f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle messageStyle = new GUIStyle(scoreStyle)
        {
            fontSize = Mathf.RoundToInt(48f * scale),
            normal = { textColor = new Color(1f, 0.82f, 0.12f) }
        };

        GUIStyle helpStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(18f * scale),
            normal = { textColor = new Color(1f, 1f, 1f, 0.85f) }
        };

        string scoreText = $"PLAYER  {playerScore}  -  {rivalScore}  RIVAL";
        string timeText = $"{Mathf.CeilToInt(timeRemaining):00}";

        GUI.Box(new Rect(Screen.width * 0.5f - 220f * scale, 14f, 440f * scale, 82f * scale), GUIContent.none);
        GUI.Label(new Rect(0f, 18f, Screen.width, 40f * scale), scoreText, scoreStyle);
        GUI.Label(new Rect(0f, 53f * scale, Screen.width, 35f * scale), timeText, scoreStyle);

        if (player != null)
        {
            float barWidth = 260f * scale;
            float barHeight = 14f * scale;
            Rect staminaBackground = new Rect(
                Screen.width * 0.5f - barWidth * 0.5f,
                101f * scale,
                barWidth,
                barHeight);
            Rect staminaFill = new Rect(
                staminaBackground.x + 2f,
                staminaBackground.y + 2f,
                (staminaBackground.width - 4f) * player.StaminaNormalized,
                staminaBackground.height - 4f);

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(staminaBackground, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(
                new Color(0.95f, 0.18f, 0.08f),
                new Color(0.2f, 0.95f, 0.35f),
                player.StaminaNormalized);
            GUI.DrawTexture(staminaFill, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        if (!string.IsNullOrEmpty(eventMessage))
        {
            GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 70f * scale), eventMessage, messageStyle);
        }

        string help = matchEnded ? "Press R to restart" : "WASD: Move    Shift: Sprint    Space: Kick";
        GUI.Label(new Rect(0f, Screen.height - 48f * scale, Screen.width, 35f * scale), help, helpStyle);
    }
}
