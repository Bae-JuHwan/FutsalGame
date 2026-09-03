using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FutsalGoalkeeperSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";
    private const string GoalkeeperMaterialPath = "Assets/Materials/GoalkeeperRed.mat";

    static FutsalGoalkeeperSetup()
    {
        EditorApplication.delayCall += SetupOpenSceneIfNeeded;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!Application.isPlaying && scene.path == ScenePath)
            EditorApplication.delayCall += SetupOpenSceneIfNeeded;
    }

    private static void SetupOpenSceneIfNeeded()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            return;

        if (GameObject.Find("Goalkeeper") == null || HasGoalBackWall())
            AddGoalkeeperAndRemoveWhiteWalls();
    }

    [MenuItem("Futsal/Add Goalkeeper and Remove Goal Walls")]
    public static void AddGoalkeeperAndRemoveWhiteWalls()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        RemoveGoalBackWalls();

        GameObject goalkeeper = GameObject.Find("Goalkeeper");
        if (goalkeeper == null)
        {
            goalkeeper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            goalkeeper.name = "Goalkeeper";
            goalkeeper.transform.position = new Vector3(0f, 1f, 16.25f);
            goalkeeper.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            goalkeeper.GetComponent<Renderer>().sharedMaterial = CreateOrLoadGoalkeeperMaterial();

            Rigidbody rigidbody = goalkeeper.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            goalkeeper.AddComponent<GoalkeeperAI>();

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Facing Marker";
            marker.transform.SetParent(goalkeeper.transform);
            marker.transform.localPosition = new Vector3(0f, 0.35f, 0.52f);
            marker.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
            marker.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/FieldWhite.mat");
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = goalkeeper;
        EditorGUIUtility.PingObject(goalkeeper);
        Debug.Log("Goalkeeper added and the white goal-back walls were removed.");
    }

    private static bool HasGoalBackWall()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform item in transforms)
        {
            if (item.name == "Goal Back")
                return true;
        }

        return false;
    }

    private static void RemoveGoalBackWalls()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform item in transforms)
        {
            if (item.name == "Goal Back")
                Object.DestroyImmediate(item.gameObject);
        }
    }

    private static Material CreateOrLoadGoalkeeperMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(GoalkeeperMaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            name = "GoalkeeperRed",
            color = new Color(0.75f, 0.06f, 0.08f)
        };
        AssetDatabase.CreateAsset(material, GoalkeeperMaterialPath);
        return material;
    }
}
