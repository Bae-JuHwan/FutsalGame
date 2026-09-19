using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GoalImpactPlayCheckBuilder
{
    public static void RunCorners3() => RunCorners(3);
    public static void RunCorners5() => RunCorners(5);

    private static void RunCorners(int teamSize)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FutsalPrototype.unity");
        new GameObject("Corner Recovery Check").AddComponent<CornerRecoveryPlayCheck>().TeamSize = teamSize;
        EditorSceneManager.SaveScene(scene, "Assets/CornerRecoveryCheck.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunMissedGoals()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FutsalPrototype.unity");
        new GameObject("Missed Goal Play Check").AddComponent<GoalMissPlayCheck>();
        EditorSceneManager.SaveScene(scene, "Assets/GoalMissCheck.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FutsalPrototype.unity");
        FutsalGoalGeometry.UpdateScene();
        new GameObject("Goal Impact Play Check").AddComponent<GoalImpactPlayCheck>();
        EditorSceneManager.SaveScene(scene, "Assets/GoalImpactCheck.unity");
        EditorApplication.EnterPlaymode();
    }
}
