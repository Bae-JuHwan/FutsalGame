// Copy to Assets/Editor in an isolated project containing the runtime scripts.
// Unity -batchmode -executeMethod DribblePhysicsChecks.Run
// Real planar PhysX checks; rendering, device input and animation are excluded.
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DribblePhysicsChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);

    public static void Run()
    {
        try
        {
            foreach (float dt in new[] { 0.02f, 0.01f })
            {
                Check("straight", Vector2.up, false, dt);
                Check("sprint", Vector2.up, true, dt);
                Check("cut90", Vector2.right, true, dt);
                Check("cut45", new Vector2(1f, 1f).normalized, true, dt);
                Check("reverse180", Vector2.down, true, dt);
                Check("zigzag", Vector2.right, true, dt);
                Check("stop", Vector2.zero, true, dt);
            }
            Debug.Log("DRIBBLE_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void Check(string name, Vector2 after, bool sprint, float dt)
    {
        Time.fixedDeltaTime = dt;
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        Physics.simulationMode = SimulationMode.Script;
        PhysicsScene physics = scene.GetPhysicsScene();
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        SceneManager.MoveGameObjectToScene(player, scene);
        player.transform.position = new Vector3(0f, 1f, 0f);
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        PlayerController motor = player.AddComponent<PlayerController>();
        Call(motor, "Awake");
        PlayerDribbleController dribble = player.AddComponent<PlayerDribbleController>();
        Call(dribble, "Awake");
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SceneManager.MoveGameObjectToScene(ballObject, scene);
        ballObject.transform.localScale = Vector3.one * 0.56f;
        ballObject.transform.position = new Vector3(0f, 0.28f, 0.95f);
        Rigidbody ballBody = ballObject.AddComponent<Rigidbody>();
        ballBody.mass = 0.43f;
        ballBody.useGravity = false;
        ballBody.constraints = RigidbodyConstraints.FreezePositionY;
        ballBody.linearDamping = 0.22f;
        BallController ball = ballObject.AddComponent<BallController>();
        Call(ball, "Awake");
        Set(dribble, "ball", ball);
        Set(dribble, "hasPossession", true);
        Physics.SyncTransforms();
        float maxGap = 0f;
        float minimumCutSpeed = float.MaxValue;
        float speed = 0f;
        for (int i = 0; i < Mathf.RoundToInt(2.4f / dt); i++)
        {
            float now = i * dt;
            Vector2 input = now < 1.2f ? Vector2.up : after;
            if (name == "zigzag" && now >= 1.2f)
                input = new Vector2(Mathf.FloorToInt((now - 1.2f) / 0.15f) % 2 == 0 ? 1f : -1f, 1f).normalized;
            Set(motor, "moveInput", input);
            Set(motor, "sprintHeld", sprint);
            Call(motor, "FixedUpdate");
            speed = new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
            if (i == 0 && speed > 0.6f) throw new Exception("Instantaneous full-speed start");
            if (now >= 1.2f && now < 1.5f) minimumCutSpeed = Mathf.Min(minimumCutSpeed, speed);
            if (float.IsNaN(speed)) throw new Exception("Invalid velocity");
            Call(dribble, "ControlBall", now, dt);
            physics.Simulate(dt);
            Vector3 offset = ballBody.position - body.position;
            offset.y = 0f;
            maxGap = Mathf.Max(maxGap, offset.magnitude);
        }
        Debug.Log($"DRIBBLE {name} dt={dt:F2} maxGap={maxGap:F3} minCutSpeed={minimumCutSpeed:F3} finalSpeed={speed:F3}");
        if (maxGap > 1.4f) throw new Exception(name + ": ball escaped close control");
        if (name == "reverse180" && minimumCutSpeed > 5f) throw new Exception("Reversal failed to brake");
        if (name == "stop" && speed > 0.1f) throw new Exception("Player failed to stop");
        dribble.ReleaseControl(1f);
        ballBody.linearVelocity = Vector3.forward * 14.5f;
        Vector3 before = ballBody.linearVelocity;
        Call(dribble, "FixedUpdate");
        if ((ballBody.linearVelocity - before).sqrMagnitude > 0.0001f)
            throw new Exception("Released pass was modified by dribble control");
        UnityEngine.Object.DestroyImmediate(player);
        UnityEngine.Object.DestroyImmediate(ballObject);
    }
}
