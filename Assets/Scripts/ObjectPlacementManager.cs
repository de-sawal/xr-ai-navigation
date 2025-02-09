using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObjectPlacementManager : MonoBehaviour
{
    [Header("Study Object Prefabs")]
    [SerializeField] private GameObject[] studyObjectPrefabs;

    [Header("Placement Constraints")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float minObjectSpacing = 0.5f;
    [SerializeField] private float minSurfaceAreaThreshold = 0.2f;  // Surfaces smaller than this are ignored
    [SerializeField] public float preferredObjectArea = 0.2f;       // Baseline area to decide how many objects to place
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.8f, 1.2f);

    // Additional parameters (if needed):
    [SerializeField] private float minWallDistance = 0.3f;          // Keep away from edges/walls
    [SerializeField] private float objectPlacementHeightOffset = 0.01f; // Slight lift to avoid Z-fighting

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private CalibrationData roomData;

    public List<GameObject> SpawnedObjects => spawnedObjects;

    private void Start()
    {
        LoadCalibrationData();
    }

    private void LoadCalibrationData()
    {
        string json = PlayerPrefs.GetString("RoomCalibration");
        if (!string.IsNullOrEmpty(json))
        {
            roomData = JsonUtility.FromJson<CalibrationData>(json);
        }
        else
        {
            Debug.LogWarning("No room calibration data found!");
        }
    }

    // --------------------------------------------------------------------
    // NEW METHODS for the ExperimentController calls:
    // --------------------------------------------------------------------

    /// <summary>
    /// Default method for placing objects. Calls the existing dynamic approach.
    /// </summary>
    public void PlaceObjects()
    {
        PlaceObjectsDynamically();
    }

    /// <summary>
    /// Overload to place exactly <paramref name="practiceCount"/> objects randomly
    /// across all visible surfaces. Useful for practice mode with a small number of objects.
    /// </summary>
    public void PlaceObjects(int practiceCount)
    {
        ClearExistingObjects();

        if (roomData == null || roomData.surfaces == null || roomData.surfaces.Count == 0)
        {
            Debug.LogWarning("No calibration data/surfaces for practice placement!");
            return;
        }

        // Gather only visible surfaces above the minimum size
        List<RoomCalibrationManager.Surface> validSurfaces = new List<RoomCalibrationManager.Surface>();
        foreach (var surface in roomData.surfaces)
        {
            if (!surface.isVisible) continue;

            float surfaceArea = surface.width * surface.depth;
            if (surfaceArea < minSurfaceAreaThreshold) continue;

            validSurfaces.Add(surface);
        }
        if (validSurfaces.Count == 0)
        {
            Debug.LogWarning("No valid surfaces found for practice placement!");
            return;
        }

        // Randomly place 'practiceCount' objects across all surfaces
        for (int i = 0; i < practiceCount; i++)
        {
            // We'll try up to 10 attempts for each object
            bool placed = false;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Pick a random surface
                RoomCalibrationManager.Surface surface =
                    validSurfaces[Random.Range(0, validSurfaces.Count)];

                Vector3 pos = CalculateRandomPositionOnSurface(surface);
                pos.y += objectPlacementHeightOffset; // minor offset if needed

                GameObject prefab = studyObjectPrefabs[Random.Range(0, studyObjectPrefabs.Length)];
                GameObject newObj = Instantiate(prefab, pos, Quaternion.identity);

                newObj.transform.up = surface.normal;
                newObj.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.World);

                float scaleFactor = Random.Range(randomScaleRange.x, randomScaleRange.y);
                newObj.transform.localScale *= scaleFactor;

                if (IsValidPlacement(newObj.transform.position, newObj.transform.localScale, minObjectSpacing))
                {
                    spawnedObjects.Add(newObj);
                    placed = true;
                    break;
                }
                else
                {
                    Destroy(newObj);
                }
            }
            if (!placed)
            {
                Debug.LogWarning($"Failed to place object #{i + 1} after 10 attempts.");
            }
        }

        Debug.Log($"Practice Mode: Placed {spawnedObjects.Count} objects.");
    }

    // --------------------------------------------------------------------
    // EXISTING DYNAMIC PLACEMENT CODE:
    // --------------------------------------------------------------------

    /// <summary>
    /// A new dynamic method that calculates how many objects can fit each surface 
    /// and scales them accordingly based on preferredObjectArea.
    /// </summary>
    public void PlaceObjectsDynamically()
    {
        if (roomData == null || roomData.surfaces == null)
        {
            Debug.LogError("No calibration data or surfaces available!");
            return;
        }

        // Clear any previously spawned objects
        ClearExistingObjects();

        // For each detected surface, place objects based on its area
        foreach (var surface in roomData.surfaces)
        {
            // Skip invisible or extremely small surfaces
            if (!surface.isVisible) continue;

            float surfaceArea = surface.width * surface.depth;
            if (surfaceArea < minSurfaceAreaThreshold) continue;

            // Estimate how many objects can fit
            // For example, each object might "need" ~ preferredObjectArea
            int objectCount = Mathf.FloorToInt(surfaceArea / preferredObjectArea);
            objectCount = Mathf.Max(objectCount, 1); // At least 1 object if big enough

            // Place each object
            for (int i = 0; i < objectCount; i++)
            {
                // Attempt a certain number of tries to find a valid placement
                bool placed = false;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    Vector3 pos = CalculateRandomPositionOnSurface(surface);

                    // Instantiate a random prefab
                    GameObject prefab = studyObjectPrefabs[Random.Range(0, studyObjectPrefabs.Length)];
                    GameObject newObj = Instantiate(prefab, pos, Quaternion.identity);

                    // Align to surface normal
                    newObj.transform.up = surface.normal;

                    // Random rotation around up axis
                    newObj.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.World);

                    // Random scale factor
                    float scaleFactor = Random.Range(randomScaleRange.x, randomScaleRange.y);
                    newObj.transform.localScale *= scaleFactor;

                    // Check if this placement is valid (no overlaps, etc.)
                    if (IsValidPlacement(newObj.transform.position, newObj.transform.localScale, minObjectSpacing))
                    {
                        spawnedObjects.Add(newObj);
                        placed = true;
                        break; // Stop trying
                    }
                    else
                    {
                        Destroy(newObj);
                    }
                }
            }
        }

        // Optionally, place a few objects on the floor as well if you treat floors differently
        PlaceSomeFloorObjects();

        Debug.Log($"Dynamically placed {spawnedObjects.Count} objects.");
    }

    /// <summary>
    /// Example method to place a few random objects on the floor specifically.
    /// </summary>
    private void PlaceSomeFloorObjects()
    {
        if (roomData.surfaces == null) return;

        var floorSurface = roomData.surfaces.FirstOrDefault(s => s.type == RoomCalibrationManager.SurfaceType.Floor);
        if (floorSurface != null)
        {
            // e.g., place 3 objects on the floor
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = CalculateRandomPositionOnSurface(floorSurface);
                pos.y += objectPlacementHeightOffset; // Slight offset so it doesn't clip the floor

                GameObject prefab = studyObjectPrefabs[Random.Range(0, studyObjectPrefabs.Length)];
                GameObject newObj = Instantiate(prefab, pos, Quaternion.identity);
                newObj.transform.up = floorSurface.normal;

                // Random scale again
                float scaleFactor = Random.Range(randomScaleRange.x, randomScaleRange.y);
                newObj.transform.localScale *= scaleFactor;

                if (IsValidPlacement(newObj.transform.position, newObj.transform.localScale, minObjectSpacing))
                {
                    spawnedObjects.Add(newObj);
                }
                else
                {
                    Destroy(newObj);
                }
            }
        }
    }

    /// <summary>
    /// Calculates a random position within the rectangle of the surface (using width/depth).
    /// </summary>
    private Vector3 CalculateRandomPositionOnSurface(RoomCalibrationManager.Surface surface)
    {
        // Because surfaces might be oriented in any direction, we create local basis vectors
        // perpendicular to surface.normal. We'll use cross products with Vector3.up, etc.

        Vector3 right = Vector3.Cross(surface.normal, Vector3.up).normalized;
        if (right == Vector3.zero)
        {
            // If the surface normal is vertical, cross with Vector3.forward as fallback
            right = Vector3.Cross(surface.normal, Vector3.forward).normalized;
        }

        Vector3 forward = Vector3.Cross(right, surface.normal).normalized;

        float halfWidth = surface.width * 0.5f - minWallDistance;
        float halfDepth = surface.depth * 0.5f - minWallDistance;

        float randX = Random.Range(-halfWidth, halfWidth);
        float randZ = Random.Range(-halfDepth, halfDepth);

        Vector3 position = surface.position + (right * randX) + (forward * randZ);
        return position;
    }

    /// <summary>
    /// Checks if placing an object here (center + scale) would collide with existing objects or environment.
    /// </summary>
    private bool IsValidPlacement(Vector3 position, Vector3 scale, float radiusBuffer)
    {
        // This is a simplistic check using a sphere overlap. 
        // For more accurate checks, consider the actual bounds of the prefab.

        float checkRadius = Mathf.Max(scale.x, scale.z) * 0.25f + radiusBuffer;
        Collider[] colliders = Physics.OverlapSphere(position, checkRadius, obstacleLayer);
        return (colliders.Length == 0);
    }

    /// <summary>
    /// Clear any existing objects that were placed previously.
    /// </summary>
    private void ClearExistingObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj) Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    /// <summary>
    /// Example: If you need references to the placed objects after the fact.
    /// </summary>
    public List<GameObject> GetSpawnedObjects()
    {
        return spawnedObjects;
    }

    /// <summary>
    /// Returns a random placed object (if any).
    /// </summary>
    public GameObject GetRandomObject()
    {
        if (spawnedObjects.Count == 0)
        {
            Debug.LogWarning("No spawned objects available!");
            return null;
        }
        return spawnedObjects[Random.Range(0, spawnedObjects.Count)];
    }
}