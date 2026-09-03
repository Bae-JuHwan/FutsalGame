using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FutsalDefenderSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";

    static FutsalDefenderSetup()
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

        if (GameObject.Find("Rival Defender") == null)
            AddRivalDefender();
    }

    [MenuItem("Futsal/Add Rival Defender")]
    public static void AddRivalDefender()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject defender = GameObject.Find("Rival Defender");
        if (defender == null)
        {
            defender = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            defender.name = "Rival Defender";
            defender.transform.position = new Vector3(0f, 1f, 6f);
            defender.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Material rivalMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/GoalkeeperRed.mat");
            defender.GetComponent<Renderer>().sharedMaterial = rivalMaterial;

            Rigidbody rigidbody = defender.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            defender.AddComponent<RivalDefenderAI>();

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Facing Marker";
            marker.transform.SetParent(defender.transform);
            marker.transform.localPosition = new Vector3(0f, 0.35f, 0.52f);
            marker.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
            marker.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/FieldWhite.mat");
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = defender;
        EditorGUIUtility.PingObject(defender);
        Debug.Log("Rival defender added. It guards its half and clears the ball toward the south goal.");
    }
}
