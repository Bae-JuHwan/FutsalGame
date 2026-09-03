using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FutsalMatchSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";

    static FutsalMatchSetup()
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

        if (GameObject.Find("Game Manager") == null)
            SetupMatchRules();
    }

    [MenuItem("Futsal/Add Match Rules and HUD")]
    public static void SetupMatchRules()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject managerObject = GameObject.Find("Game Manager");
        if (managerObject == null)
        {
            managerObject = new GameObject("Game Manager");
            managerObject.AddComponent<FutsalGameManager>();
        }

        CreateGoalTrigger("North Goal Trigger", new Vector3(0f, 1.25f, 17.5f), GoalSide.North);
        CreateGoalTrigger("South Goal Trigger", new Vector3(0f, 1.25f, -17.5f), GoalSide.South);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = managerObject;
        EditorGUIUtility.PingObject(managerObject);
        Debug.Log("Match rules added. Score in the north goal before the 60 second timer ends.");
    }

    private static void CreateGoalTrigger(string name, Vector3 position, GoalSide side)
    {
        GameObject triggerObject = GameObject.Find(name);
        if (triggerObject == null)
        {
            triggerObject = new GameObject(name);
            triggerObject.transform.position = position;
        }

        BoxCollider collider = triggerObject.GetComponent<BoxCollider>();
        if (collider == null)
            collider = triggerObject.AddComponent<BoxCollider>();

        collider.isTrigger = true;
        collider.size = new Vector3(5.8f, 2.5f, 0.7f);

        GoalTrigger goalTrigger = triggerObject.GetComponent<GoalTrigger>();
        if (goalTrigger == null)
            goalTrigger = triggerObject.AddComponent<GoalTrigger>();

        goalTrigger.SetDefendedSide(side);
    }
}
