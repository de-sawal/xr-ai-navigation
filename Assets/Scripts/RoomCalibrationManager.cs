using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

public class RoomCalibrationManager : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private float minimumSurfaceArea = 0.25f;
    [SerializeField] private float scanResolution = 0.1f;
    [SerializeField] private float minScanAngle = 30f;
    [SerializeField] private float requiredViewCoverage = 0.8f; // 80% coverage required

    [Header("Visual Feedback")]
    [SerializeField] private GameObject scanIndicatorPrefab;
    [SerializeField] private Material scanProgressMaterial;
    [SerializeField] private float indicatorSpacing = 0.5f;

    [Header("Events")]
    public UnityEvent<float> OnScanProgressUpdated;
    public UnityEvent OnCalibrationComplete;

    private List<Surface> detectedSurfaces = new List<Surface>();
    private Vector3 roomCenter;
    private bool isCalibrating = false;
    private float currentScanCoverage = 0f;
    private Dictionary<Vector3, bool> scannedDirections = new Dictionary<Vector3, bool>();
    private List<GameObject> scanIndicators = new List<GameObject>();

    [System.Serializable]
    public class Surface
    {
        public Vector3 position;
        public Vector3 normal;
        public float width;
        public float depth;
        public float height;
        public SurfaceType type;
        public bool isVisible; // Visibility from center
        public List<Vector3> corners = new List<Vector3>();
    }

    public enum SurfaceType
    {
        Floor,
        Wall,
        Table,
        Shelf
    }

    public void StartCalibration(System.Action onCalibrationComplete = null)
    {
        if (isCalibrating) return;

        ClearExistingData();
        isCalibrating = true;
        StartCoroutine(CalibrationProcess(onCalibrationComplete));
    }

    private void ClearExistingData()
    {
        detectedSurfaces.Clear();
        scannedDirections.Clear();
        CleanupScanIndicators();
        currentScanCoverage = 0f;
    }

    private IEnumerator CalibrationProcess(System.Action onComplete)
    {
        // Wait for XR to initialize
        yield return new WaitForSeconds(1f);

        // Initialize room boundaries
        if (!InitializeRoomBoundaries())
        {
            Debug.LogError("Failed to initialize room boundaries");
            yield break;
        }

        // Start continuous scanning
        while (currentScanCoverage < requiredViewCoverage)
        {
            ScanCurrentView();
            UpdateScanProgress();
            yield return new WaitForSeconds(0.1f);
        }

        // Final processing
        ProcessDetectedSurfaces();
        SaveCalibrationData();

        isCalibrating = false;
        OnCalibrationComplete?.Invoke();
        onComplete?.Invoke();
    }

    private bool InitializeRoomBoundaries()
    {
        // Get room bounds using XR boundary system
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        UnityEngine.XR.InputTracking.GetNodeStates(nodeStates); // Populates the list with XR node states

        if (nodeStates.Count > 0) // Check if there are any nodes
        {
            // Calculate room center and approximate size
            Vector3[] boundaryPoints = new Vector3[0];
            CalculateRoomDimensions(boundaryPoints);
            return true;
        }

        return false;
    }

    private void ScanCurrentView()
    {
        Camera xrCamera = Camera.main;
        if (xrCamera == null) return;

        Vector3 cameraForward = xrCamera.transform.forward;
        Vector3 cameraPosition = xrCamera.transform.position;

        // Check if this direction has been scanned
        Vector3 quantizedDirection = QuantizeDirection(cameraForward);
        if (scannedDirections.ContainsKey(quantizedDirection)) return;

        // Cast rays in a cone pattern
        for (float angle = -minScanAngle; angle <= minScanAngle; angle += 5f)
        {
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * cameraForward;
            CastScanRay(cameraPosition, rayDirection);
        }

        // Mark direction as scanned
        scannedDirections[quantizedDirection] = true;
        SpawnScanIndicator(cameraPosition + quantizedDirection);
    }

    private void CastScanRay(Vector3 origin, Vector3 direction)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, 10f);
        foreach (RaycastHit hit in hits)
        {
            ProcessSurfaceHit(hit);
        }
    }

    private Vector3 QuantizeDirection(Vector3 direction)
    {
        float angle = 15f; // Quantize to 15-degree segments
        return Quaternion.Euler(
            Mathf.Round(direction.x / angle) * angle,
            Mathf.Round(direction.y / angle) * angle,
            Mathf.Round(direction.z / angle) * angle
        ) * Vector3.forward;
    }

    private void ProcessSurfaceHit(RaycastHit hit)
    {
        // Check if hit is part of existing surface
        foreach (Surface surface in detectedSurfaces)
        {
            if (Vector3.Distance(hit.point, surface.position) < scanResolution)
            {
                UpdateExistingSurface(surface, hit);
                return;
            }
        }

        // Create new surface
        CreateNewSurface(hit);
    }

    private void UpdateExistingSurface(Surface surface, RaycastHit hit)
    {
        // Update surface dimensions
        surface.corners.Add(hit.point);
        RecalculateSurfaceDimensions(surface);
    }

    private void CreateNewSurface(RaycastHit hit)
    {
        Surface newSurface = new Surface
        {
            position = hit.point,
            normal = hit.normal,
            corners = new List<Vector3> { hit.point }
        };

        // Determine surface type based on normal and height
        newSurface.type = DetermineSurfaceType(hit);

        detectedSurfaces.Add(newSurface);
    }

    private SurfaceType DetermineSurfaceType(RaycastHit hit)
    {
        float angle = Vector3.Angle(hit.normal, Vector3.up);
        float height = hit.point.y;

        if (angle < 20f) // Nearly horizontal
        {
            if (height < 0.1f) return SurfaceType.Floor;
            if (height < 1.0f) return SurfaceType.Shelf;
            return SurfaceType.Table;
        }

        return SurfaceType.Wall;
    }

    private void RecalculateSurfaceDimensions(Surface surface)
    {
        if (surface.corners.Count < 2) return;

        // Calculate bounding box
        Vector3 min = surface.corners[0];
        Vector3 max = surface.corners[0];

        foreach (Vector3 corner in surface.corners)
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
        }

        surface.width = max.x - min.x;
        surface.depth = max.z - min.z;
        surface.height = max.y - min.y;

        // Check if surface meets minimum size requirements
        if (surface.width * surface.depth < minimumSurfaceArea)
        {
            detectedSurfaces.Remove(surface);
        }
    }

    private void UpdateScanProgress()
    {
        // Calculate coverage based on scanned directions
        currentScanCoverage = (float)scannedDirections.Count / (360f / minScanAngle);
        OnScanProgressUpdated?.Invoke(currentScanCoverage);

        // Update scan visualization
        UpdateScanVisualization();
    }

    private void SpawnScanIndicator(Vector3 position)
    {
        if (scanIndicatorPrefab == null) return;

        GameObject indicator = Instantiate(scanIndicatorPrefab, position, Quaternion.identity);
        scanIndicators.Add(indicator);

        // Fade out indicator
        StartCoroutine(FadeOutIndicator(indicator));
    }

    private IEnumerator FadeOutIndicator(GameObject indicator)
    {
        float duration = 1f;
        float elapsed = 0f;
        Material material = indicator.GetComponent<Renderer>().material;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);

            Color color = material.color;
            color.a = alpha;
            material.color = color;

            yield return null;
        }

        scanIndicators.Remove(indicator);
        Destroy(indicator);
    }

    private void UpdateScanVisualization()
    {
        if (scanProgressMaterial != null)
        {
            scanProgressMaterial.SetFloat("_Progress", currentScanCoverage);
        }
    }

    private void CleanupScanIndicators()
    {
        foreach (GameObject indicator in scanIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        scanIndicators.Clear();
    }

    private void CalculateRoomDimensions(Vector3[] boundaryPoints)
    {
        if (boundaryPoints == null || boundaryPoints.Length == 0)
        {
            // Fallback to approximate room size
            roomCenter = Camera.main.transform.position;
            return;
        }

        Vector3 sum = Vector3.zero;
        foreach (Vector3 point in boundaryPoints)
        {
            sum += point;
        }
        roomCenter = sum / boundaryPoints.Length;
    }

    private void ProcessDetectedSurfaces()
    {
        // Remove duplicate surfaces
        for (int i = detectedSurfaces.Count - 1; i >= 0; i--)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                if (Vector3.Distance(detectedSurfaces[i].position, detectedSurfaces[j].position) < scanResolution)
                {
                    detectedSurfaces.RemoveAt(i);
                    break;
                }
            }
        }

        // Check visibility from center
        foreach (Surface surface in detectedSurfaces)
        {
            CheckSurfaceVisibility(surface);
        }
    }

    private void CheckSurfaceVisibility(Surface surface)
    {
        Vector3 directionToSurface = (surface.position - roomCenter).normalized;
        float distanceToSurface = Vector3.Distance(roomCenter, surface.position);

        surface.isVisible = !Physics.Raycast(roomCenter, directionToSurface, distanceToSurface - 0.1f);
    }

    private void SaveCalibrationData()
    {
        CalibrationData data = new CalibrationData
        {
            surfaces = detectedSurfaces,
            centerPoint = roomCenter,
            scanCoverage = currentScanCoverage
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("RoomCalibration", json);
        PlayerPrefs.Save();
    }

    public float GetScanProgress()
    {
        return currentScanCoverage;
    }

    public List<Surface> GetDetectedSurfaces()
    {
        return new List<Surface>(detectedSurfaces);
    }

    public Vector3 GetRoomCenter()
    {
        return roomCenter;
    }
}

[System.Serializable]
public class CalibrationData
{
    public List<RoomCalibrationManager.Surface> surfaces;
    public Vector3 centerPoint;
    public float scanCoverage;
}