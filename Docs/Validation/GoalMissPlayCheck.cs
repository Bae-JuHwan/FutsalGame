// Copy to Assets in the isolated validation project, never the game project.
using System;
using System.Reflection;
using UnityEngine;

public class GoalMissPlayCheck : MonoBehaviour
{
    private FutsalGameManager manager;
    private BallController ball;
    private int shot;
    private bool waitingForShot;
    private float shotAt;
    private float started;
    private int priorHome;
    private int priorAway;

    private void Start()
    {
        started = Time.realtimeSinceStartup;
        Physics.simulationMode = SimulationMode.FixedUpdate;
    }

    private void Update()
    {
        try
        {
            if (Time.realtimeSinceStartup - started > 90f) throw new Exception("Missed-goal play check timed out");
            manager = FutsalGameManager.Instance;
            if (manager == null) return;
            if (manager.IsPaused) manager.ResumeMatch();
            if (manager.IsSelectingMode)
            {
                typeof(FutsalGameManager).GetField("kickoffCountdown", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 0.1f);
                manager.StartMatch(5);
                ball = FindFirstObjectByType<BallController>();
            }
            if (!waitingForShot && manager.IsPlaying)
            {
                manager.Teams.StopAll();
                manager.Teams.enabled = false;
                foreach (var player in manager.Teams.Players)
                {
                    player.Motor.enabled = false;
                    player.Dribble.enabled = false;
                    player.Body.constraints = RigidbodyConstraints.FreezeAll;
                    player.GetComponent<Collider>().enabled = false;
                    player.Body.position = new Vector3(-8f, 1f, 0f);
                }
                Time.fixedDeltaTime = shot < 6 ? 0.02f : 0.01f;
                float sign = shot % 6 < 3 ? 1f : -1f;
                int kind = shot % 3;
                float x = kind == 0 ? 2.7f : -2.7f;
                Vector3 position = kind == 2 ? new Vector3(0, 1.9f, 17.4f * sign) : new Vector3(x, 0.15f, 17.5f * sign);
                Vector3 velocity = kind == 2 ? new Vector3(0, 4f, 20f * sign) : new Vector3(Mathf.Sign(x) * 4f, 0, 22f * sign);
                ball.Rigidbody.position = position;
                ball.Rigidbody.linearVelocity = velocity;
                ball.Rigidbody.angularVelocity = Vector3.zero;
                priorHome = manager.PlayerScore;
                priorAway = manager.RivalScore;
                shotAt = Time.time;
                waitingForShot = true;
            }
            if (!waitingForShot || Time.time - shotAt < 0.6f) return;
            bool north = shot % 6 < 3;
            if (manager.PlayerScore != priorHome + (north ? 1 : 0) || manager.RivalScore != priorAway + (north ? 0 : 1))
                throw new Exception($"Missed live goal shot={shot} position={ball.Rigidbody.position} velocity={ball.Rigidbody.linearVelocity} score={manager.PlayerScore}:{manager.RivalScore}");
            if (!manager.IsGoalCelebration || Time.timeScale != 1f) throw new Exception("Celebration failed after edge shot");
            Debug.Log($"GOAL_MISS_PLAY shot={shot} side={(north ? "North" : "South")} kind={shot % 3} dt={Time.fixedDeltaTime} PASS");
            shot++;
            waitingForShot = false;
            if (shot == 12)
            {
                Debug.Log("GOAL_MISS_PLAY_CHECKS_PASS");
                Finish(0);
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    private static void Finish(int code)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(code);
#else
        Application.Quit(code);
#endif
    }
}
