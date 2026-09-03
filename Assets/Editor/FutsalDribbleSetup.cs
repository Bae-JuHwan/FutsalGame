using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FutsalDribbleSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";

    static FutsalDribbleSetup()
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

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<PlayerDribbleController>() == null)
            AddPlayerDribbling();
    }

    [MenuItem("Futsal/Add Player Dribbling")]
    public static void AddPlayerDribbling()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Player was not found in FutsalPrototype scene.");
            return;
        }

        PlayerDribbleController dribble = player.GetComponent<PlayerDribbleController>();
        if (dribble == null)
            dribble = player.gameObject.AddComponent<PlayerDribbleController>();

        EditorUtility.SetDirty(dribble);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = player.gameObject;
        EditorGUIUtility.PingObject(player.gameObject);
        Debug.Log("Player dribbling added. Move into the ball to make short controlled touches.");
    }
}
