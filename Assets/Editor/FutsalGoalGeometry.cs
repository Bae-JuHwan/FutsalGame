using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Baked editor geometry with one shared net mesh and simple boundary colliders.
public static class FutsalGoalGeometry
{
    private const string NetPath = "Assets/Materials/GoalNet.asset";
    private const float HalfWidth = 3f;
    private const float Height = 2.2f;

    public static void Build(Transform goal, Material material)
    {
        float sign = goal.position.z < 0f ? -1f : 1f;
        Vector3 Point(float x, float y, float z) => new Vector3(x, y, z * sign);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * HalfWidth;
            Tube(side < 0 ? "Left Post" : "Right Post", Point(x, 0.06f, 0), Point(x, Height, 0), 0.12f, goal, material, true);
            Tube("Rear Support", Point(x, 0.06f, 1.35f), Point(x, Height, 0.75f), 0.06f, goal, material, false);
            Tube("Top Support", Point(x, Height, 0), Point(x, Height, 0.75f), 0.06f, goal, material, false);
            Tube("Ground Rail", Point(x, 0.04f, 0), Point(x, 0.04f, 1.35f), 0.06f, goal, material, false);
        }
        Tube("Crossbar", Point(-HalfWidth, Height, 0), Point(HalfWidth, Height, 0), 0.12f, goal, material, true);
        Tube("Rear Ground Rail", Point(-HalfWidth, 0.04f, 1.35f), Point(HalfWidth, 0.04f, 1.35f), 0.06f, goal, material, false);
        Tube("Rear Top Rail", Point(-HalfWidth, Height, 0.75f), Point(HalfWidth, Height, 0.75f), 0.06f, goal, material, false);

        GameObject net = new GameObject("Goal Net", typeof(MeshFilter), typeof(MeshRenderer));
        net.transform.SetParent(goal, false);
        // Rotate the symmetric mesh instead of using a negative scale.
        if (sign < 0) net.transform.localRotation = Quaternion.Euler(0, 180, 0);
        net.GetComponent<MeshFilter>().sharedMesh = NetMesh();
        net.GetComponent<MeshRenderer>().sharedMaterial = material;
        net.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Close only the back and sides, leaving the goal mouth open.
        Barrier("Net Back Collision", Point(0, Height * 0.5f, 1.05f), new Vector3(6f, 2.3f, 0.04f), goal,
            Quaternion.Euler(-Mathf.Atan2(0.6f * sign, Height) * Mathf.Rad2Deg, 0, 0));
        foreach (float x in new[] { -HalfWidth, HalfWidth })
            Barrier("Net Side Collision", Point(x, Height * 0.5f, 0.68f), new Vector3(0.04f, Height, 1.36f), goal, Quaternion.identity);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Goal Floor";
        floor.transform.SetParent(goal, false);
        floor.transform.localPosition = Point(0, -0.1f, 0.85f);
        floor.transform.localScale = new Vector3(6.12f, 0.2f, 1.1f);
        floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FieldGreen.mat");
    }

    private static void Barrier(string name, Vector3 position, Vector3 size, Transform parent, Quaternion rotation)
    {
        GameObject barrier = new GameObject(name, typeof(BoxCollider));
        barrier.transform.SetParent(parent, false);
        barrier.transform.localPosition = position;
        barrier.transform.localRotation = rotation;
        barrier.GetComponent<BoxCollider>().size = size;
    }

    public static void OpenGoalMouths()
    {
        foreach (string name in new[] { "North Wall", "South Wall" })
        {
            GameObject wall = GameObject.Find(name);
            if (wall == null || GameObject.Find(name + " Right") != null) continue;
            Vector3 position = wall.transform.position;
            position.x = -7.03f;
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(7.94f, 1f, 0.5f);
            GameObject right = Object.Instantiate(wall, wall.transform.parent);
            right.name = name + " Right";
            position.x = 7.03f;
            right.transform.position = position;
        }
    }

    private static void Tube(string name, Vector3 a, Vector3 b, float diameter, Transform parent, Material material, bool collision)
    {
        GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tube.name = name;
        tube.transform.SetParent(parent, false);
        tube.transform.localPosition = (a + b) * 0.5f;
        tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        tube.transform.localScale = new Vector3(diameter, (b - a).magnitude * 0.5f, diameter);
        tube.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) Object.DestroyImmediate(tube.GetComponent<Collider>());
    }

    private static Mesh NetMesh()
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(NetPath);
        if (existing != null && existing.name == "Goal Net Flexible") return existing;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        void Line(Vector3 a, Vector3 b)
        {
            // Interior rings let impacts bend strands instead of only moving
            // their endpoints, which are pinned to the frame.
            int segments = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 0.15f));
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, b - a);
            int start = vertices.Count;
            for (int segment = 0; segment <= segments; segment++)
            {
                Vector3 center = Vector3.Lerp(a, b, segment / (float)segments);
                for (int corner = 0; corner < 4; corner++)
                {
                    float angle = corner * Mathf.PI * 0.5f;
                    vertices.Add(center + rotation * new Vector3(Mathf.Cos(angle) * 0.008f, 0, Mathf.Sin(angle) * 0.008f));
                }
                if (segment == 0) continue;
                int ring = start + segment * 4;
                for (int corner = 0; corner < 4; corner++)
                {
                    int next = (corner + 1) % 4;
                    triangles.Add(ring - 4 + corner); triangles.Add(ring + corner); triangles.Add(ring + next);
                    triangles.Add(ring - 4 + corner); triangles.Add(ring + next); triangles.Add(ring - 4 + next);
                }
            }
        }
        // Back panel slopes from the top supports to a deeper ground rail.
        for (int i = 0; i <= 40; i++)
        {
            float x = Mathf.Lerp(-HalfWidth, HalfWidth, i / 40f);
            Line(new Vector3(x, 0.04f, 1.35f), new Vector3(x, Height, 0.75f));
            Line(new Vector3(x, Height, 0), new Vector3(x, Height, 0.75f));
        }
        for (int i = 0; i <= 15; i++)
        {
            float t = i / 15f;
            float y = Mathf.Lerp(0.04f, Height, t);
            float z = Mathf.Lerp(1.35f, 0.75f, t);
            Line(new Vector3(-HalfWidth, y, z), new Vector3(HalfWidth, y, z));
            foreach (float x in new[] { -HalfWidth, HalfWidth })
                Line(new Vector3(x, y, 0), new Vector3(x, y, z));
        }
        for (int i = 0; i <= 9; i++)
        {
            float t = i / 9f;
            foreach (float x in new[] { -HalfWidth, HalfWidth })
                Line(new Vector3(x, 0.04f, 1.35f * t), new Vector3(x, Height, 0.75f * t));
        }
        for (int i = 0; i <= 5; i++)
            Line(new Vector3(-HalfWidth, Height, i * 0.15f), new Vector3(HalfWidth, Height, i * 0.15f));
        var mesh = new Mesh { name = "Goal Net Flexible" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }
        AssetDatabase.CreateAsset(mesh, NetPath);
        return mesh;
    }

    [MenuItem("Futsal/Update Ball and Goal Appearance")]
    public static void UpdateScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/FutsalPrototype.unity") return;
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GoalWhite.mat");
        NetMesh();
        OpenGoalMouths();
        foreach (string name in new[] { "North Goal", "South Goal" })
        {
            GameObject goal = GameObject.Find(name);
            if (goal == null || goal.transform.Find("Goal Net") != null) continue;
            while (goal.transform.childCount > 0) Object.DestroyImmediate(goal.transform.GetChild(0).gameObject);
            Build(goal.transform, material);
        }
        GameObject ball = GameObject.Find("Ball");
        if (ball != null)
        {
            ball.transform.localScale = Vector3.one * 0.3f;
            Vector3 position = ball.transform.position;
            position.y = 0.17f;
            ball.transform.position = position;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
