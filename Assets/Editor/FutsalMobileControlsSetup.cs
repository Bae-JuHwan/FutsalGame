using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FutsalMobileControlsSetup
{
    private const string ScenePath = "Assets/Scenes/FutsalPrototype.unity";

    static FutsalMobileControlsSetup()
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

        if (GameObject.Find("Mobile Controls") == null || NeedsKickTuning())
            AddMobileControlsAndTuneKick();
    }

    [MenuItem("Futsal/Add Mobile Controls and Tune Kick")]
    public static void AddMobileControlsAndTuneKick()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        TuneKick();
        EnsureEventSystem();

        GameObject canvasObject = GameObject.Find("Mobile Controls");
        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                "Mobile Controls",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateMoveStick(canvasObject.transform);
            CreateKickButton(canvasObject.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvasObject;
        EditorGUIUtility.PingObject(canvasObject);
        Debug.Log("Mobile controls ready. Kick power is 8.5 with low lift (0.18). ");
    }

    private static void TuneKick()
    {
        PlayerKickController kickController = Object.FindFirstObjectByType<PlayerKickController>();
        if (kickController != null)
        {
            kickController.ConfigureKick(8.5f, 0.18f);
            EditorUtility.SetDirty(kickController);
        }

        BallController ballController = Object.FindFirstObjectByType<BallController>();
        if (ballController != null)
        {
            SerializedObject serializedBall = new SerializedObject(ballController);
            SerializedProperty maxHorizontalSpeed = serializedBall.FindProperty("maxHorizontalSpeed");
            if (maxHorizontalSpeed != null)
            {
                maxHorizontalSpeed.floatValue = 34f;
                serializedBall.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static bool NeedsKickTuning()
    {
        PlayerKickController kickController = Object.FindFirstObjectByType<PlayerKickController>();
        if (kickController == null)
            return false;

        SerializedObject serializedKick = new SerializedObject(kickController);
        SerializedProperty power = serializedKick.FindProperty("kickPower");
        SerializedProperty lift = serializedKick.FindProperty("liftPower");
        return power == null || lift == null ||
               !Mathf.Approximately(power.floatValue, 8.5f) ||
               !Mathf.Approximately(lift.floatValue, 0.18f);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventObject.GetComponent<EventSystem>();
        }

        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            Object.DestroyImmediate(legacyModule);

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }

    private static void CreateMoveStick(Transform canvasTransform)
    {
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject background = CreateImageObject(
            "Move Stick Background",
            canvasTransform,
            new Color(0.04f, 0.08f, 0.12f, 0.48f),
            uiSprite);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        SetBottomLeft(backgroundRect, new Vector2(190f, 185f), new Vector2(245f, 245f));
        background.GetComponent<Image>().raycastTarget = false;

        GameObject stick = CreateImageObject(
            "Move Stick",
            canvasTransform,
            new Color(0.25f, 0.75f, 1f, 0.88f),
            knobSprite);
        RectTransform stickRect = stick.GetComponent<RectTransform>();
        SetBottomLeft(stickRect, new Vector2(190f, 185f), new Vector2(135f, 135f));

        OnScreenStick onScreenStick = stick.AddComponent<OnScreenStick>();
        onScreenStick.controlPath = "<Gamepad>/leftStick";
        onScreenStick.movementRange = 85f;
    }

    private static void CreateKickButton(Transform canvasTransform)
    {
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject button = CreateImageObject(
            "Kick Button",
            canvasTransform,
            new Color(0.92f, 0.16f, 0.08f, 0.88f),
            uiSprite);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-170f, 175f);
        buttonRect.sizeDelta = new Vector2(175f, 175f);

        OnScreenButton onScreenButton = button.AddComponent<OnScreenButton>();
        onScreenButton.controlPath = "<Gamepad>/buttonSouth";

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(button.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = "KICK";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 32;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static GameObject CreateImageObject(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        return imageObject;
    }

    private static void SetBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
