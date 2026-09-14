using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Created by the match manager so existing prototype scenes need no manual wiring.
public class FutsalMatchUI : MonoBehaviour
{
    private FutsalGameManager match;
    private GameObject canvasObject;
    private RectTransform safeRoot;
    private RectTransform controlsSafeRoot;
    private RectTransform menuSafeRoot;
    private RectTransform moveStick;
    private Vector2 moveStickOrigin;
    private GameObject mobileControls;
    private GameObject menu;
    private Button pauseButton;
    private Button resumeButton;
    private Button restartButton;
    private Button modesButton;
    private Button threeButton;
    private Button fiveButton;
    private Button passButton;
    private Button defendButton;
    private Text score;
    private Text timer;
    private Text message;
    private Text menuTitle;
    private Text menuScore;
    private Image staminaFill;
    private Image shotPowerBackground;
    private Image shotPowerFill;
    private Font font;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public void Initialize(FutsalGameManager gameManager)
    {
        match = gameManager;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvasObject = new GameObject("Match UI", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        safeRoot = CreateRect("Safe Area", canvasObject.transform);
        Stretch(safeRoot);
        Image scoreboard = CreatePanel("Scoreboard", safeRoot, new Color(0.03f, 0.06f, 0.1f, 0.9f));
        Place(scoreboard.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(580f, 150f));
        score = CreateText("Score", scoreboard.transform, 34);
        Place(score.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(560f, 50f));
        timer = CreateText("Timer", scoreboard.transform, 42);
        Place(timer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(240f, 56f));
        Image stamina = CreatePanel("Stamina", scoreboard.transform, new Color(0.15f, 0.19f, 0.23f));
        Place(stamina.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(400f, 12f));
        staminaFill = CreatePanel("Fill", stamina.transform, Color.green);
        Stretch(staminaFill.rectTransform);

        shotPowerBackground = CreatePanel("Shot Power", safeRoot, new Color(0.04f, 0.06f, 0.09f, 0.88f));
        Place(shotPowerBackground.rectTransform, new Vector2(1f, 0f),
            new Vector2(-170f, 65f), new Vector2(175f, 18f));
        shotPowerFill = CreatePanel("Fill", shotPowerBackground.transform, new Color(1f, 0.65f, 0.08f));
        Stretch(shotPowerFill.rectTransform);

        pauseButton = CreateButton("Pause", "PAUSE", safeRoot, match.PauseMatch);
        Place(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f),
            new Vector2(-130f, -70f), new Vector2(220f, 100f));
        message = CreateText("Goal Message", safeRoot, 64);
        message.color = new Color(1f, 0.82f, 0.12f);
        Place(message.rectTransform, new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(800f, 100f));

        // The full-screen backdrop blocks touches outside the safe-area menu, too.
        Image backdrop = CreatePanel("Match Menu", canvasObject.transform, new Color(0f, 0.02f, 0.05f, 0.88f));
        backdrop.raycastTarget = true;
        Stretch(backdrop.rectTransform);
        menu = backdrop.gameObject;
        menuSafeRoot = CreateRect("Menu Safe Area", menu.transform);
        Stretch(menuSafeRoot);
        menuTitle = CreateText("Title", menuSafeRoot, 64);
        Place(menuTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(800f, 100f));
        menuScore = CreateText("Final Score", menuSafeRoot, 38);
        Place(menuScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(800f, 70f));
        resumeButton = CreateButton("Resume", "RESUME", menuSafeRoot, match.ResumeMatch);
        Place(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -20f), new Vector2(440f, 110f));
        restartButton = CreateButton("Restart", "RESTART", menuSafeRoot, match.RestartMatch);
        Place(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -160f), new Vector2(440f, 110f));
        modesButton = CreateButton("Modes", "CHANGE MODE", menuSafeRoot, match.ReturnToModeSelection);
        Place(modesButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -300f), new Vector2(440f, 100f));
        threeButton = CreateButton("Three a Side", "3 v 3", menuSafeRoot, () => match.StartMatch(3));
        Place(threeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(-240f, -70f), new Vector2(400f, 140f));
        fiveButton = CreateButton("Five a Side", "5 v 5", menuSafeRoot, () => match.StartMatch(5));
        Place(fiveButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(240f, -70f), new Vector2(400f, 140f));

        mobileControls = GameObject.Find("Mobile Controls");
        if (mobileControls != null)
        {
            moveStick = mobileControls.transform.Find("Move Stick") as RectTransform;
            if (moveStick != null)
                moveStickOrigin = moveStick.anchoredPosition;
            controlsSafeRoot = CreateRect("Controls Safe Area", mobileControls.transform);
            Stretch(controlsSafeRoot);
            // Preserve each existing stick/button's anchors and offsets.
            for (int i = mobileControls.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = mobileControls.transform.GetChild(i);
                if (child != controlsSafeRoot)
                    child.SetParent(controlsSafeRoot, false);
            }

            passButton = CreateButton("Pass Button", "PASS", controlsSafeRoot, Pass);
            Place(passButton.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                new Vector2(-365f, 300f), new Vector2(155f, 155f));
            defendButton = CreateButton("Defend Button", "DEF", controlsSafeRoot, Defend);
            Place(defendButton.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                new Vector2(-540f, 145f), new Vector2(145f, 145f));
        }

        ApplySafeArea();
        Refresh();
    }

    private void LateUpdate()
    {
        if (match == null)
            return;

        if (lastSafeArea != Screen.safeArea || lastScreenSize != new Vector2Int(Screen.width, Screen.height))
            ApplySafeArea();
        Refresh();
    }

    private void Refresh()
    {
        string scoreText = $"PLAYER  {match.PlayerScore}  -  {match.RivalScore}  RIVAL";
        score.text = scoreText;
        timer.text = $"{match.TeamSize}v{match.TeamSize}  |  {Mathf.CeilToInt(match.TimeRemaining):00}";
        float stamina = Mathf.Clamp01(match.StaminaNormalized);
        staminaFill.rectTransform.anchorMax = new Vector2(stamina, 1f);
        staminaFill.color = Color.Lerp(new Color(0.95f, 0.18f, 0.08f), new Color(0.2f, 0.95f, 0.35f), stamina);
        PlayerKickController kick = match.Teams != null && match.Teams.ControlledPlayer != null
            ? match.Teams.ControlledPlayer.GetComponent<PlayerKickController>()
            : null;
        float shotPower = kick != null ? kick.ChargeNormalized : 0f;
        shotPowerBackground.gameObject.SetActive(kick != null && kick.IsCharging && match.IsPlaying);
        shotPowerFill.rectTransform.anchorMax = new Vector2(shotPower, 1f);
        shotPowerFill.color = Color.Lerp(
            new Color(1f, 0.78f, 0.08f),
            new Color(1f, 0.18f, 0.05f),
            shotPower);
        message.text = match.EventMessage;
        bool selecting = match.IsSelectingMode;
        bool showMenu = selecting || match.IsPaused || match.MatchEnded;
        menu.SetActive(showMenu);
        pauseButton.gameObject.SetActive(!showMenu);
        message.gameObject.SetActive(!showMenu);
        resumeButton.gameObject.SetActive(match.IsPaused && !match.MatchEnded);
        restartButton.gameObject.SetActive(!selecting);
        modesButton.gameObject.SetActive(!selecting);
        threeButton.gameObject.SetActive(selecting);
        fiveButton.gameObject.SetActive(selecting);
        safeRoot.gameObject.SetActive(!selecting);
        menuTitle.text = selecting ? "FUTSAL" : match.MatchEnded ? match.EventMessage : "PAUSED";
        menuScore.text = selecting ? "SELECT MATCH MODE" : $"{match.TeamSize} v {match.TeamSize}   |   {scoreText}";
        if (mobileControls != null && mobileControls.activeSelf != match.IsPlaying)
        {
            mobileControls.SetActive(match.IsPlaying);
            if (!match.IsPlaying && moveStick != null)
                moveStick.anchoredPosition = moveStickOrigin;
        }
    }

    private void ApplySafeArea()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        Vector2 min = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        Vector2 max = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        SetSafeAnchors(safeRoot, min, max);
        SetSafeAnchors(menuSafeRoot, min, max);
        if (controlsSafeRoot != null)
            SetSafeAnchors(controlsSafeRoot, min, max);
    }

    private void Pass()
    {
        if (match.Teams != null)
            match.Teams.TryPass(match.Teams.ControlledPlayer);
    }

    private void Defend()
    {
        if (match.Teams != null)
            match.Teams.TryTackle(match.Teams.ControlledPlayer);
    }

    private static void SetSafeAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreatePanel(string name, Transform parent, Color color)
    {
        Image panel = CreateRect(name, parent).gameObject.AddComponent<Image>();
        panel.color = color;
        panel.raycastTarget = false;
        return panel;
    }

    private Text CreateText(string name, Transform parent, int size)
    {
        Text text = CreateRect(name, parent).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string name, string label, Transform parent, UnityAction action)
    {
        Image background = CreatePanel(name, parent, new Color(0.06f, 0.4f, 0.62f));
        background.raycastTarget = true;
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        Text text = CreateText("Label", button.transform, 34);
        text.text = label;
        Stretch(text.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        SetSafeAnchors(rect, Vector2.zero, Vector2.one);
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
            Destroy(canvasObject);
    }
}
