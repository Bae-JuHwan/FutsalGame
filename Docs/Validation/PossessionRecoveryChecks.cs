using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Isolated Unity project only. Exercises actual match/action/dribble methods
// with deterministic ball arrivals; not an end-to-end input/trajectory test.
public static class PossessionRecoveryChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("POSSESSION CHECK: " + message);
    }

    public static void Run()
    {
        try
        {
            foreach (int size in new[] { 3, 5 })
                foreach (float diameter in new[] { 0.3f, 0.5f }) CheckMatch(size, diameter);
            CheckSolo();
            Debug.Log("POSSESSION_RECOVERY_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static void CheckMatch(int size, float diameter)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Physics.simulationMode = SimulationMode.Script;
        var manager = new GameObject("Manager").AddComponent<FutsalGameManager>();
        Call(manager, "Awake");
        Set(manager, "state", Enum.Parse(Get(manager, "state").GetType(), "Playing"));
        GameObject template = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        template.AddComponent<Rigidbody>();
        var motor = template.AddComponent<PlayerController>();
        template.AddComponent<PlayerDribbleController>();
        template.AddComponent<PlayerKickController>();
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.transform.localScale = Vector3.one * diameter;
        var ball = ballObject.AddComponent<BallController>();
        Call(ball, "Awake");
        ball.Rigidbody.useGravity = false;
        var match = manager.gameObject.AddComponent<FutsalTeamMatch>();
        match.Initialize(motor, ball, size);
        foreach (var player in match.Players)
        {
            Call(player.Motor, "Awake");
            Call(player.Dribble, "Awake");
            Set(player.Dribble, "ball", ball);
            player.Body.useGravity = false;
        }

        // Repeat actions so an expired release does not leave stale possession.
        for (int cycle = 0; cycle < 3; cycle++)
        {
            match.ResetFormation();
            // In edit-mode manual simulation, Rigidbody.position does not yet
            // update Transform.position; align both as a real physics tick does.
            foreach (var player in match.Players)
                player.transform.SetPositionAndRotation(player.Body.position, player.Body.rotation);
            Physics.SyncTransforms();
            var passer = match.ControlledPlayer;
            Set(match, "nextActionTime", -1f);
            PutBall(ball, passer, diameter, 0f);
            match.TryPass(passer);
            var receiver = (FutsalPlayer)Get(match, "receiver");
            Require(match.Possessor == null && receiver != null, "Pass releases owner");
            Vector3 passVelocity = ball.Rigidbody.linearVelocity;
            Call(passer.Dribble, "FixedUpdate");
            Require((ball.Rigidbody.linearVelocity - passVelocity).sqrMagnitude < 0.0001f, "Passer cannot recapture during cooldown");
            PutBall(ball, receiver, diameter, 14.5f);
            Call(match, "ResolvePossession");
            Require(match.Possessor == receiver, "Fast pass received at ground height");
            Require(match.ControlledPlayer == receiver, "Control switches to receiver");
            Call(receiver.Dribble, "FixedUpdate");
            Require(ball.Rigidbody.linearVelocity.magnitude < 10f, "Receiver resumes dribble touches");

            Set(match, "nextActionTime", -1f);
            match.TryShoot(receiver);
            Require(match.Possessor == null && ball.Rigidbody.linearVelocity.magnitude > 16f, "Shot releases owner");
            FutsalPlayer defender = match.Players[size];
            PutBall(ball, defender, diameter, 25f);
            Call(match, "ResolvePossession");
            Require(match.Possessor == null, "Fast shot is not instantly trapped");
            PutBall(ball, defender, diameter, 4f);
            Call(match, "FixedUpdate");
            Require(match.Possessor == defender, "AI recovers loose shot");
            Call(defender.Motor, "FixedUpdate");
            Call(defender.Dribble, "FixedUpdate");
            Physics.Simulate(0.02f);
            Require(defender.Motor.MoveDirection.sqrMagnitude > 0.01f && defender.Body.linearVelocity.sqrMagnitude > 0.01f,
                "AI moves after recovering possession");
            Require(defender.Dribble.HasPossession, "AI dribble remains active");

            match.StopAll();
            foreach (var player in match.Players) player.Dribble.ResetControl();
            PutBall(ball, defender, diameter, 0f);
            ball.transform.position += Vector3.up * 2f;
            Physics.SyncTransforms();
            Call(match, "ResolvePossession");
            Require(match.Possessor == null, "High ball stays out of dribble reach");
        }
        Debug.Log($"POSSESSION_RECOVERY {size}v{size} diameter={diameter} PASS");
    }

    private static void PutBall(BallController ball, FutsalPlayer player, float diameter, float speed)
    {
        Vector3 position = player.Body.position + player.transform.forward * 0.95f;
        position.y = diameter * 0.5f;
        ball.transform.position = position;
        ball.Rigidbody.position = position;
        ball.Rigidbody.linearVelocity = player.transform.forward * speed;
        Physics.SyncTransforms();
    }

    private static void CheckSolo()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.transform.position = Vector3.up;
        var motor = player.AddComponent<PlayerController>();
        Call(motor, "Awake");
        var dribble = player.AddComponent<PlayerDribbleController>();
        Call(dribble, "Awake");
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.transform.localScale = Vector3.one * 0.3f;
        ballObject.transform.position = new Vector3(0, 0.15f, 0.95f);
        var ball = ballObject.AddComponent<BallController>();
        Call(ball, "Awake");
        Set(dribble, "ball", ball);
        Physics.SyncTransforms();
        Call(dribble, "FixedUpdate");
        Require(dribble.HasPossession, "Standalone dribble acquires small ground ball");
        dribble.ReleaseControl(1f);
        Call(dribble, "FixedUpdate");
        Require(!dribble.HasPossession, "Standalone release cooldown respected");
        Set(dribble, "releasedUntil", -1f);
        Call(dribble, "FixedUpdate");
        Require(dribble.HasPossession, "Standalone reacquires after release");
    }
}
