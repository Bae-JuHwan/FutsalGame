using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FutsalSprintSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";

    static FutsalSprintSetup()
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

        if (GameObject.Find("Sprint Button") == null)
            AddSprintButton();
    }

    [MenuItem("Futsal/Add Sprint and Stamina")]
    public static void AddSprintButton()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject canvasObject = GameObject.Find("Mobile Controls");
        if (canvasObject == null)
        {
            FutsalMobileControlsSetup.AddMobileControlsAndTuneKick();
            canvasObject = GameObject.Find("Mobile Controls");
        }

        if (canvasObject == null)
        {
            Debug.LogError("Mobile Controls canvas could not be created.");
            return;
        }

        GameObject sprintButton = GameObject.Find("Sprint Button");
        if (sprintButton == null)
        {
            Sprite buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sprintButton = new GameObject("Sprint Button", typeof(RectTransform), typeof(Image));
            sprintButton.transform.SetParent(canvasObject.transform, false);

            RectTransform buttonRect = sprintButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-365f, 135f);
            buttonRect.sizeDelta = new Vector2(140f, 140f);

            Image image = sprintButton.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.58f, 0.95f, 0.88f);

            OnScreenButton onScreenButton = sprintButton.AddComponent<OnScreenButton>();
            onScreenButton.controlPath = "<Gamepad>/rightShoulder";

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(sprintButton.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = "RUN";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 27;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = sprintButton;
        EditorGUIUtility.PingObject(sprintButton);
        Debug.Log("Sprint and stamina added. Hold Shift or the RUN button while moving.");
    }
}
