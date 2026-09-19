// Copy to Assets in the isolated validation project only.
using System;
using System.Reflection;
using UnityEngine;

public class CornerRecoveryPlayCheck : MonoBehaviour
{
    public int TeamSize = 5;
    private FutsalGameManager manager;
    private BallController ball;
    private int scenario;
    private bool running;
    private float caseStarted;
    private float started;
    private float closest;

    private void Start()
    {
        started = Time.realtimeSinceStartup;
        Physics.simulationMode = SimulationMode.FixedUpdate;
        Time.fixedDeltaTime = 0.02f;
    }

    private void Update()
    {
        try
        {
            if (Time.realtimeSinceStartup - started > 60f) throw new Exception("Corner validation timed out");
            manager = FutsalGameManager.Instance;
            if (manager == null) return;
            if (manager.IsPaused) manager.ResumeMatch();
            if (manager.IsSelectingMode)
            {
                typeof(FutsalGameManager).GetField("kickoffCountdown", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 0.1f);
                manager.StartMatch(TeamSize);
                ball = FindFirstObjectByType<BallController>();
            }
            if (!manager.IsPlaying) return;
            if (!running)
            {
                manager.Teams.StopAll();
                foreach (var player in manager.Teams.Players)
                {
                    player.ResetToHome();
                    player.Body.position = new Vector3(player.HomePosition.x * 0.4f, 1f, player.HomePosition.z * 0.2f);
                }
                float xSign = scenario % 2 == 0 ? 1f : -1f;
                float zSign = scenario % 4 < 2 ? 1f : -1f;
                Vector3 corner = new Vector3(10.84f * xSign, 0.15f, 17.84f * zSign);
                var defender = manager.Teams.Players[TeamSize];
                defender.Body.position = new Vector3(10f * xSign, 1f, 16f * zSign);
                defender.Body.rotation = Quaternion.LookRotation(new Vector3(0.84f * xSign, 0, 1.84f * zSign));
                if (scenario >= 4)
                {
                    var teammate = manager.Teams.Players[TeamSize + 1];
                    teammate.Body.position = new Vector3(9f * xSign, 1f, 16.7f * zSign);
                }
                ball.Rigidbody.position = corner;
                ball.Rigidbody.linearVelocity = Vector3.zero;
                ball.Rigidbody.angularVelocity = Vector3.zero;
                caseStarted = Time.time;
                closest = float.MaxValue;
                running = true;
                return;
            }

            foreach (var player in manager.Teams.Players)
                if (player.Team == FutsalTeam.Away && player.Role == FutsalRole.FieldPlayer)
                {
                    Vector3 delta = player.Body.position - ball.Rigidbody.position;
                    delta.y = 0;
                    closest = Mathf.Min(closest, delta.magnitude);
                }
            if (manager.Teams.Possessor != null && manager.Teams.Possessor.Team == FutsalTeam.Away)
            {
                Debug.Log($"CORNER_RECOVERY {TeamSize}v{TeamSize} case={scenario} crowded={scenario >= 4} time={Time.time - caseStarted:F2} gap={closest:F3} PASS");
                scenario++;
                running = false;
                if (scenario == 8) { Debug.Log("CORNER_RECOVERY_CHECKS_PASS"); Finish(0); }
                return;
            }
            if (Time.time - caseStarted > 4f)
                throw new Exception($"Corner defender failed to recover ball: mode={TeamSize} case={scenario} closest={closest:F3} ball={ball.Rigidbody.position}");
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
