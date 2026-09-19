using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GoalScoringChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Debug.Log("GOAL_SCORING CHECK: " + message);
    }

    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/FutsalPrototype.unity");
            FutsalGoalGeometry.UpdateScene();
            var manager = UnityEngine.Object.FindFirstObjectByType<FutsalGameManager>();
            var ball = UnityEngine.Object.FindFirstObjectByType<BallController>();
            var template = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Call(manager, "Awake");
            Call(ball, "Awake");
            Set(manager, "ball", ball);
            Set(manager, "playerTemplate", template);
            manager.StartMatch(5);
            foreach (var player in manager.Teams.Players)
            {
                Call(player.Motor, "Awake");
                Call(player.Dribble, "Awake");
            }
            Physics.SyncTransforms();
            foreach (var trigger in UnityEngine.Object.FindObjectsByType<GoalTrigger>(FindObjectsSortMode.None))
            {
                Call(trigger, "Start");
                float direction = (GoalSide)Get(trigger, "defendedSide") == GoalSide.North ? 1f : -1f;
                float edge = (float)Get(trigger, "lineEdge");
                bool Path(Vector3 from, Vector3 to, float radius = 0.15f)
                {
                    trigger.GetType().GetField("enteredThroughMouth", Private)?.SetValue(trigger, false);
                    from.z *= direction;
                    to.z *= direction;
                    return (bool)Call(trigger, "CrossedGoalLine", from, to, radius);
                }
                bool Cross(float from, float to, float x = 0, float y = 0.15f, float radius = 0.15f) =>
                    Path(new Vector3(x, y, from), new Vector3(x, y, to), radius);
                Require(Path(new Vector3(2.70f, 0.15f, edge - 0.41f), new Vector3(2.83f, 0.15f, edge + 0.29f)), "Diagonal goal into right side net scores");
                Require(Path(new Vector3(-2.70f, 0.15f, edge - 0.41f), new Vector3(-2.83f, 0.15f, edge + 0.29f)), "Diagonal goal into left side net scores");
                Require(Path(new Vector3(0, 1.90f, edge - 0.51f), new Vector3(0, 2.05f, edge + 0.29f)), "Rising shot after clearing crossbar scores");
                Require(!Cross(edge - 0.5f, edge - 0.05f), "Touching painted line is not a goal");
                Require(!Cross(edge - 0.5f, edge + 0.14f), "Partial crossing is not a goal");
                Require(Cross(edge - 0.5f, edge + 0.17f), "Whole ball crossing is a goal");
                Require(Cross(edge - 0.5f, edge + 0.7f), "Fast shot cannot skip goal plane");
                Require(!Cross(edge - 0.5f, edge + 0.5f, 3.5f), "Outside post rejected");
                Require(!Cross(edge - 0.5f, edge + 0.5f, 2.85f), "Ball overlaps post opening rejected");
                Require(!Cross(edge - 0.5f, edge + 0.5f, 0, 2.3f), "Over crossbar rejected");
                Require(!Cross(edge + 0.5f, edge - 0.5f), "Crossing back toward field rejected");
                Require(!Cross(edge + 0.3f, edge + 0.5f), "Ball already behind goal does not score again");
                Require(!Cross(edge - 0.5f, edge + 0.24f, 0, 0.25f, 0.25f), "Larger ball must also fully cross");
                Require(!Cross(edge - 0.5f, edge + 0.1f), "Entry remains pending while ball overlaps goal line");
                Require((bool)Call(trigger, "CrossedGoalLine", new Vector3(0, 0.15f, direction * (edge + 0.1f)),
                    new Vector3(0, 0.15f, direction * (edge + 0.2f)), 0.15f), "Pending entry scores on a later physics step");
                Call(trigger, "CrossedGoalLine", new Vector3(0, 0.15f, direction * (edge + 0.1f)),
                    new Vector3(0, 0.15f, direction * (edge - 0.5f)), 0.15f);
                Require(!(bool)Call(trigger, "CrossedGoalLine", new Vector3(3.5f, 0.15f, direction * (edge - 0.5f)),
                    new Vector3(3.5f, 0.15f, direction * (edge + 0.5f)), 0.15f), "Returning to field clears previous valid entry");
                Require(!Path(new Vector3(3.5f, 0.15f, edge - 0.3f), new Vector3(0, 0.15f, edge + 1f)), "Entering from outside post cannot score inside net");

                Set(manager, "state", Enum.Parse(Get(manager, "state").GetType(), "Playing"));
                Set(trigger, "tracking", false);
                ball.Rigidbody.position = new Vector3(0, 0.15f, direction * (edge - 0.5f));
                Call(trigger, "FixedUpdate");
                ball.Rigidbody.position = new Vector3(0, 0.15f, direction * (edge + 0.17f));
                ball.Rigidbody.linearVelocity = Vector3.forward * direction * 20f;
                int score = manager.PlayerScore + manager.RivalScore;
                Call(trigger, "FixedUpdate");
                Require(manager.PlayerScore + manager.RivalScore == score + 1, "Crossing awards exactly one goal");
                Require(manager.IsGoalCelebration && !manager.IsPlaying && Time.timeScale == 1f, "Celebration disables play but keeps physics running");
                Require(ball.Rigidbody.linearVelocity.magnitude > 19f, "Scored ball keeps shot momentum");
                Require((float)Get(manager, "goalDelayRemaining") >= 2f, "Net impact has two seconds to play out");
                Call(trigger, "FixedUpdate");
                manager.RegisterGoal((GoalSide)Get(trigger, "defendedSide"));
                Require(manager.PlayerScore + manager.RivalScore == score + 1, "Celebration cannot award duplicate goal");
                manager.PauseMatch();
                Require(Time.timeScale == 0f, "Pause freezes celebration");
                manager.ResumeMatch();
                Require(Time.timeScale == 1f, "Resume continues celebration");
                Set(manager, "state", Enum.Parse(Get(manager, "state").GetType(), "Playing"));
                Set(trigger, "tracking", true);
                Set(trigger, "previousCenter", new Vector3(0, 0.15f, direction * (edge - 0.5f)));
                score = manager.PlayerScore + manager.RivalScore;
                trigger.CheckNetContact(ball, new Vector3(0, 0.15f, direction * (edge + 0.6f)), Vector3.back * direction);
                Require(manager.PlayerScore + manager.RivalScore == score + 1, "Crossing and net rebound within one physics step still scores");
            }

            foreach (var net in UnityEngine.Object.FindObjectsByType<GoalNetReaction>(FindObjectsSortMode.None))
            {
                Transform netTransform = net.transform.Find("Goal Net");
                Mesh shared = netTransform.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] original = shared.vertices;
                Call(net, "Awake");
                net.React(netTransform.TransformPoint(new Vector3(0, 0.6f, 1.2f)), netTransform.TransformDirection(Vector3.forward * 30f));
                Call(net, "Animate", 0.1f);
                Mesh animated = netTransform.GetComponent<MeshFilter>().sharedMesh;
                float displacement = 0f;
                Vector3[] changed = animated.vertices;
                for (int i = 0; i < changed.Length; i++) displacement = Mathf.Max(displacement, (changed[i] - original[i]).magnitude);
                Require(displacement > 0.15f, "Net visibly bulges at impact");
                Require(shared != animated && shared.vertices[0] == original[0], "Shared net asset remains intact");
                Call(net, "Animate", 1.4f);
                changed = animated.vertices;
                for (int i = 0; i < changed.Length; i++) RequireVertex(changed[i] == original[i]);
                Debug.Log("GOAL_SCORING CHECK: Net returns to rest");
            }
            manager.RestartMatch();
            Require(!manager.IsPlaying && Time.timeScale == 0f && manager.PlayerScore == 0 && manager.RivalScore == 0, "Restart resets scores and begins countdown");
            Debug.Log("GOAL_SCORING_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void RequireVertex(bool value)
    {
        if (!value) throw new Exception("Net failed to return to rest");
    }
}
