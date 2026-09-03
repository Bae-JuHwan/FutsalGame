using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FutsalStarterSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes";
    private const string ScenePath = SceneFolder + "/FutsalPrototype.unity";
    private const string MaterialFolder = "Assets/Materials";

    static FutsalStarterSceneBuilder()
    {
        EditorApplication.delayCall += CreateOnceIfNeeded;
    }

    private static void CreateOnceIfNeeded()
    {
        if (Application.isPlaying || File.Exists(ScenePath))
            return;

        CreateStarterScene();
    }

    [MenuItem("Futsal/Create or Rebuild Starter Scene")]
    public static void CreateStarterScene()
    {
        EnsureFolder(SceneFolder);
        EnsureFolder(MaterialFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material fieldMaterial = CreateOrLoadMaterial("FieldGreen", new Color(0.08f, 0.42f, 0.20f));
        Material lineMaterial = CreateOrLoadMaterial("FieldWhite", new Color(0.93f, 0.95f, 0.92f));
        Material wallMaterial = CreateOrLoadMaterial("WallBlue", new Color(0.06f, 0.20f, 0.55f));
        Material playerMaterial = CreateOrLoadMaterial("PlayerOrange", new Color(1f, 0.28f, 0.05f));
        Material goalMaterial = CreateOrLoadMaterial("GoalWhite", Color.white);

        GameObject environment = new GameObject("Environment");
        CreateBox("Field", new Vector3(0f, -0.1f, 0f), new Vector3(22f, 0.2f, 36f), fieldMaterial, environment.transform);

        GameObject markings = new GameObject("Field Markings");
        markings.transform.SetParent(environment.transform);
        CreateVisualBox("Center Line", new Vector3(0f, 0.015f, 0f), new Vector3(21.6f, 0.03f, 0.12f), lineMaterial, markings.transform);
        CreateVisualBox("Left Sideline", new Vector3(-10.85f, 0.015f, 0f), new Vector3(0.12f, 0.03f, 35.6f), lineMaterial, markings.transform);
        CreateVisualBox("Right Sideline", new Vector3(10.85f, 0.015f, 0f), new Vector3(0.12f, 0.03f, 35.6f), lineMaterial, markings.transform);
        CreateVisualBox("North Goal Line", new Vector3(0f, 0.015f, 17.85f), new Vector3(21.6f, 0.03f, 0.12f), lineMaterial, markings.transform);
        CreateVisualBox("South Goal Line", new Vector3(0f, 0.015f, -17.85f), new Vector3(21.6f, 0.03f, 0.12f), lineMaterial, markings.transform);

        GameObject boundaries = new GameObject("Boundary Walls");
        boundaries.transform.SetParent(environment.transform);
        CreateBox("Left Wall", new Vector3(-11.25f, 0.5f, 0f), new Vector3(0.5f, 1f, 37f), wallMaterial, boundaries.transform);
        CreateBox("Right Wall", new Vector3(11.25f, 0.5f, 0f), new Vector3(0.5f, 1f, 37f), wallMaterial, boundaries.transform);
        CreateBox("North Wall", new Vector3(0f, 0.5f, 18.25f), new Vector3(22f, 1f, 0.5f), wallMaterial, boundaries.transform);
        CreateBox("South Wall", new Vector3(0f, 0.5f, -18.25f), new Vector3(22f, 1f, 0.5f), wallMaterial, boundaries.transform);

        CreateGoal("North Goal", new Vector3(0f, 0f, 17.7f), goalMaterial, environment.transform);
        CreateGoal("South Goal", new Vector3(0f, 0f, -17.7f), goalMaterial, environment.transform);

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, -6f);
        player.GetComponent<Renderer>().sharedMaterial = playerMaterial;

        Rigidbody rigidbody = player.AddComponent<Rigidbody>();
        rigidbody.mass = 70f;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        player.AddComponent<PlayerController>();

        GameObject directionMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        directionMarker.name = "Facing Marker";
        directionMarker.transform.SetParent(player.transform);
        directionMarker.transform.localPosition = new Vector3(0f, 0.35f, 0.52f);
        directionMarker.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
        directionMarker.GetComponent<Renderer>().sharedMaterial = lineMaterial;
        Object.DestroyImmediate(directionMarker.GetComponent<Collider>());

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = player.transform.position + new Vector3(0f, 16f, -12f);
        camera.fieldOfView = 55f;
        camera.nearClipPlane = 0.1f;
        CameraFollow cameraFollow = cameraObject.AddComponent<CameraFollow>();
        cameraFollow.SetTarget(player.transform);
        cameraObject.transform.LookAt(player.transform.position + Vector3.up);

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.40f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.20f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        Debug.Log("Futsal starter scene created. Press Play and move with WASD or the arrow keys.");
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static GameObject CreateVisualBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject box = CreateBox(name, position, scale, material, parent);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        return box;
    }

    private static void CreateGoal(string name, Vector3 position, Material material, Transform parent)
    {
        GameObject goal = new GameObject(name);
        goal.transform.SetParent(parent);
        goal.transform.position = position;

        const float halfWidth = 3f;
        const float height = 2.2f;
        const float thickness = 0.16f;

        CreateBox("Left Post", position + new Vector3(-halfWidth, height * 0.5f, 0f), new Vector3(thickness, height, thickness), material, goal.transform);
        CreateBox("Right Post", position + new Vector3(halfWidth, height * 0.5f, 0f), new Vector3(thickness, height, thickness), material, goal.transform);
        CreateBox("Crossbar", position + new Vector3(0f, height, 0f), new Vector3(halfWidth * 2f, thickness, thickness), material, goal.transform);
    }

    private static Material CreateOrLoadMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.color = color;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }
}
