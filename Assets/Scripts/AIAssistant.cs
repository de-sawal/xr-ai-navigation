using UnityEngine;
using System.Collections;
using DG.Tweening;
using System;
using UnityEngine.Events;

public class AIAssistant : MonoBehaviour
{


    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float teleportFadeDuration = 0.2f;
    [SerializeField] private float highlightDuration = 1.5f;
    [SerializeField] private float pathfindingUpdateRate = 0.2f;
    [SerializeField] private float minDistanceFromTarget = 1.5f;
    [SerializeField] private float maxDistanceFromTarget = 2.5f;

    [Header("Robot Sphere References")]
    [SerializeField] private Animator robotAnimator;
    [SerializeField] private GameObject robotModel;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float transitionDelay = 0.3f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject assistantModel;
    [SerializeField] private GameObject shapeIndicator;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private ParticleSystem teleportEffect;
    [SerializeField] private LineRenderer pathRenderer;
    [SerializeField] private Material pathMaterial;

    // Animation hash IDs for better performance
    private readonly int IdleHash = Animator.StringToHash("Idle");
    private readonly int WalkHash = Animator.StringToHash("Walk");
    private readonly int RollHash = Animator.StringToHash("Roll");
    private readonly int OpenHash = Animator.StringToHash("Open");
    private readonly int CloseHash = Animator.StringToHash("Close");
    private readonly int StopRollHash = Animator.StringToHash("StopRoll");
    private readonly int GoToRollHash = Animator.StringToHash("GoToRoll");

    [Header("Shape Indicator Settings")]
    [SerializeField] private float pulseMinScale = 0.8f;
    [SerializeField] private float pulseMaxScale = 1.2f;
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private Color shapeHighlightColor = Color.yellow;
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private float shapeVerticalOffset = 0.5f;

    // Events
    public UnityEvent OnNavigationStarted;
    public UnityEvent OnNavigationCompleted;
    public UnityEvent<float> OnNavigationProgress;

    private NavigationStyle currentStyle;
    private GameObject currentTarget;
    private Material[] originalMaterials;
    private bool isMoving;
    private Coroutine shapeAnimationCoroutine;
    private Material shapeIndicatorMaterial;
    private Vector3 lastValidPosition;
    private bool isInitialized;

    private void Awake()
    {
        InitializeComponents();
        lastValidPosition = transform.position;

        // Ensure we have the robot animator
        if (robotAnimator == null && robotModel != null)
        {
            robotAnimator = robotModel.GetComponent<Animator>();
        }
    }

    private IEnumerator TeleportSequence()
    {
        isMoving = true;
        OnNavigationProgress?.Invoke(0f);

        // Close robot before teleport
        if (robotAnimator != null)
        {
            robotAnimator.SetTrigger(CloseHash);
            yield return new WaitForSeconds(transitionDelay);
        }

        ShowPreTeleportIndicator();
        yield return new WaitForSeconds(0.5f);

        yield return FadeScreen(true);
        OnNavigationProgress?.Invoke(0.3f);

        Vector3 targetPosition = CalculateOptimalViewingPosition();
        if (ValidatePosition(targetPosition))
        {
            transform.position = targetPosition;
            lastValidPosition = targetPosition;
            transform.LookAt(currentTarget.transform);
        }
        else
        {
            transform.position = lastValidPosition;
        }

        OnNavigationProgress?.Invoke(0.6f);
        yield return FadeScreen(false);
        ShowPostTeleportEffect();

        // Open robot after teleport
        if (robotAnimator != null)
        {
            robotAnimator.SetTrigger(OpenHash);
            yield return new WaitForSeconds(transitionDelay);
        }

        OnNavigationProgress?.Invoke(0.8f);
        yield return HighlightTarget();

        CompleteNavigation();
    }

    private IEnumerator WalkSequence()
    {
        isMoving = true;
        OnNavigationProgress?.Invoke(0f);

        Vector3 targetPosition = CalculateOptimalViewingPosition();
        if (!ValidatePosition(targetPosition))
        {
            targetPosition = FindAlternativePosition();
        }

        ShowPathPreview(targetPosition);
        float totalDistance = Vector3.Distance(transform.position, targetPosition);
        float duration = totalDistance / walkSpeed;

        // Start walking animation
        if (robotAnimator != null)
        {
            robotAnimator.SetTrigger(OpenHash);
            yield return new WaitForSeconds(transitionDelay);
            robotAnimator.SetTrigger(WalkHash);
        }

        // Movement sequence with rotation
        transform.DOMove(targetPosition, duration).SetEase(Ease.Linear);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Smooth rotation towards target
            Vector3 targetDirection = (currentTarget.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            OnNavigationProgress?.Invoke(elapsed / duration * 0.8f);
            yield return null;
        }

        // Stop walking animation
        if (robotAnimator != null)
        {
            robotAnimator.SetTrigger(IdleHash);
        }

        HidePathPreview();
        OnNavigationProgress?.Invoke(0.9f);
        yield return HighlightTarget();

        CompleteNavigation();
    }

    private IEnumerator ShapeSequence()
    {
        isMoving = true;
        OnNavigationProgress?.Invoke(0f);

        // Hide robot model in shape mode
        if (robotModel != null)
        {
            robotModel.SetActive(false);
        }

        if (shapeIndicator != null)
        {
            Vector3 indicatorPosition = currentTarget.transform.position + Vector3.up * shapeVerticalOffset;
            shapeIndicator.transform.position = indicatorPosition;
            shapeIndicator.SetActive(true);
            shapeAnimationCoroutine = StartCoroutine(AnimateShape());
        }

        OnNavigationProgress?.Invoke(0.5f);
        yield return HighlightTarget();
        OnNavigationProgress?.Invoke(1f);

        CompleteNavigation();
    }

    public void Initialize(NavigationStyle style)
    {
        currentStyle = style;
        UpdateVisuals();
        isInitialized = true;
        ResetState();

        // Set initial robot state based on navigation style
        if (robotModel != null)
        {
            robotModel.SetActive(style != NavigationStyle.Shape);
            if (robotAnimator != null && style != NavigationStyle.Shape)
            {
                robotAnimator.SetTrigger(IdleHash);
            }
        }
    }

    private void CompleteNavigation()
    {
        isMoving = false;
        OnNavigationProgress?.Invoke(1f);

        // Reset robot to idle state
        if (robotAnimator != null && currentStyle != NavigationStyle.Shape)
        {
            robotAnimator.SetTrigger(IdleHash);
        }

        OnNavigationCompleted?.Invoke();
    }

    private void OnDisable()
    {
        DOTween.Kill(transform);
        CleanupCurrentTarget();

        // Reset robot state
        if (robotAnimator != null)
        {
            robotAnimator.SetTrigger(IdleHash);
        }
    }

 
    private void InitializeComponents()
    {
        // Setup shape indicator
        if (shapeIndicator != null)
        {
            shapeIndicatorMaterial = new Material(Shader.Find("Standard"));
            shapeIndicatorMaterial.EnableKeyword("_EMISSION");
            shapeIndicator.GetComponent<Renderer>().material = shapeIndicatorMaterial;
            shapeIndicator.SetActive(false);
        }

        // Setup path renderer
        if (pathRenderer != null)
        {
            pathRenderer.material = pathMaterial;
            pathRenderer.enabled = false;
        }

        if (teleportEffect != null)
        {
            var mainModule = teleportEffect.main;
            mainModule.playOnAwake = false;
            teleportEffect.Stop();
        }

        // Initialize events if null
        OnNavigationStarted ??= new UnityEvent();
        OnNavigationCompleted ??= new UnityEvent();
        OnNavigationProgress ??= new UnityEvent<float>();
    }

    private void ResetState()
    {
        isMoving = false;
        if (shapeAnimationCoroutine != null)
        {
            StopCoroutine(shapeAnimationCoroutine);
        }
        StopAllCoroutines();
        CleanupCurrentTarget();
    }

    private void UpdateVisuals()
    {
        bool isShapeMode = currentStyle == NavigationStyle.Shape;
        if (assistantModel != null) assistantModel.SetActive(!isShapeMode);
        if (shapeIndicator != null) shapeIndicator.SetActive(false);
        if (pathRenderer != null) pathRenderer.enabled = false;
    }

    public bool GuideToTarget(GameObject target)
    {
        if (!isInitialized || isMoving || target == null)
        {
            Debug.LogWarning("Cannot guide to target: " +
                (!isInitialized ? "Not initialized" :
                 isMoving ? "Already moving" : "Invalid target"));
            return false;
        }

        CleanupCurrentTarget();
        currentTarget = target;
        StoreOriginalMaterials();

        OnNavigationStarted?.Invoke();

        switch (currentStyle)
        {
            case NavigationStyle.Teleport:
                StartCoroutine(TeleportSequence());
                break;
            case NavigationStyle.Walking:
                StartCoroutine(WalkSequence());
                break;
            case NavigationStyle.Shape:
                StartCoroutine(ShapeSequence());
                break;
            default:
                Debug.LogError("Unknown navigation style");
                return false;
        }

        return true;
    }

    private void StoreOriginalMaterials()
    {
        if (currentTarget.TryGetComponent<MeshRenderer>(out var renderer))
        {
            originalMaterials = renderer.materials;
        }
    }


    private Vector3 CalculateOptimalViewingPosition()
    {
        if (currentTarget == null) return lastValidPosition;

        Vector3 userPosition = Camera.main.transform.position;
        Vector3 targetPosition = currentTarget.transform.position;
        Vector3 directionToUser = (userPosition - targetPosition).normalized;

        // Try different distances
        for (float distance = minDistanceFromTarget; distance <= maxDistanceFromTarget; distance += 0.2f)
        {
            Vector3 potentialPosition = targetPosition - directionToUser * distance;
            if (ValidatePosition(potentialPosition))
            {
                return potentialPosition;
            }
        }

        return lastValidPosition;
    }

    private bool ValidatePosition(Vector3 position)
    {
        // Check for collisions and boundaries
        if (Physics.CheckSphere(position, 0.3f))
        {
            return false;
        }

        // Check if position is within room bounds (assuming room calibration data is available)
        // Add room boundary check here if needed

        return true;
    }

    private Vector3 FindAlternativePosition()
    {
        // Implement fallback position finding logic
        // For now, return last valid position
        return lastValidPosition;
    }

    private void ShowPathPreview(Vector3 targetPosition)
    {
        if (pathRenderer != null)
        {
            pathRenderer.enabled = true;
            Vector3[] pathPoints = new Vector3[] { transform.position, targetPosition };
            pathRenderer.positionCount = 2;
            pathRenderer.SetPositions(pathPoints);
        }
    }

    private void HidePathPreview()
    {
        if (pathRenderer != null)
        {
            pathRenderer.enabled = false;
        }
    }

    private void CleanupCurrentTarget()
    {
        if (currentTarget != null && originalMaterials != null)
        {
            if (currentTarget.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.materials = originalMaterials;
            }
        }
        currentTarget = null;
        originalMaterials = null;
    }

    private void OnDestroy()
    {
        if (shapeIndicatorMaterial != null)
        {
            Destroy(shapeIndicatorMaterial);
        }
    }

    // Keep existing helper methods (ShowPreTeleportIndicator, ShowPostTeleportEffect, 
    // AnimateShape, FadeScreen, HighlightTarget) as they are...




    private void ShowPreTeleportIndicator()
    {
        if (teleportEffect != null)
        {
            teleportEffect.transform.position = transform.position;
            teleportEffect.Play();
        }
    }

    private void ShowPostTeleportEffect()
    {
        if (teleportEffect != null)
        {
            teleportEffect.transform.position = transform.position;
            teleportEffect.Play();
        }
    }


    private IEnumerator AnimateShape()
    {
        while (true)
        {
            // Scale animation
            yield return shapeIndicator.transform.DOScale(Vector3.one * pulseMaxScale, pulseDuration / 2)
                .SetEase(Ease.InOutSine).WaitForCompletion();
            yield return shapeIndicator.transform.DOScale(Vector3.one * pulseMinScale, pulseDuration / 2)
                .SetEase(Ease.InOutSine).WaitForCompletion();

            // Color animation
            if (shapeIndicatorMaterial != null)
            {
                Color emissionColor = shapeHighlightColor * glowIntensity;
                shapeIndicatorMaterial.SetColor("_EmissionColor", emissionColor);
            }
        }
    }

    private Vector3 GetOptimalViewingPosition()
    {
        Vector3 directionToUser = (Camera.main.transform.position - currentTarget.transform.position).normalized;
        return currentTarget.transform.position - directionToUser * 1.5f;
    }

    private IEnumerator FadeScreen(bool fadeOut)
    {
        if (fadePanel != null)
        {
            float target = fadeOut ? 1f : 0f;
            fadePanel.DOFade(target, teleportFadeDuration);
            yield return new WaitForSeconds(teleportFadeDuration);
        }
    }

    private IEnumerator HighlightTarget()
    {
        if (currentTarget == null || highlightMaterial == null) yield break;

        MeshRenderer renderer = currentTarget.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material[] highlightMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < highlightMaterials.Length; i++)
            {
                highlightMaterials[i] = highlightMaterial;
            }

            renderer.materials = highlightMaterials;
            yield return new WaitForSeconds(highlightDuration);

            if (originalMaterials != null)
            {
                renderer.materials = originalMaterials;
            }
        }
    }

}
