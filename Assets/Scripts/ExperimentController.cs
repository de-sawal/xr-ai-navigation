using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class ExperimentController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private RoomCalibrationManager calibrationManager;
    [SerializeField] private ObjectPlacementManager objectManager;
    [SerializeField] private AIAssistant assistant;
    [SerializeField] private InteractionManager interactionManager;
    [SerializeField] private DataLogger dataLogger;

    [Header("UI Setup Reference")]
    // We will get all UI panels and elements at runtime from this.
    [SerializeField] private ExperimentUISetup uiSetup;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip objectiveAnnouncement;
    [SerializeField] private AudioClip helpRequestSound;
    [SerializeField] private AudioClip selectionSound;
    [SerializeField] private AudioClip confirmationSound;
    [SerializeField] private AudioClip errorSound;

    [Header("Selection Feedback")]
    [SerializeField] private Material selectionPreviewMaterial;
    [SerializeField] private Material confirmationMaterial;
    [SerializeField] private float helpButtonCooldown = 1f;

    private ExperimentState currentState;

    // The standard 3 conditions (Teleport, Walking, Shape),
    // We'll use an array of size 3 for the Latin Square logic.
    private NavigationStyle[] conditionOrder;
    private int currentTrialNumber;
    private int currentConditionIndex;
    private float trialStartTime;
    private GameObject currentTarget;
    private GameObject selectedObject;
    private bool isWaitingForHelpRequest;
    private bool canSelectObject;
    private bool helpButtonEnabled = true;
    private Material[] originalMaterials;

    private const int TRIALS_PER_CONDITION = 10;
    private const int TOTAL_CONDITIONS = 3;

    // The same experiment states from your code
    private enum ExperimentState
    {
        MainMenu,
        Calibration,
        ObjectPlacement,
        TrialAnnouncement,
        WaitingForHelp,
        Navigation,
        Selection,
        SelectionConfirmation,
        Questionnaire,
        Complete
    }

    // ---------------------------
    //  AWAKE: GET ALL UI REFERENCES
    // ---------------------------
    private void Awake()
    {
        // Make sure uiSetup has created all UI elements
        // We'll copy references into local fields for easy use:

        // MAIN MENU
        if (uiSetup.mainMenuPanel != null)
        {
            // Panels
            // (We can store them or just reference them directly.)
        }

        // For clarity, we store them in local variables we can Show/Hide:
        mainMenuPanel = uiSetup.mainMenuPanel;
        participantIdInput = uiSetup.participantIdInput;
        startButton = uiSetup.startButton;
        calibrateButton = uiSetup.calibrateButton;
        practiceButton = uiSetup.practiceButton;

        // EXPERIMENT PANEL
        experimentPanel = uiSetup.experimentPanel;
        objectiveText = uiSetup.objectiveText;
        helpPromptText = uiSetup.helpPromptText;
        progressText = uiSetup.progressText;
        conditionText = uiSetup.conditionText;

        // CALIBRATION PANEL
        calibrationPanel = uiSetup.calibrationPanel;
        calibrationText = uiSetup.calibrationText;
        progressBg = uiSetup.progressBg;
        progressFill = uiSetup.progressFill;

        // PRACTICE PANEL
        practicePanel = uiSetup.practicePanel;
        practiceText = uiSetup.practiceText;
        teleportButton = uiSetup.teleportButton;
        walkButton = uiSetup.walkButton;
        shapeButton = uiSetup.shapeButton;
        exitPracticeButton = uiSetup.exitPracticeButton;

        // QUESTIONNAIRE PANEL
        questionnairePanel = uiSetup.questionnairePanel;

        // Add UI button listeners
        if (startButton) startButton.onClick.AddListener(HandleStartButton);
        if (calibrateButton) calibrateButton.onClick.AddListener(() => TransitionToState(ExperimentState.Calibration));
        if (practiceButton) practiceButton.onClick.AddListener(StartPracticeMode);

        if (teleportButton) teleportButton.onClick.AddListener(() => TryNavigationStyle(NavigationStyle.Teleport));
        if (walkButton) walkButton.onClick.AddListener(() => TryNavigationStyle(NavigationStyle.Walking));
        if (shapeButton) shapeButton.onClick.AddListener(() => TryNavigationStyle(NavigationStyle.Shape));
        if (exitPracticeButton) exitPracticeButton.onClick.AddListener(EndPracticeMode);

        // InteractionManager OnObjectSelected event
        if (interactionManager != null)
        {
            interactionManager.OnObjectSelected.AddListener(HandleObjectHover);
        }

        // AIAssistant events
        if (assistant != null)
        {
            assistant.OnNavigationStarted.AddListener(() =>
            {
                canSelectObject = false;
                interactionManager.SetSelectionEnabled(false);
            });
            assistant.OnNavigationCompleted.AddListener(() =>
            {
                if (currentState == ExperimentState.Navigation)
                    TransitionToState(ExperimentState.Selection);
            });
        }

        // RoomCalibrationManager event
        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationComplete.AddListener(() =>
                TransitionToState(ExperimentState.ObjectPlacement));
        }

        // We'll disable all panels, show main menu
        DisableAllPanels();
        TransitionToState(ExperimentState.MainMenu);
    }

    // -----------------------------
    //  UI FIELDS (Now Private)
    // -----------------------------
    private GameObject mainMenuPanel;
    private TMP_InputField participantIdInput;
    private Button startButton;
    private Button calibrateButton;
    private Button practiceButton;

    private GameObject experimentPanel;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI helpPromptText;
    private TextMeshProUGUI progressText;
    private TextMeshProUGUI conditionText;

    private GameObject calibrationPanel;
    private TextMeshProUGUI calibrationText;
    private Image progressBg;
    private Image progressFill;

    private GameObject practicePanel;
    private TextMeshProUGUI practiceText;
    private Button teleportButton;
    private Button walkButton;
    private Button shapeButton;
    private Button exitPracticeButton;

    private GameObject questionnairePanel;

    // -----------------------------
    //  UNITY UPDATE
    // -----------------------------
    private void Update()
    {
        HandleInputs();
    }

    private void HandleInputs()
    {
        // If using OVRInput:
        // A button for help request or selection
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (currentState == ExperimentState.WaitingForHelp && helpButtonEnabled)
            {
                // Help request
                HandleHelpRequest();
                StartCoroutine(HelpButtonCooldown());
            }
            else if (currentState == ExperimentState.Selection && selectedObject != null)
            {
                // Move to confirmation
                TransitionToState(ExperimentState.SelectionConfirmation);
            }
            else if (currentState == ExperimentState.SelectionConfirmation && selectedObject != null)
            {
                // Confirm final selection
                FinalizeSelection();
            }
        }

        // B button to cancel confirmation
        if (currentState == ExperimentState.SelectionConfirmation && OVRInput.GetDown(OVRInput.Button.Two))
        {
            ClearSelection();
            TransitionToState(ExperimentState.Selection);
        }
    }

    private IEnumerator HelpButtonCooldown()
    {
        helpButtonEnabled = false;
        yield return new WaitForSeconds(helpButtonCooldown);
        helpButtonEnabled = true;
    }

    // -----------------------------
    //  MAIN MENU & SETUP
    // -----------------------------
    private void HandleStartButton()
    {
        if (!ValidateSetup()) return;

        dataLogger.SetParticipantId(participantIdInput.text);
        GenerateConditionOrder();

        // Check if we have calibration data
        if (!PlayerPrefs.HasKey("RoomCalibration"))
            TransitionToState(ExperimentState.Calibration);
        else
            TransitionToState(ExperimentState.ObjectPlacement);
    }

    // Called from the "Practice Session" button
    public void StartPracticeMode()
    {
        DisableAllPanels();
        practicePanel.SetActive(true);

        // Place fewer objects (5) for practice
        if (objectManager != null)
        {
            objectManager.PlaceObjects(5);
        }
    }

    // Called from "Exit Practice" button
    private void EndPracticeMode()
    {
        practicePanel.SetActive(false);
        TransitionToState(ExperimentState.MainMenu);
    }

    // -----------------------------
    //  EXPERIMENT FLOW
    // -----------------------------
    private void TransitionToState(ExperimentState newState)
    {
        // Exit logic for old state if needed
        // ...

        // Enter new state
        currentState = newState;
        switch (newState)
        {
            case ExperimentState.MainMenu:
                DisableAllPanels();
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                break;

            case ExperimentState.Calibration:
                DisableAllPanels();
                if (calibrationPanel) calibrationPanel.SetActive(true);
                StartCalibration();
                break;

            case ExperimentState.ObjectPlacement:
                StartCoroutine(HandleObjectPlacement());
                break;

            case ExperimentState.TrialAnnouncement:
                PrepareNewTrial();
                break;

            case ExperimentState.WaitingForHelp:
                SetupHelpPrompt();
                break;

            case ExperimentState.Navigation:
                StartNavigation();
                break;

            case ExperimentState.Selection:
                EnableObjectSelection();
                break;

            case ExperimentState.SelectionConfirmation:
                ShowSelectionConfirmation();
                break;

            case ExperimentState.Questionnaire:
                ShowQuestionnaire();
                break;

            case ExperimentState.Complete:
                EndExperiment();
                break;
        }

        // Update UI progress or text
        UpdateUI();
    }

    private void DisableAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (experimentPanel) experimentPanel.SetActive(false);
        if (calibrationPanel) calibrationPanel.SetActive(false);
        if (practicePanel) practicePanel.SetActive(false);
        if (questionnairePanel) questionnairePanel.SetActive(false);
    }

    // -----------------------------
    //  CALIBRATION
    // -----------------------------
    private void StartCalibration()
    {
        if (calibrationText != null)
            calibrationText.text = "Please walk around the room to scan all surfaces...";

        if (calibrationManager != null)
            calibrationManager.StartCalibration();
    }

    // Called automatically by calibrationManager.OnCalibrationComplete -> see Awake()
    // We simply jump to ObjectPlacement
    // or the manager directly calls TransitionToState(ExperimentState.ObjectPlacement)
    // For clarity, it's already done above.

    // -----------------------------
    //  OBJECT PLACEMENT
    // -----------------------------
    private IEnumerator HandleObjectPlacement()
    {
        // Place the standard objects
        if (objectManager != null)
            objectManager.PlaceObjects();

        yield return new WaitForSeconds(1f);

        // Start session data
        if (dataLogger != null)
            dataLogger.StartNewSession();

        currentTrialNumber = 0;
        currentConditionIndex = 0;

        TransitionToState(ExperimentState.TrialAnnouncement);
    }

    // -----------------------------
    //  TRIAL CONTROL
    // -----------------------------
    private void PrepareNewTrial()
    {
        if (currentTrialNumber >= TRIALS_PER_CONDITION)
        {
            TransitionToState(ExperimentState.Questionnaire);
            return;
        }

        DisableAllPanels();
        if (experimentPanel) experimentPanel.SetActive(true);

        // Get a random object as the target
        if (objectManager != null)
        {
            currentTarget = objectManager.GetRandomObject();
        }

        trialStartTime = Time.time;
        currentTrialNumber++;

        if (currentTarget != null)
        {
            string objectName = currentTarget.name.Replace("(Clone)", "").Trim();
            if (objectiveText) objectiveText.text = $"Find the {objectName}";
        }

        // Play audio
        if (audioSource && objectiveAnnouncement)
            audioSource.PlayOneShot(objectiveAnnouncement);

        TransitionToState(ExperimentState.WaitingForHelp);
    }

    private void SetupHelpPrompt()
    {
        isWaitingForHelpRequest = true;
        if (helpPromptText) helpPromptText.gameObject.SetActive(true);
        if (helpPromptText) helpPromptText.text = "Press A for help";
    }

    private void TryNavigationStyle(NavigationStyle style)
    {
        // If your AI Assistant is set up properly, do:
        if (assistant != null)
        {
            // Switch the assistant to the selected style
            assistant.Initialize(style);

            // Optionally pick a random object to demonstrate navigation
            GameObject targetObject = objectManager.GetRandomObject();
            if (targetObject != null)
            {
                assistant.GuideToTarget(targetObject);
            }
        }
        else
        {
            Debug.LogWarning("Assistant is null; cannot try navigation style.");
        }
    }


    private void HandleHelpRequest()
    {
        if (!isWaitingForHelpRequest) return;
        isWaitingForHelpRequest = false;

        if (audioSource && helpRequestSound)
            audioSource.PlayOneShot(helpRequestSound);

        if (helpPromptText) helpPromptText.gameObject.SetActive(false);
        TransitionToState(ExperimentState.Navigation);
    }

    private void StartNavigation()
    {
        if (assistant != null && currentTarget != null)
        {
            assistant.GuideToTarget(currentTarget);
        }
    }

    private void EnableObjectSelection()
    {
        canSelectObject = true;
        if (interactionManager != null)
            interactionManager.SetSelectionEnabled(true);
    }

    private void ShowSelectionConfirmation()
    {
        if (selectedObject != null && confirmationMaterial != null && originalMaterials != null)
        {
            // Swap to confirmation material
            var mesh = selectedObject.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                Material[] confirmMaterials = new Material[mesh.materials.Length];
                for (int i = 0; i < confirmMaterials.Length; i++)
                {
                    confirmMaterials[i] = confirmationMaterial;
                }
                mesh.materials = confirmMaterials;
            }
        }
        if (helpPromptText) helpPromptText.text = "Press A to confirm, B to cancel";
    }

    private void FinalizeSelection()
    {
        float completionTime = Time.time - trialStartTime;
        bool isCorrect = (selectedObject == currentTarget);

        if (audioSource)
            audioSource.PlayOneShot(isCorrect ? confirmationSound : errorSound);

        LogTrialData(completionTime, isCorrect);
        ClearSelection();

        StartCoroutine(DelayedStateTransition(ExperimentState.TrialAnnouncement, 1.5f));
    }

    private IEnumerator DelayedStateTransition(ExperimentState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        TransitionToState(newState);
    }

    // This is triggered by the InteractionManager when the user hovers or selects an object
    private void HandleObjectHover(GameObject obj)
    {
        if (currentState != ExperimentState.Selection && currentState != ExperimentState.SelectionConfirmation)
            return;

        if (!canSelectObject) return;

        if (selectedObject != obj)
        {
            ClearSelection();
            selectedObject = obj;
            var mesh = obj.GetComponent<MeshRenderer>();

            if (mesh != null)
            {
                originalMaterials = mesh.materials;
                Material[] newMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = (currentState == ExperimentState.Selection)
                        ? selectionPreviewMaterial
                        : confirmationMaterial;
                }
                mesh.materials = newMaterials;

                if (audioSource && selectionSound)
                    audioSource.PlayOneShot(selectionSound);
            }
        }
    }

    private void ClearSelection()
    {
        if (selectedObject != null && originalMaterials != null)
        {
            var mesh = selectedObject.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.materials = originalMaterials;
            }
        }
        selectedObject = null;
        originalMaterials = null;
    }

    // -----------------------------
    //  QUESTIONNAIRE + END
    // -----------------------------
    private void ShowQuestionnaire()
    {
        DisableAllPanels();
        if (questionnairePanel) questionnairePanel.SetActive(true);

        StartCoroutine(HandleQuestionnaireCompletion());
    }

    private IEnumerator HandleQuestionnaireCompletion()
    {
        // Example: wait until user presses B or some UI button
        yield return new WaitUntil(() => OVRInput.GetDown(OVRInput.Button.Two));

        currentConditionIndex++;
        if (currentConditionIndex < TOTAL_CONDITIONS)
        {
            currentTrialNumber = 0;
            assistant.Initialize(conditionOrder[currentConditionIndex]);
            TransitionToState(ExperimentState.TrialAnnouncement);
        }
        else
        {
            TransitionToState(ExperimentState.Complete);
        }
    }

    private void EndExperiment()
    {
        if (dataLogger != null)
            dataLogger.EndSession();

        if (objectiveText)
            objectiveText.text = "Experiment completed!\nThank you for participating.";

        StartCoroutine(DelayedStateTransition(ExperimentState.MainMenu, 5f));
    }

    // -----------------------------
    //  HELPER METHODS
    // -----------------------------
    private void GenerateConditionOrder()
    {
        // e.g., participant ID mod 6 for latin square
        int pid;
        if (!int.TryParse(participantIdInput.text, out pid))
        {
            pid = 0; // fallback
        }
        int orderIndex = pid % 6;

        NavigationStyle[][] latinSquareOrders = {
            new[] { NavigationStyle.Teleport, NavigationStyle.Walking, NavigationStyle.Shape },
            new[] { NavigationStyle.Walking, NavigationStyle.Shape, NavigationStyle.Teleport },
            new[] { NavigationStyle.Shape, NavigationStyle.Teleport, NavigationStyle.Walking },
            new[] { NavigationStyle.Teleport, NavigationStyle.Shape, NavigationStyle.Walking },
            new[] { NavigationStyle.Walking, NavigationStyle.Teleport, NavigationStyle.Shape },
            new[] { NavigationStyle.Shape, NavigationStyle.Walking, NavigationStyle.Teleport }
        };

        conditionOrder = latinSquareOrders[orderIndex];
        assistant.Initialize(conditionOrder[0]);
    }

    private void LogTrialData(float completionTime, bool isCorrect)
    {
        var trialData = new TrialData
        {
            participantId = dataLogger.GetParticipantId(),
            trialNumber = currentTrialNumber,
            condition = conditionOrder[currentConditionIndex],
            completionTime = completionTime,
            isCorrect = isCorrect,
            timestamp = DateTime.Now,
            startPosition = Camera.main.transform.position,
            endPosition = (currentTarget != null) ? currentTarget.transform.position : Vector3.zero,
            totalDistance = (currentTarget != null)
                ? Vector3.Distance(Camera.main.transform.position, currentTarget.transform.position)
                : 0f,
            errors = isCorrect ? 0 : 1
        };

        dataLogger.LogTrial(trialData);
    }

    private bool ValidateSetup()
    {
        if (string.IsNullOrEmpty(participantIdInput.text))
        {
            if (objectiveText) objectiveText.text = "Please enter a Participant ID";
            return false;
        }
        int pid;
        if (!int.TryParse(participantIdInput.text, out pid))
        {
            if (objectiveText) objectiveText.text = "Participant ID must be a number";
            return false;
        }
        return true;
    }

    private void UpdateUI()
    {
        if (currentState == ExperimentState.TrialAnnouncement ||
            currentState == ExperimentState.WaitingForHelp ||
            currentState == ExperimentState.Navigation ||
            currentState == ExperimentState.Selection)
        {
            int totalTrials = TRIALS_PER_CONDITION * TOTAL_CONDITIONS;
            int completedTrials = (currentConditionIndex * TRIALS_PER_CONDITION) + currentTrialNumber;
            if (progressText) progressText.text = $"Progress: {completedTrials}/{totalTrials}";

            if (conditionText && currentConditionIndex < conditionOrder.Length)
            {
                conditionText.text = $"Current Mode: {conditionOrder[currentConditionIndex]}";
            }
        }
    }

}