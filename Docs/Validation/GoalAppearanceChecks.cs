using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GoalAppearanceChecks
{
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/FutsalPrototype.unity");
            FutsalGoalGeometry.UpdateScene();
            Physics.SyncTransforms();
            GameObject ball = GameObject.Find("Ball");
            Require(Mathf.Abs(ball.GetComponent<SphereCollider>().bounds.size.x - 0.3f) < 0.001f, "Ball diameter");
            foreach (string name in new[] { "North Goal", "South Goal" })
            {
                Transform goal = GameObject.Find(name).transform;
                Require(goal.Find("Goal Net").GetComponent<MeshFilter>().sharedMesh.vertexCount > 0, "Baked net mesh");
                Require(goal.Find("Left Post").GetComponent<Collider>() != null, "Post collision");
                Require(goal.Find("Goal Net").GetComponent<Collider>() == null, "No solid net mouth");
                Require(goal.Find("Goal Floor") != null, "Floor under net");
            }
            Physics.SyncTransforms();
            foreach (float sign in new[] { -1f, 1f })
            {
                Require(!Physics.Raycast(new Vector3(0, 0.3f, sign * 17.6f), Vector3.forward * sign,
                    0.7f, ~0, QueryTriggerInteraction.Ignore), "Open goal mouth");
                Require(Physics.Raycast(new Vector3(0, 0.3f, sign * 18.4f), Vector3.forward * sign,
                    1f, ~0, QueryTriggerInteraction.Ignore), "Back net stops ball");
            }
            int objectsBefore = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;
            FutsalGoalGeometry.UpdateScene();
            Require(objectsBefore == UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length, "Idempotent update");

            // Render a close view using the validation project's built-in pipeline.
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer.sharedMaterial == null) continue;
                var material = new Material(Shader.Find("Standard"));
                material.color = renderer.sharedMaterial.HasProperty("_BaseColor")
                    ? renderer.sharedMaterial.GetColor("_BaseColor") : renderer.sharedMaterial.color;
                renderer.sharedMaterial = material;
            }
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(9f, 6.5f, 9f);
            camera.transform.LookAt(new Vector3(0, 1f, 17.5f));
            camera.fieldOfView = 43f;
            ball.transform.position = new Vector3(1.2f, 0.17f, 16.5f);
            var target = new RenderTexture(1400, 900, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1400, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes("goal-preview.png", image.EncodeToPNG());
            Debug.Log("GOAL_APPEARANCE_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("GOAL CHECK: " + message);
    }
}
