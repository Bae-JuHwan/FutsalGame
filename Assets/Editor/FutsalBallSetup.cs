using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FutsalBallSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";
    private const string BallMaterialPath = "Assets/Materials/BallWhite.mat";

    static FutsalBallSetup()
    {
        EditorApplication.delayCall += AddBallToOpenSceneIfNeeded;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!Application.isPlaying && scene.path == ScenePath && GameObject.Find("Ball") == null)
            EditorApplication.delayCall += AddBallToOpenSceneIfNeeded;
    }

    private static void AddBallToOpenSceneIfNeeded()
    {
        if (Application.isPlaying)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath || GameObject.Find("Ball") != null)
            return;

        AddBallAndShooting();
    }

    [MenuItem("Futsal/Add Ball and Shooting")]
    public static void AddBallAndShooting()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player was not found in FutsalPrototype scene.");
            return;
        }

        if (player.GetComponent<PlayerKickController>() == null)
            player.AddComponent<PlayerKickController>();

        GameObject ball = GameObject.Find("Ball");
        if (ball == null)
        {
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.position = new Vector3(0f, 0.28f, -3.5f);
            ball.transform.localScale = Vector3.one * 0.5f;

            Renderer renderer = ball.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateOrLoadBallMaterial();

            SphereCollider collider = ball.GetComponent<SphereCollider>();
            collider.material = CreateOrLoadBallPhysicsMaterial();

            Rigidbody rigidbody = ball.AddComponent<Rigidbody>();
            rigidbody.mass = 0.43f;
            rigidbody.linearDamping = 0.22f;
            rigidbody.angularDamping = 0.12f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            ball.AddComponent<BallController>();

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            patch.name = "Direction Patch";
            patch.transform.SetParent(ball.transform);
            patch.transform.localPosition = new Vector3(0f, 0f, 0.43f);
            patch.transform.localScale = Vector3.one * 0.22f;
            patch.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/WallBlue.mat");
            Object.DestroyImmediate(patch.GetComponent<Collider>());
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = ball;
        EditorGUIUtility.PingObject(ball);
        Debug.Log("Ball and shooting added. Move with WASD and kick with Space.");
    }

    private static Material CreateOrLoadBallMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(BallMaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            name = "BallWhite",
            color = new Color(0.92f, 0.92f, 0.88f)
        };
        AssetDatabase.CreateAsset(material, BallMaterialPath);
        return material;
    }

    private static PhysicsMaterial CreateOrLoadBallPhysicsMaterial()
    {
        const string path = "Assets/Materials/BallPhysics.physicsMaterial";
        PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material != null)
            return material;

        material = new PhysicsMaterial("BallPhysics")
        {
            dynamicFriction = 0.35f,
            staticFriction = 0.35f,
            bounciness = 0.2f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
