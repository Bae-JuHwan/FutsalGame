// Run only in the isolated validation project. Requires GPU rendering (omit -nographics).
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AthleteVisualChecks
{
    public static void Run()
    {
        try
        {
            CheckTeams(3);
            CheckTeams(5);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = Vector3.one * 2f;
            floor.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.09f, 0.19f, 0.13f) };
            for (int i = 0; i < 3; i++)
            {
                var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.transform.position = new Vector3((i - 1) * 1.15f, 1f, 0f);
                player.AddComponent<Rigidbody>().isKinematic = true;
                Material source = player.GetComponent<Renderer>().sharedMaterial;
                player.AddComponent<FutsalPlayerVisual>().Initialize(source,
                    i == 1 ? FutsalTeam.Away : FutsalTeam.Home,
                    i == 2 ? FutsalRole.Goalkeeper : FutsalRole.FieldPlayer, i + 7);
                if (player.GetComponent<Renderer>().enabled) throw new Exception("Capsule was not hidden");
                if (player.GetComponentsInChildren<Collider>().Length != 1) throw new Exception("Visual added physics colliders");
                Transform model = player.transform.Find("Player Model");
                var animation = model.GetComponent<Animation>();
                foreach (string name in new[] { "Idle", "Jog", "Sprint", "Kick" })
                    if (animation[name] == null) throw new Exception("Missing animation: " + name);
                string pose = i == 1 ? "Jog" : "Idle";
                animation[pose].clip.SampleAnimation(model.gameObject, i == 1 ? 0.12f : 0f);
                int vertices = 0;
                Bounds bounds = new Bounds(); bool first = true;
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.updateWhenOffscreen = true;
                    vertices += renderer.sharedMesh.vertexCount;
                    Mesh baked = new Mesh();renderer.BakeMesh(baked, true);
                    foreach (Vector3 point in baked.vertices)
                    {
                        Vector3 world = renderer.transform.TransformPoint(point);
                        if (first) { bounds = new Bounds(world, Vector3.zero); first = false; }
                        else bounds.Encapsulate(world);
                    }
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                Debug.Log($"ATHLETE {i} vertices={vertices} height={bounds.size.y:F3} feet={bounds.min.y:F3}");
                if (vertices < 1000 || vertices > 60000) throw new Exception("Unexpected mesh complexity");
                if (i != 1 && (bounds.size.y < 1.6f || bounds.size.y > 2.1f || Mathf.Abs(bounds.min.y) > 0.15f))
                    throw new Exception("Incorrect character scale or ground alignment");
            }
            var light = new GameObject("Key").AddComponent<Light>();
            light.type = LightType.Directional;light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(3.8f, 2.9f, 5.8f);
            camera.transform.LookAt(new Vector3(0f, 0.9f, 0f));
            camera.orthographic = true;camera.orthographicSize = 1.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.08f);
            var target = new RenderTexture(1400, 900, 24);
            camera.targetTexture = target;camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1400, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0);image.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "athlete-unity-preview.png"), image.EncodeToPNG());
            RenderTexture.active = null;camera.targetTexture = null;target.Release();
            Debug.Log("ATHLETE_VISUAL_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void CheckTeams(int teamSize)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject template = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        template.AddComponent<Rigidbody>();
        var motor = template.AddComponent<PlayerController>();
        template.AddComponent<PlayerDribbleController>();
        template.AddComponent<PlayerKickController>();
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var ball = ballObject.AddComponent<BallController>();
        typeof(BallController).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(ball, null);
        var match = new GameObject("Teams").AddComponent<FutsalTeamMatch>();
        match.Initialize(motor, ball, teamSize);
        if (match.Players.Count != teamSize * 2) throw new Exception("Incorrect team size");
        int keepers = 0;
        foreach (FutsalPlayer player in match.Players)
        {
            if (player.Role == FutsalRole.Goalkeeper) keepers++;
            if (player.GetComponent<Renderer>().enabled) throw new Exception("Visible capsule in match");
            if (player.GetComponentsInChildren<SkinnedMeshRenderer>().Length != 5) throw new Exception("Missing player meshes");
            if (player.GetComponentsInChildren<Rigidbody>().Length != 1 || player.GetComponentsInChildren<Collider>().Length != 1)
                throw new Exception("Character visual changed physics components");
            player.GetComponent<FutsalPlayerVisual>().PlayKick(1f);
        }
        match.ResetFormation();
        if (keepers != 2) throw new Exception("Incorrect goalkeeper count");
        Debug.Log($"ATHLETE_TEAM_CHECK {teamSize}v{teamSize} players={match.Players.Count} keepers={keepers} PASS");
    }
}
