using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
    [Range(10, 500)]
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
    
    [Header("Crow Sound Settings")]
    [Tooltip("Minimum time between random crow sounds (seconds)")]
    [Range(5f, 10f)]
    public float minSoundInterval = 5f;
    
    [Tooltip("Maximum time between random crow sounds (seconds)")]
    [Range(5f, 10f)]
    public float maxSoundInterval = 10f;
    
    [Tooltip("Crow scatter sound to play when cannon fires")]
    public AudioClip crowScatterSound;
    
    [Tooltip("Volume of the crow scatter sound")]
    [Range(0f, 1f)]
    public float scatterSoundVolume = 0.7f;
    
    [Tooltip("Minimum cooldown time between scatter sounds (seconds)")]
    [Range(20f, 40f)]
    public float minScatterSoundCooldown = 20f;
    
    [Tooltip("Maximum cooldown time between scatter sounds (seconds)")]
    [Range(20f, 100f)]
    public float maxScatterSoundCooldown = 40f;
    
    [System.Serializable]
    public class CrowPath
    {
        [Tooltip("Start point of the path")]
        public Transform startPoint;
        
        [Tooltip("End point of the path")]
        public Transform endPoint;
        
        [Tooltip("Path name for debugging")]
        public string pathName = "Path";
    }
    
    [Header("Path-Based Crow Spawning")]
    [Tooltip("Enable path-based crow spawning")]
    public bool enablePathSpawning = true;
    
    [Tooltip("Paths for crows to follow (3-4 paths recommended). Each path has a start and end point.")]
    public CrowPath[] crowPaths;
    
    [Tooltip("Minimum time between path crow spawns (seconds)")]
    [Range(2f, 60f)]
    public float minPathSpawnInterval = 5f;
    
    [Tooltip("Maximum time between path crow spawns (seconds)")]
    [Range(5f, 200f)]
    public float maxPathSpawnInterval = 15f;
    
    [Tooltip("Group sizes for path spawns (e.g., 6, 7, 8, 9)")]
    public int[] pathGroupSizes = new int[] { 6, 7, 8, 9 };
    
    [Tooltip("Speed multiplier for path crows")]
    [Range(0.5f, 3f)]
    public float pathCrowSpeedMultiplier = 2f;
    
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
    private Coroutine soundCoroutine;
    private Coroutine pathSpawningCoroutine;
    private Dictionary<int, List<GameObject>> crowGroups = new Dictionary<int, List<GameObject>>();
    private int nextGroupID = 0;
    private AudioSource audioSource;
    private float scatterSoundCooldownEndTime = 0f;
    private bool canPlayScatterSound = true;
    
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
        
        // Start sound playback coroutine
        soundCoroutine = StartCoroutine(PlayRandomCrowSoundCoroutine());
        
        // Setup audio for scatter sound
        SetupAudio();
        
        // Subscribe to cannon fire event
        EventManager.OnCannonFired += OnCannonFired;
        
        // Validate paths
        ValidatePaths();
        
        // Start path-based spawning coroutine
        if (enablePathSpawning)
        {
            pathSpawningCoroutine = StartCoroutine(SpawnPathCrowsCoroutine());
        }
        
        if (showDebug) Debug.Log("CrowSpawner: Started crow spawning system");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from cannon fire event
        EventManager.OnCannonFired -= OnCannonFired;
        
        // Stop spawning coroutine
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
        }
        
        // Stop sound coroutine
        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
        }
        
        // Stop path spawning coroutine
        if (pathSpawningCoroutine != null)
        {
            StopCoroutine(pathSpawningCoroutine);
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
        
        // Create a new group
        int groupID = nextGroupID++;
        List<GameObject> groupCrows = new List<GameObject>();
        crowGroups[groupID] = groupCrows;
        
        // Random group movement direction
        float groupDirectionAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 groupDirection = new Vector3(Mathf.Cos(groupDirectionAngle), Random.Range(-0.2f, 0.2f), Mathf.Sin(groupDirectionAngle)).normalized;
        
        if (showDebug) Debug.Log($"CrowSpawner: Spawning group {groupID} of {groupSize} crows at distance {distance:F1}m, height {height:F1}m");
        
        // Spawn crows in the group
        for (int i = 0; i < groupSize; i++)
        {
            Vector3 spawnPosition = GetSpawnPositionInGroup(groupCenter, i, groupSize);
            GameObject crow = SpawnCrow(spawnPosition, groupID, groupCenter, groupDirection);
            groupCrows.Add(crow);
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
    private GameObject SpawnCrow(Vector3 position, int groupID = -1, Vector3 groupCenter = default, Vector3 groupDirection = default, float speedMultiplier = 1f)
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
            
            // Apply speed multiplier if different from default
            if (speedMultiplier != 1f)
            {
                // Modify flySpeed (Start() has already run, so we need to update normalSpeed too)
                crowController.flySpeed *= speedMultiplier;
                
                // Update normalSpeed using reflection (since it's private)
                var normalSpeedField = typeof(CrowController).GetField("normalSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (normalSpeedField != null)
                {
                    float currentNormalSpeed = (float)normalSpeedField.GetValue(crowController);
                    normalSpeedField.SetValue(crowController, currentNormalSpeed * speedMultiplier);
                }
                
                // Update currentSpeed as well
                var currentSpeedField = typeof(CrowController).GetField("currentSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentSpeedField != null)
                {
                    float currentSpeedValue = (float)currentSpeedField.GetValue(crowController);
                    currentSpeedField.SetValue(crowController, currentSpeedValue * speedMultiplier);
                }
            }
            
            // Set group information if this is part of a group
            if (groupID >= 0)
            {
                crowController.groupID = groupID;
                crowController.groupCenter = groupCenter;
                crowController.groupDirection = groupDirection;
                crowController.isInGroup = true;
                crowController.crowSpawner = this;
            }
            
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
        
        if (showDebug) Debug.Log($"CrowSpawner: Spawned crow at {position} with lifetime {crowLifetime}s (Group: {groupID}, Speed: {speedMultiplier}x)");
        
        return crow;
    }
    
    /// <summary>
    /// Removes a crow from the spawned list (called by CrowController when destroyed)
    /// </summary>
    public void RemoveCrow(GameObject crow)
    {
        spawnedCrows.Remove(crow);
        
        // Remove from group if in one
        CrowController controller = crow.GetComponent<CrowController>();
        if (controller != null && controller.isInGroup && crowGroups.ContainsKey(controller.groupID))
        {
            crowGroups[controller.groupID].Remove(crow);
            
            // Clean up empty groups
            if (crowGroups[controller.groupID].Count == 0)
            {
                crowGroups.Remove(controller.groupID);
            }
        }
    }
    
    /// <summary>
    /// Gets all crows in a group
    /// </summary>
    public List<GameObject> GetGroupCrows(int groupID)
    {
        if (crowGroups.ContainsKey(groupID))
        {
            // Clean up null references
            crowGroups[groupID].RemoveAll(crow => crow == null);
            return new List<GameObject>(crowGroups[groupID]);
        }
        return new List<GameObject>();
    }
    
    /// <summary>
    /// Gets the current center position of a group
    /// </summary>
    public Vector3 GetGroupCenter(int groupID)
    {
        List<GameObject> groupCrows = GetGroupCrows(groupID);
        if (groupCrows.Count == 0) return Vector3.zero;
        
        Vector3 center = Vector3.zero;
        foreach (GameObject crow in groupCrows)
        {
            if (crow != null)
            {
                center += crow.transform.position;
            }
        }
        return center / groupCrows.Count;
    }
    
    /// <summary>
    /// Gets the current number of crows in the scene
    /// </summary>
    public int GetCrowCount()
    {
        spawnedCrows.RemoveAll(crow => crow == null);
        return spawnedCrows.Count;
    }
    
    /// <summary>
    /// Coroutine that randomly selects a crow and plays its sound every 5-10 seconds
    /// </summary>
    private IEnumerator PlayRandomCrowSoundCoroutine()
    {
        while (true)
        {
            // Wait for random interval between min and max
            float waitTime = Random.Range(minSoundInterval, maxSoundInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Clean up null references
            spawnedCrows.RemoveAll(crow => crow == null);
            
            // Check if there are any crows in the scene
            if (spawnedCrows.Count == 0)
            {
                if (showDebug) Debug.Log("CrowSpawner: No crows in scene, skipping sound playback");
                continue;
            }
            
            // Select a random crow
            int randomIndex = Random.Range(0, spawnedCrows.Count);
            GameObject selectedCrow = spawnedCrows[randomIndex];
            
            if (selectedCrow == null)
            {
                continue;
            }
            
            // Get the crow controller and play its sound
            CrowController crowController = selectedCrow.GetComponent<CrowController>();
            if (crowController != null)
            {
                crowController.PlayCrowSound();
                if (showDebug) Debug.Log($"CrowSpawner: Playing sound on random crow {randomIndex + 1}/{spawnedCrows.Count}");
            }
        }
    }
    
    /// <summary>
    /// Sets up the AudioSource component for playing crow scatter sound
    /// </summary>
    private void SetupAudio()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound (or 1f for 3D)
        audioSource.volume = scatterSoundVolume;
    }
    
    /// <summary>
    /// Called when cannon is fired - plays crow scatter sound with cooldown
    /// </summary>
    private void OnCannonFired(Vector3 firePosition)
    {
        // Check if cooldown has expired
        if (!canPlayScatterSound)
        {
            if (Time.time < scatterSoundCooldownEndTime)
            {
                // Still in cooldown, don't play
                if (showDebug) Debug.Log($"CrowSpawner: Scatter sound in cooldown. Time remaining: {scatterSoundCooldownEndTime - Time.time:F1}s");
                return;
            }
            else
            {
                // Cooldown expired, can play again
                canPlayScatterSound = true;
            }
        }
        
        // Play the sound
        if (crowScatterSound != null && audioSource != null && canPlayScatterSound)
        {
            audioSource.PlayOneShot(crowScatterSound, scatterSoundVolume);
            
            // Set random cooldown duration
            float cooldownDuration = Random.Range(minScatterSoundCooldown, maxScatterSoundCooldown);
            scatterSoundCooldownEndTime = Time.time + cooldownDuration;
            canPlayScatterSound = false;
            
            if (showDebug) Debug.Log($"CrowSpawner: Playing crow scatter sound on cannon fire at {firePosition}. Next sound available in {cooldownDuration:F1}s");
        }
    }
    
    /// <summary>
    /// Validates that paths are properly set up
    /// </summary>
    private void ValidatePaths()
    {
        if (crowPaths == null)
        {
            Debug.LogWarning("CrowSpawner: Paths array is null! Path spawning will be disabled.");
            enablePathSpawning = false;
            return;
        }
        
        if (crowPaths.Length == 0)
        {
            Debug.LogWarning("CrowSpawner: No paths defined! Path spawning will be disabled.");
            enablePathSpawning = false;
            return;
        }
        
        int validPaths = 0;
        for (int i = 0; i < crowPaths.Length; i++)
        {
            // Initialize null elements
            if (crowPaths[i] == null)
            {
                crowPaths[i] = new CrowPath();
            }
            
            if (crowPaths[i].startPoint != null && crowPaths[i].endPoint != null)
            {
                validPaths++;
                if (string.IsNullOrEmpty(crowPaths[i].pathName))
                {
                    crowPaths[i].pathName = $"Path {i + 1}";
                }
            }
        }
        
        if (validPaths == 0)
        {
            Debug.LogWarning("CrowSpawner: No valid paths found! Please assign start and end points for at least one path.");
            enablePathSpawning = false;
        }
        else if (showDebug)
        {
            Debug.Log($"CrowSpawner: Found {validPaths} valid paths out of {crowPaths.Length} total paths");
        }
    }
    
    /// <summary>
    /// Coroutine that spawns groups of crows on paths at random intervals
    /// </summary>
    private IEnumerator SpawnPathCrowsCoroutine()
    {
        // Wait initial delay before first spawn
        yield return new WaitForSeconds(Random.Range(3f, 8f));
        
        while (true)
        {
            // Clean up destroyed crows
            spawnedCrows.RemoveAll(crow => crow == null);
            
            // Check if we can spawn more crows
            if (spawnedCrows.Count < maxCrowsInScene)
            {
                // Get available paths
                List<CrowPath> availablePaths = new List<CrowPath>();
                if (crowPaths != null)
                {
                    for (int i = 0; i < crowPaths.Length; i++)
                    {
                        if (crowPaths[i] != null && crowPaths[i].startPoint != null && crowPaths[i].endPoint != null)
                        {
                            availablePaths.Add(crowPaths[i]);
                        }
                    }
                }
                
                if (availablePaths.Count > 0)
                {
                    // Pick random path
                    CrowPath selectedPath = availablePaths[Random.Range(0, availablePaths.Count)];
                    
                    // Pick random group size
                    int groupSize = pathGroupSizes[Random.Range(0, pathGroupSizes.Length)];
                    
                    // Calculate how many we can actually spawn
                    int availableSlots = maxCrowsInScene - spawnedCrows.Count;
                    groupSize = Mathf.Min(groupSize, availableSlots);
                    
                    if (groupSize > 0)
                    {
                        SpawnPathGroup(selectedPath, groupSize);
                    }
                }
            }
            
            // Wait random interval before next spawn
            float waitTime = Random.Range(minPathSpawnInterval, maxPathSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    /// <summary>
    /// Spawns a group of crows on a path - all spawn at same position and move in same direction as a cluster
    /// </summary>
    private void SpawnPathGroup(CrowPath path, int groupSize)
    {
        if (path == null || path.startPoint == null || path.endPoint == null || crowPrefab == null)
        {
            if (showDebug) Debug.LogWarning("CrowSpawner: Cannot spawn path group - path or prefab is null");
            return;
        }
        
        // Create a new group
        int groupID = nextGroupID++;
        List<GameObject> groupCrows = new List<GameObject>();
        crowGroups[groupID] = groupCrows;
        
        Vector3 startPos = path.startPoint.position;
        Vector3 endPos = path.endPoint.position;
        Vector3 pathDirection = (endPos - startPos).normalized;
        float pathLength = Vector3.Distance(startPos, endPos);
        
        // Always spawn at the start of the path (spawnT = 0)
        float spawnT = 0f;
        Vector3 groupSpawnCenter = startPos;
        
        // Always move towards the end (direction = 1, towards end)
        Vector3 groupMoveDirection = pathDirection; // Always move from start to end
        
        // Calculate perpendicular direction for cluster spread
        Vector3 perpendicular = Vector3.Cross(pathDirection, Vector3.up).normalized;
        if (perpendicular == Vector3.zero) perpendicular = Vector3.Cross(pathDirection, Vector3.right).normalized;
        
        if (showDebug) Debug.Log($"CrowSpawner: Spawning path group {groupID} of {groupSize} crows on {path.pathName} at T={spawnT:F2}, moving from start to end");
        
        // Spawn all crows in the group with natural cluster formation (horizontal and vertical spread)
        for (int i = 0; i < groupSize; i++)
        {
            // Create natural cluster formation (some ahead, some behind, some to sides, some up/down)
            Vector3 clusterOffset = Vector3.zero;
            if (groupSize > 1)
            {
                // Distribute crows in 3D formation (left/right, forward/back, and up/down)
                // Spread them horizontally and vertically to fit 6-9 crows
                float forwardBackOffset = Random.Range(-2.5f, 2.5f); // Forward/back along path
                float sideOffset = Random.Range(-5f, 5f); // Left/right perpendicular to path (wider spread)
                float verticalOffset = Random.Range(-2f, 2f); // Up/down variation for top and bottom formation
                
                // Calculate offsets (horizontal and vertical)
                Vector3 forwardBack = pathDirection * forwardBackOffset;
                Vector3 side = perpendicular * sideOffset;
                Vector3 vertical = Vector3.up * verticalOffset;
                
                clusterOffset = forwardBack + side + vertical; // Include vertical component
            }
            
            Vector3 spawnPosition = groupSpawnCenter + clusterOffset;
            
            // All crows move in the same direction
            // Spawn crow with path information and cluster offset
            GameObject crow = SpawnCrowOnPath(spawnPosition, groupID, path, groupMoveDirection, spawnT, clusterOffset);
            groupCrows.Add(crow);
        }
    }
    
    /// <summary>
    /// Spawns a single crow on a path
    /// </summary>
    private GameObject SpawnCrowOnPath(Vector3 position, int groupID, CrowPath path, Vector3 moveDirection, float pathT, Vector3 clusterOffset = default)
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
            
            // Apply speed multiplier if different from default
            if (pathCrowSpeedMultiplier != 1f)
            {
                crowController.flySpeed *= pathCrowSpeedMultiplier;
                
                // Update normalSpeed using reflection (since it's private)
                var normalSpeedField = typeof(CrowController).GetField("normalSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (normalSpeedField != null)
                {
                    float currentNormalSpeed = (float)normalSpeedField.GetValue(crowController);
                    normalSpeedField.SetValue(crowController, currentNormalSpeed * pathCrowSpeedMultiplier);
                }
                
                // Update currentSpeed as well
                var currentSpeedField = typeof(CrowController).GetField("currentSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentSpeedField != null)
                {
                    float currentSpeedValue = (float)currentSpeedField.GetValue(crowController);
                    currentSpeedField.SetValue(crowController, currentSpeedValue * pathCrowSpeedMultiplier);
                }
            }
            
            // Set path information
            var pathField = typeof(CrowController).GetField("followPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pathStartField = typeof(CrowController).GetField("pathStart", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pathEndField = typeof(CrowController).GetField("pathEnd", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pathTField = typeof(CrowController).GetField("pathT", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pathDirectionField = typeof(CrowController).GetField("pathDirection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clusterOffsetField = typeof(CrowController).GetField("clusterOffset", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clusterSpeedVariationField = typeof(CrowController).GetField("clusterSpeedVariation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (pathField != null) pathField.SetValue(crowController, true);
            if (pathStartField != null) pathStartField.SetValue(crowController, path.startPoint.position);
            if (pathEndField != null) pathEndField.SetValue(crowController, path.endPoint.position);
            if (pathTField != null) pathTField.SetValue(crowController, pathT);
            if (pathDirectionField != null) pathDirectionField.SetValue(crowController, moveDirection);
            if (clusterOffsetField != null) clusterOffsetField.SetValue(crowController, clusterOffset);
            // Add speed variation for natural cluster movement (some crows slightly faster/slower)
            if (clusterSpeedVariationField != null) 
            {
                float speedVar = Random.Range(-1f, 1f); // Small speed variation
                clusterSpeedVariationField.SetValue(crowController, speedVar);
            }
            
            // Set group information
            if (groupID >= 0)
            {
                crowController.groupID = groupID;
                crowController.isInGroup = true;
                crowController.crowSpawner = this;
            }
            
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
        
        if (showDebug) Debug.Log($"CrowSpawner: Spawned path crow at {position} on {path.pathName} (T={pathT:F2}, Direction={moveDirection})");
        
        return crow;
    }
}
