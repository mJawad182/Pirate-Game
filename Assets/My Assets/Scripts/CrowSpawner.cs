using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns crows in groups around the main camera at varying heights and distances
/// </summary>
public class CrowSpawner : MonoBehaviour
{
    [Header("Crow Prefab")]
    [Tooltip("Crow prefab to spawn")]
    public GameObject crowPrefab;
    
    [Header("Spawning Settings")]
    [Tooltip("Minimum distance from camera to spawn crows")]
    [Range(10f, 300f)]
    public float minSpawnDistance = 20f;
    
    [Tooltip("Maximum distance from camera to spawn crows")]
    [Range(20f, 600f)]
    public float maxSpawnDistance = 50f;
    
    [Tooltip("Minimum height above camera to spawn crows")]
    [Range(5f, 50f)]
    public float minSpawnHeight = 10f;
    
    [Tooltip("Maximum height above camera to spawn crows")]
    [Range(10f, 300f)]
    public float maxSpawnHeight = 30f;
    
    [Header("Group Spawning")]
    [Tooltip("Possible group sizes (e.g., 2, 3, 6, 9)")]
    public int[] groupSizes = new int[] { 2, 3, 6, 9 };
    
    [Tooltip("Time between spawning groups (seconds)")]
    [Range(1f, 30f)]
    public float spawnInterval = 5f;
    
    [Tooltip("Maximum number of crows in scene at once")]
    [Range(10, 100)]
    public int maxCrowsInScene = 30;
    
    [Header("Group Formation")]
    [Tooltip("Spread radius for crows within a group")]
    [Range(2f, 20f)]
    public float groupSpreadRadius = 5f;
    
    [Tooltip("Minimum distance between crows in a group")]
    [Range(1f, 10f)]
    public float minCrowDistance = 2f;
    
    [Header("Crow Lifetime Settings")]
    [Tooltip("Override crow lifetime settings for all spawned crows")]
    public bool overrideCrowLifetime = true;
    
    [Tooltip("Lifetime duration for spawned crows (seconds). Set to 0 to disable lifetime destruction.")]
    [Range(0f, 300f)]
    public float crowLifetime = 30f;
    
    [Tooltip("Random variation in lifetime (adds ±variation to lifetime)")]
    [Range(0f, 10f)]
    public float crowLifetimeVariation = 5f;
    
    [Header("Camera Reference")]
    [Tooltip("Main camera to spawn around (auto-finds if not assigned)")]
    public Camera mainCamera;
    
    [Tooltip("Auto-find main camera if not assigned")]
    public bool autoFindCamera = true;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private List<GameObject> spawnedCrows = new List<GameObject>();
    private Coroutine spawningCoroutine;
    
    void Start()
    {
        // Find main camera if not assigned
        if (mainCamera == null && autoFindCamera)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("CrowSpawner: Main camera not found! Please assign it in the Inspector.");
            enabled = false;
            return;
        }
        
        if (crowPrefab == null)
        {
            Debug.LogError("CrowSpawner: Crow prefab not assigned! Please assign it in the Inspector.");
            enabled = false;
            return;
        }
        
        // Validate group sizes
        if (groupSizes == null || groupSizes.Length == 0)
        {
            Debug.LogWarning("CrowSpawner: No group sizes defined! Using default [2, 3]");
            groupSizes = new int[] { 2, 3 };
        }
        
        // Start spawning coroutine
        spawningCoroutine = StartCoroutine(SpawnCrowsCoroutine());
        
        if (showDebug) Debug.Log("CrowSpawner: Started crow spawning system");
    }
    
    void OnDestroy()
    {
        // Stop spawning coroutine
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
        }
    }
    
    /// <summary>
    /// Coroutine that continuously spawns groups of crows
    /// </summary>
    private IEnumerator SpawnCrowsCoroutine()
    {
        while (true)
        {
            // Clean up destroyed crows from list
            spawnedCrows.RemoveAll(crow => crow == null);
            
            // Check if we can spawn more crows
            if (spawnedCrows.Count < maxCrowsInScene)
            {
                // Pick a random group size
                int groupSize = groupSizes[Random.Range(0, groupSizes.Length)];
                
                // Calculate how many we can actually spawn
                int availableSlots = maxCrowsInScene - spawnedCrows.Count;
                groupSize = Mathf.Min(groupSize, availableSlots);
                
                if (groupSize > 0)
                {
                    SpawnCrowGroup(groupSize);
                }
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    /// <summary>
    /// Spawns a group of crows around the main camera
    /// </summary>
    private void SpawnCrowGroup(int groupSize)
    {
        if (mainCamera == null || crowPrefab == null)
            return;
        
        // Random distance and height for the group center
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        float height = Random.Range(minSpawnHeight, maxSpawnHeight);
        
        // Random angle around camera
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Calculate group center position
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        Vector3 up = mainCamera.transform.up;
        
        // Create a horizontal circle around camera
        Vector3 horizontalOffset = (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * distance;
        Vector3 verticalOffset = up * height;
        Vector3 groupCenter = cameraPosition + horizontalOffset + verticalOffset;
        
        if (showDebug) Debug.Log($"CrowSpawner: Spawning group of {groupSize} crows at distance {distance:F1}m, height {height:F1}m");
        
        // Spawn crows in the group
        for (int i = 0; i < groupSize; i++)
        {
            Vector3 spawnPosition = GetSpawnPositionInGroup(groupCenter, i, groupSize);
            SpawnCrow(spawnPosition);
        }
    }
    
    /// <summary>
    /// Gets a spawn position for a crow within a group
    /// </summary>
    private Vector3 GetSpawnPositionInGroup(Vector3 groupCenter, int index, int totalCrows)
    {
        // Try to position crows in a formation
        Vector3 offset = Vector3.zero;
        
        if (totalCrows == 1)
        {
            // Single crow at center
            offset = Vector3.zero;
        }
        else if (totalCrows == 2)
        {
            // Two crows side by side
            float spacing = minCrowDistance;
            offset = index == 0 ? Vector3.left * spacing : Vector3.right * spacing;
        }
        else
        {
            // Multiple crows in a loose formation
            float angle = (360f / totalCrows) * index * Mathf.Deg2Rad;
            float radius = Random.Range(minCrowDistance, groupSpreadRadius);
            offset = new Vector3(Mathf.Cos(angle) * radius, Random.Range(-2f, 2f), Mathf.Sin(angle) * radius);
        }
        
        return groupCenter + offset;
    }
    
    /// <summary>
    /// Spawns a single crow at the specified position
    /// </summary>
    private void SpawnCrow(Vector3 position)
    {
        GameObject crow = Instantiate(crowPrefab, position, Quaternion.identity);
        
        // Set up crow controller if it exists
        CrowController crowController = crow.GetComponent<CrowController>();
        if (crowController == null)
        {
            crowController = crow.AddComponent<CrowController>();
        }
        
        // Pass camera reference to crow
        if (crowController != null)
        {
            crowController.mainCamera = mainCamera;
            
            // Override lifetime settings if enabled
            if (overrideCrowLifetime)
            {
                crowController.useLifetime = true;
                crowController.lifetime = crowLifetime;
                crowController.lifetimeVariation = crowLifetimeVariation;
            }
        }
        
        // Add to spawned list
        spawnedCrows.Add(crow);
        
        if (showDebug) Debug.Log($"CrowSpawner: Spawned crow at {position} with lifetime {crowLifetime}s");
    }
    
    /// <summary>
    /// Removes a crow from the spawned list (called by CrowController when destroyed)
    /// </summary>
    public void RemoveCrow(GameObject crow)
    {
        spawnedCrows.Remove(crow);
    }
    
    /// <summary>
    /// Gets the current number of crows in the scene
    /// </summary>
    public int GetCrowCount()
    {
        spawnedCrows.RemoveAll(crow => crow == null);
        return spawnedCrows.Count;
    }
}
