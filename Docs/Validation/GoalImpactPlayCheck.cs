// Copy to Assets (not Assets/Editor) in the isolated validation project only.
using System;
using System.IO;
using UnityEngine;

public class GoalImpactPlayCheck : MonoBehaviour
{
    private FutsalGameManager manager;
    private BallController ball;
    private Mesh net;
    private Vector3[] rest;
    private int stage;
    private float started;
    private float shotAt;
    private float maximumBulge;
    private bool captured;
    private bool south;

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
            if (Time.realtimeSinceStartup - started > 30f) throw new Exception("Play-mode goal check timed out");
            manager = FutsalGameManager.Instance;
            if (manager == null) return;
            if (manager.IsPaused) manager.ResumeMatch();
            if (stage == 0)
            {
                ball = FindFirstObjectByType<BallController>();
                manager.StartMatch(5);
                stage = 1;
            }
            if (stage == 1 && manager.IsPlaying)
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
                float sign = south ? -1f : 1f;
                ball.Rigidbody.position = new Vector3(0f, 0.3f, sign * 16.5f);
                ball.Rigidbody.linearVelocity = Vector3.forward * sign * 30f;
                ball.Rigidbody.angularVelocity = Vector3.zero;
                net = GameObject.Find(south ? "South Goal" : "North Goal").transform.Find("Goal Net").GetComponent<MeshFilter>().sharedMesh;
                rest = net.vertices;
                maximumBulge = 0f;
                captured = false;
                shotAt = Time.time;
                Camera camera = Camera.main;
                camera.GetComponent<CameraFollow>().enabled = false;
                camera.transform.position = new Vector3(7f, 4f, sign * 12f);
                camera.transform.LookAt(new Vector3(0f, 1f, sign * 18f));
                camera.fieldOfView = 48f;
                // Preview pipeline only; game assets are never saved here.
                foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (renderer.sharedMaterial == null) continue;
                    Material source = renderer.sharedMaterial;
                    var material = new Material(Shader.Find("Standard"));
                    material.color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                    renderer.sharedMaterial = material;
                }
                stage = 2;
            }
            if (stage == 2)
            {
                Vector3[] vertices = net.vertices;
                float bulge = 0f;
                for (int i = 0; i < vertices.Length; i++) bulge = Mathf.Max(bulge, (vertices[i] - rest[i]).magnitude);
                maximumBulge = Mathf.Max(maximumBulge, bulge);
                if (!captured && bulge > 0.07f)
                {
                    Capture(south ? "south-goal-impact.png" : "north-goal-impact.png");
                    captured = true;
                }
                if (Time.time - shotAt < 1.1f) return;
                if (manager.PlayerScore != 1 || manager.RivalScore != (south ? 1 : 0)) throw new Exception($"Incorrect live goal score {manager.PlayerScore}:{manager.RivalScore} ball={ball.Rigidbody.position} velocity={ball.Rigidbody.linearVelocity} bulge={maximumBulge}");
                if (!manager.IsGoalCelebration || Time.timeScale != 1f) throw new Exception("Ball physics stopped during goal");
                if (maximumBulge < 0.07f) throw new Exception("Live ball collision did not shake net");
                Debug.Log($"GOAL_IMPACT_PLAY {(south ? "South" : "North")} bulge={maximumBulge:F3} score={manager.PlayerScore}:{manager.RivalScore} PASS");
                if (south)
                {
                    Debug.Log("GOAL_IMPACT_PLAY_CHECKS_PASS");
                    Finish(0);
                }
                else { south = true; stage = 1; }
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    private static void Capture(string path)
    {
        var target = new RenderTexture(1400, 900, 24);
        Camera camera = Camera.main;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1400, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        target.Release();
        Destroy(image);
        Destroy(target);
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
