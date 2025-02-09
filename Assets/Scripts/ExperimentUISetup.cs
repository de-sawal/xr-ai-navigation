using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode]
public class ExperimentUISetup : MonoBehaviour
{
    [Header("Canvas Settings")]
    public Canvas mainCanvas;
    public CanvasScaler canvasScaler;

    [Header("Generated UI References")]
    // Main Menu
    [HideInInspector] public GameObject mainMenuPanel;
    [HideInInspector] public TMP_InputField participantIdInput;
    [HideInInspector] public Button startButton;
    [HideInInspector] public Button calibrateButton;
    [HideInInspector] public Button practiceButton;

    // Experiment Panel
    [HideInInspector] public GameObject experimentPanel;
    [HideInInspector] public TextMeshProUGUI objectiveText;
    [HideInInspector] public TextMeshProUGUI helpPromptText;
    [HideInInspector] public TextMeshProUGUI progressText;
    [HideInInspector] public TextMeshProUGUI conditionText;

    // Calibration Panel
    [HideInInspector] public GameObject calibrationPanel;
    [HideInInspector] public TextMeshProUGUI calibrationText;
    [HideInInspector] public Image progressBg;
    [HideInInspector] public Image progressFill;

    // Practice Panel
    [HideInInspector] public GameObject practicePanel;
    [HideInInspector] public TextMeshProUGUI practiceText;
    [HideInInspector] public Button teleportButton;
    [HideInInspector] public Button walkButton;
    [HideInInspector] public Button shapeButton;
    [HideInInspector] public Button exitPracticeButton;

    // Questionnaire Panel
    [HideInInspector] public GameObject questionnairePanel;
    // (You can add more controls here if you want sub-questions, sliders, etc.)

    private void OnEnable()
    {
        // Remove this check if you want runtime generation:
        // if (Application.isPlaying) return; 
        SetupUI();
    }

    private void SetupUI()
    {
        // 1. Create/find a Canvas if one doesn’t exist
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("ExperimentCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.WorldSpace;

            canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Position the canvas in front of the user if in VR
            mainCanvas.transform.position = new Vector3(0, 1.6f, 2f);
            mainCanvas.transform.rotation = Quaternion.Euler(0, 180, 0);
            mainCanvas.transform.localScale = Vector3.one * 0.001f;
        }

        // 2. MAIN MENU PANEL
        if (mainMenuPanel == null)
        {
            mainMenuPanel = CreatePanel("MainMenuPanel", mainCanvas.transform);
            // Title
            CreateText(mainMenuPanel.transform, "Title", "VR Navigation Study",
                       new Vector2(0, 200), new Vector2(800, 100), 48);

            // Participant ID input
            participantIdInput = CreateInputField(mainMenuPanel.transform,
                              "ParticipantID", "Enter Participant ID",
                              new Vector2(0, 50), new Vector2(400, 60));

            // Start Button
            startButton = CreateButton(mainMenuPanel.transform, "StartButton", "Start Experiment",
                                       new Vector2(0, -50), new Vector2(300, 60));

            // Calibrate Button
            calibrateButton = CreateButton(mainMenuPanel.transform, "CalibrateButton", "Calibrate Room",
                                           new Vector2(0, -150), new Vector2(300, 60));

            // Practice Button
            practiceButton = CreateButton(mainMenuPanel.transform, "PracticeButton", "Practice Session",
                                          new Vector2(0, -250), new Vector2(300, 60));
        }

        // 3. EXPERIMENT PANEL
        if (experimentPanel == null)
        {
            experimentPanel = CreatePanel("ExperimentPanel", mainCanvas.transform);
            experimentPanel.SetActive(false);

            objectiveText = CreateText(experimentPanel.transform, "ObjectiveText", "Find the [object]",
                                       new Vector2(0, 200), new Vector2(800, 100), 36);
            helpPromptText = CreateText(experimentPanel.transform, "HelpPrompt", "Press A for help",
                                        new Vector2(0, 100), new Vector2(600, 80), 28);
            progressText = CreateText(experimentPanel.transform, "ProgressText", "Progress: 0/30",
                                      new Vector2(0, -200), new Vector2(400, 60), 24);
            conditionText = CreateText(experimentPanel.transform, "ConditionText", "Current Mode: Walking",
                                       new Vector2(0, -250), new Vector2(400, 60), 24);
        }

        // 4. CALIBRATION PANEL
        if (calibrationPanel == null)
        {
            calibrationPanel = CreatePanel("CalibrationPanel", mainCanvas.transform);
            calibrationPanel.SetActive(false);

            calibrationText = CreateText(calibrationPanel.transform, "CalibrationText",
                "Please walk around the room to scan all surfaces...",
                new Vector2(0, 100), new Vector2(800, 200), 36);

            // Progress bar background
            progressBg = CreateImage(calibrationPanel.transform, "ProgressBackground",
                                     new Vector2(0, 0), new Vector2(600, 40));
            progressBg.color = Color.gray;

            // Progress bar fill
            progressFill = CreateImage(progressBg.transform, "ProgressFill",
                                       new Vector2(0, 0), new Vector2(600, 40));
            progressFill.color = Color.green;
            // Anchor it so we can scale it from left to right
            RectTransform fillRect = progressFill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.sizeDelta = new Vector2(0, 0); // Initially 0 width
        }

        // 5. PRACTICE PANEL
        if (practicePanel == null)
        {
            practicePanel = CreatePanel("PracticePanel", mainCanvas.transform);
            practicePanel.SetActive(false);

            practiceText = CreateText(practicePanel.transform, "PracticeText",
                "Practice Session\nTry all navigation modes",
                new Vector2(0, 200), new Vector2(800, 200), 36);

            teleportButton = CreateButton(practicePanel.transform, "TeleportButton", "Try Teleport",
                                          new Vector2(0, 50), new Vector2(300, 60));
            walkButton = CreateButton(practicePanel.transform, "WalkButton", "Try Walking",
                                      new Vector2(0, -50), new Vector2(300, 60));
            shapeButton = CreateButton(practicePanel.transform, "ShapeButton", "Try Shape",
                                       new Vector2(0, -150), new Vector2(300, 60));
            exitPracticeButton = CreateButton(practicePanel.transform, "ExitPracticeButton", "Exit Practice",
                                              new Vector2(0, -250), new Vector2(300, 60));
        }

        // 6. QUESTIONNAIRE PANEL (if needed)
        if (questionnairePanel == null)
        {
            questionnairePanel = CreatePanel("QuestionnairePanel", mainCanvas.transform);
            questionnairePanel.SetActive(false);

            // Simple example text
            CreateText(questionnairePanel.transform, "QuestionnaireText",
                       "Please fill out the questionnaire...\nPress B to proceed",
                       new Vector2(0, 0), new Vector2(800, 200), 32);
        }
    }

    // -----------------------------------------------------------------------
    // Helper Functions for Creating Panels, Text, InputField, Buttons, Images
    // -----------------------------------------------------------------------
    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920, 1080);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        return panel;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text,
                                       Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }

    private TMP_InputField CreateInputField(Transform parent, string name, string placeholder,
                                            Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Image bgImage = obj.AddComponent<Image>();
        bgImage.color = Color.white;  // background color for the input field

        TMP_InputField input = obj.AddComponent<TMP_InputField>();

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        // Create Text child
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(obj.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 6);
        textRect.offsetMax = new Vector2(-10, -7);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "";
        tmpText.fontSize = 24;
        tmpText.alignment = TextAlignmentOptions.Center;
        input.textComponent = tmpText;

        // Create Placeholder child
        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
        placeholderObj.transform.SetParent(obj.transform, false);
        var placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0, 0);
        placeholderRect.anchorMax = new Vector2(1, 1);
        placeholderRect.offsetMin = new Vector2(10, 6);
        placeholderRect.offsetMax = new Vector2(-10, -7);

        TextMeshProUGUI tmpPlaceholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        tmpPlaceholder.text = placeholder;
        tmpPlaceholder.fontSize = 24;
        tmpPlaceholder.alignment = TextAlignmentOptions.Center;
        tmpPlaceholder.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        input.placeholder = tmpPlaceholder;

        return input;
    }

    private Button CreateButton(Transform parent, string name, string text,
                                Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f); // button background color

        Button button = obj.AddComponent<Button>();

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        // Create text child
        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(obj.transform, false);
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.sizeDelta = size;
        txtRect.anchorMin = new Vector2(0, 0);
        txtRect.anchorMax = new Vector2(1, 1);

        TextMeshProUGUI tmpText = txtObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 24;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        return button;
    }

    private Image CreateImage(Transform parent, string name,
                              Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        return img;
    }
}