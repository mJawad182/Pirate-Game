using UnityEngine;

/// <summary>
/// Controls individual crow movement and destruction when out of range
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CrowController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Flying speed of the crow")]
    [Range(1f, 20f)]
    public float flySpeed = 5f;
    
    [Tooltip("Speed variation (random multiplier between 1-variation and 1+variation)")]
    [Range(0f, 0.5f)]
    public float speedVariation = 0.2f;
    
    [Tooltip("Rotation speed when changing direction")]
    [Range(1f, 10f)]
    public float rotationSpeed = 3f;
    
    [Header("Flight Behavior")]
    [Tooltip("How often the crow changes direction (seconds)")]
    [Range(1f, 10f)]
    public float directionChangeInterval = 3f;
    
    [Tooltip("Maximum angle change when changing direction (degrees)")]
    [Range(10f, 90f)]
    public float maxDirectionChange = 45f;
    
    [Tooltip("Vertical movement range (how much the crow can move up/down)")]
    [Range(0f, 10f)]
    public float verticalMovementRange = 3f;
    
    [Header("Height Constraints")]
    [Tooltip("Minimum height above camera to prevent crows from going underwater")]
    [Range(0f, 50f)]
    public float minHeightAboveCamera = 5f;
    
    [Tooltip("Minimum absolute Y position (world space). Set to a low value to use relative to camera instead.")]
    public float minAbsoluteHeight = -1000f;
    
    [Tooltip("Use absolute height instead of relative to camera")]
    public bool useAbsoluteHeight = false;
    
    [Header("Range Settings")]
    [Tooltip("Distance from camera before crow is destroyed")]
    [Range(50f, 500f)]
    public float destroyDistance = 100f;
    
    [Tooltip("Check distance every N seconds (for performance)")]
    [Range(0.1f, 2f)]
    public float distanceCheckInterval = 1f;
    
    [Header("Lifetime Settings")]
    [Tooltip("Enable lifetime/duration-based destruction")]
    public bool useLifetime = true;
    
    [Tooltip("How long the crow stays in scene before being destroyed (seconds). Set to 0 to disable.")]
    [Range(0f, 300f)]
    public float lifetime = 30f;
    
    [Tooltip("Random variation in lifetime (adds ±variation to lifetime)")]
    [Range(0f, 10f)]
    public float lifetimeVariation = 5f;
    
    [Header("Camera Reference")]
    [Tooltip("Main camera to measure distance from (auto-finds if not assigned)")]
    public Camera mainCamera;
    
    [Tooltip("Auto-find main camera if not assigned")]
    public bool autoFindCamera = true;
    
    [Header("Cannon Hit Reaction")]
    [Tooltip("Speed multiplier when reacting to cannon hits (how fast crow flees)")]
    [Range(1f, 5f)]
    public float cannonReactionSpeedMultiplier = 2f;
    
    [Tooltip("Duration of cannon reaction (how long crow flees before returning to normal)")]
    [Range(1f, 30f)]
    public float cannonReactionDuration = 5f;
    
    [Tooltip("Maximum distance from cannon fire/hit to react (crows beyond this distance won't react)")]
    [Range(10f, 1000f)]
    public float cannonReactionDistance = 500f;
    
    [Tooltip("Show gizmos in Scene view (reaction range, movement direction, etc.)")]
    public bool showGizmos = true;
    
    [Header("Group Movement")]
    [Tooltip("Strength of group cohesion (how much crow follows group direction)")]
    [Range(0f, 1f)]
    public float groupCohesion = 0.7f;
    
    [Tooltip("Strength of separation (how much crow avoids crowding others)")]
    [Range(0f, 1f)]
    public float groupSeparation = 0.3f;
    
    [Tooltip("Desired distance from other crows in group")]
    [Range(1f, 10f)]
    public float groupSeparationDistance = 3f;
    
    [Header("Audio")]
    [Tooltip("Audio clip to play (crow caw sound)")]
    public AudioClip crowSound;
    
    [Tooltip("Volume of the crow sound")]
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    // Group behavior fields (set by CrowSpawner)
    [HideInInspector]
    public int groupID = -1;
    
    [HideInInspector]
    public bool isInGroup = false;
    
    [HideInInspector]
    public Vector3 groupCenter;
    
    [HideInInspector]
    public Vector3 groupDirection;
    
    [HideInInspector]
    public CrowSpawner crowSpawner;
    
    private Rigidbody rb;
    private Vector3 currentDirection;
    private float currentSpeed;
    private float lastDirectionChangeTime;
    private float lastDistanceCheckTime;
    private CrowSpawner spawner;
    private float spawnTime;
    private float actualLifetime;
    private bool isReactingToCannon = false;
    private float cannonReactionEndTime = 0f;
    private float normalSpeed;
    private Vector3 cannonReactionDirection = Vector3.zero;
    private AudioSource audioSource;
    
    // Path following
    private bool followPath = false;
    private Vector3 pathStart = Vector3.zero;
    private Vector3 pathEnd = Vector3.zero;
    private float pathT = 0f; // Position along path (0 = start, 1 = end)
    private Vector3 pathDirection = Vector3.zero; // Direction along path (1 = towards end, -1 = towards start)
    private Vector3 clusterOffset = Vector3.zero; // Offset from path center for cluster formation
    private float clusterSpeedVariation = 0f; // Speed variation for natural cluster movement
    
    void Start()
    {
        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure Rigidbody for flying
        rb.useGravity = false;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
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
            Debug.LogWarning("CrowController: Main camera not found! Distance checking will not work.");
        }
        
        // Find spawner to register with
        spawner = FindObjectOfType<CrowSpawner>();
        if (spawner == null && crowSpawner != null)
        {
            spawner = crowSpawner;
        }
        
        // Record spawn time for lifetime tracking
        spawnTime = Time.time;
        
        // Calculate actual lifetime with variation
        if (useLifetime && lifetime > 0f)
        {
            actualLifetime = lifetime + Random.Range(-lifetimeVariation, lifetimeVariation);
            actualLifetime = Mathf.Max(0.1f, actualLifetime); // Ensure minimum lifetime
            if (showDebug) Debug.Log($"CrowController: Crow lifetime set to {actualLifetime:F1} seconds");
        }
        else
        {
            actualLifetime = float.MaxValue; // Disable lifetime destruction
        }
        
        // Initialize random direction and speed
        InitializeMovement();
        
        // Store normal speed
        normalSpeed = currentSpeed;
        
        // Subscribe to cannon fire and hit events (react to both)
        // Subscribe even if EventManager.Instance is null (static events work without instance)
        EventManager.OnCannonFired += OnCannonFired;
        EventManager.OnCannonHit += OnCannonHit;
        if (showDebug) Debug.Log($"CrowController: Subscribed to cannon fire/hit events. Reaction Distance: {cannonReactionDistance}m");
        
        // Setup audio source for playing sounds
        SetupAudio();
        
        if (showDebug) Debug.Log($"CrowController: Initialized crow at {transform.position}, Reaction Distance: {cannonReactionDistance}m");
    }
    
    /// <summary>
    /// Sets up the AudioSource component for playing crow sounds
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
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 500f;
        audioSource.volume = soundVolume;
    }
    
    /// <summary>
    /// Plays the crow sound (called by CrowSpawner)
    /// </summary>
    public void PlayCrowSound()
    {
        if (crowSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(crowSound, soundVolume);
            if (showDebug) Debug.Log($"CrowController: Playing crow sound");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from cannon events
        EventManager.OnCannonFired -= OnCannonFired;
        EventManager.OnCannonHit -= OnCannonHit;
    }
    
    /// <summary>
    /// Initializes the crow's movement with random direction and speed
    /// </summary>
    private void InitializeMovement()
    {
        // Random horizontal direction
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        currentDirection = new Vector3(Mathf.Cos(randomAngle), Random.Range(-0.2f, 0.2f), Mathf.Sin(randomAngle)).normalized;
        
        // Random speed with variation
        float speedMultiplier = 1f + Random.Range(-speedVariation, speedVariation);
        currentSpeed = flySpeed * speedMultiplier;
        
        // Initialize timing
        lastDirectionChangeTime = Time.time;
        lastDistanceCheckTime = Time.time;
        
        // Set initial rotation
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection);
        }
    }
    
    void FixedUpdate()
    {
        // Check if cannon reaction has ended
        if (isReactingToCannon && Time.time >= cannonReactionEndTime)
        {
            isReactingToCannon = false;
            currentSpeed = normalSpeed;
            cannonReactionDirection = Vector3.zero;
            if (showDebug) Debug.Log("CrowController: Cannon reaction ended, returning to normal flight");
        }
        
        // Move the crow
        MoveCrow();
        
        // Check lifetime
        if (useLifetime && lifetime > 0f)
        {
            CheckLifetimeAndDestroy();
        }
        
        // Check distance periodically
        if (Time.time - lastDistanceCheckTime >= distanceCheckInterval)
        {
            CheckDistanceAndDestroy();
            lastDistanceCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// Moves the crow in its current direction
    /// </summary>
    private void MoveCrow()
    {
        // PRIORITY 1: If reacting to cannon, disable path following and scatter immediately
        if (isReactingToCannon)
        {
            // Disable path following when scattering (ensure it's off)
            followPath = false;
            
            // Keep direction locked to reaction direction
            if (cannonReactionDirection != Vector3.zero)
            {
                currentDirection = cannonReactionDirection;
            }
        }
        // PRIORITY 2: Follow path only if enabled and NOT reacting to cannon
        else if (followPath && pathStart != Vector3.zero && pathEnd != Vector3.zero)
        {
            FollowPath();
            return; // Path following handles movement, skip normal movement code
        }
        // Don't change direction during cannon reaction (crow is fleeing)
        else if (Time.time - lastDirectionChangeTime >= directionChangeInterval)
        {
            ChangeDirection();
            lastDirectionChangeTime = Time.time;
        }
        
        // Apply vertical variation (less during cannon reaction)
        Vector3 movementDirection = currentDirection;
        if (!isReactingToCannon)
        {
            float verticalVariation = Mathf.Sin(Time.time * 0.5f) * verticalMovementRange * 0.1f;
            movementDirection.y += verticalVariation;
        }
        
        // Apply group behavior if in a group (disabled during cannon reaction)
        if (isInGroup && !isReactingToCannon)
        {
            movementDirection = ApplyGroupBehavior(movementDirection);
        }
        
        movementDirection.Normalize();
        
        // Move using Rigidbody
        Vector3 movement = movementDirection * currentSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = rb.position + movement;
        
        // Enforce minimum height constraint
        newPosition = EnforceMinimumHeight(newPosition);
        
        rb.MovePosition(newPosition);
        
        // Adjust direction if we hit the height limit (prevent downward movement when too low)
        if (newPosition.y <= GetMinimumHeight())
        {
            // If moving downward, adjust direction upward
            if (movementDirection.y < 0)
            {
                movementDirection.y = Mathf.Abs(movementDirection.y) * 0.5f; // Push upward
                movementDirection.Normalize();
                currentDirection = movementDirection;
                // Update reaction direction too if reacting
                if (isReactingToCannon)
                {
                    cannonReactionDirection = movementDirection;
                }
            }
        }
        
        // Rotate towards movement direction - faster rotation during cannon reaction
        if (movementDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            // Use faster rotation during cannon reaction (3x speed for quick but smooth turn)
            float effectiveRotationSpeed = isReactingToCannon ? rotationSpeed * 3f : rotationSpeed;
            Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, effectiveRotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothedRotation);
        }
    }
    
    /// <summary>
    /// Applies group behavior: cohesion and separation
    /// </summary>
    private Vector3 ApplyGroupBehavior(Vector3 baseDirection)
    {
        if (crowSpawner == null || groupID < 0) return baseDirection;
        
        Vector3 groupBehaviorDirection = baseDirection;
        
        // Get group members
        System.Collections.Generic.List<GameObject> groupMembers = crowSpawner.GetGroupCrows(groupID);
        if (groupMembers.Count <= 1) return baseDirection; // No other members
        
        // Get current group center
        Vector3 currentGroupCenter = crowSpawner.GetGroupCenter(groupID);
        if (currentGroupCenter == Vector3.zero) currentGroupCenter = groupCenter;
        
        // Cohesion: steer towards group center and follow group direction
        Vector3 toGroupCenter = (currentGroupCenter - transform.position).normalized;
        Vector3 cohesionDirection = Vector3.Lerp(baseDirection, toGroupCenter + groupDirection.normalized * 0.5f, groupCohesion * 0.5f).normalized;
        
        // Separation: avoid crowding nearby group members
        Vector3 separationDirection = Vector3.zero;
        int nearbyCount = 0;
        
        foreach (GameObject otherCrow in groupMembers)
        {
            if (otherCrow == null || otherCrow == gameObject) continue;
            
            Vector3 offset = transform.position - otherCrow.transform.position;
            float distance = offset.magnitude;
            
            if (distance > 0 && distance < groupSeparationDistance)
            {
                separationDirection += offset.normalized / distance; // Closer = stronger separation
                nearbyCount++;
            }
        }
        
        if (nearbyCount > 0)
        {
            separationDirection /= nearbyCount;
            separationDirection.Normalize();
            groupBehaviorDirection = Vector3.Lerp(cohesionDirection, separationDirection, groupSeparation).normalized;
        }
        else
        {
            groupBehaviorDirection = cohesionDirection;
        }
        
        // Update current direction to gradually align with group behavior
        currentDirection = Vector3.Lerp(currentDirection, groupBehaviorDirection, Time.fixedDeltaTime * 2f).normalized;
        
        return groupBehaviorDirection;
    }
    
    /// <summary>
    /// Called when cannon is fired - makes crow flee away from firing position
    /// </summary>
    private void OnCannonFired(Vector3 firePosition)
    {
        // Check if crow is within reaction distance
        float distanceToFire = Vector3.Distance(transform.position, firePosition);
        
        if (showDebug) Debug.Log($"[CROW REACTION] Crow at {transform.position} received cannon fire event at {firePosition}. Distance: {distanceToFire:F1}m, Reaction Distance: {cannonReactionDistance}m, FollowPath: {followPath}");
        
        if (distanceToFire > cannonReactionDistance)
        {
            // Too far, don't react
            if (showDebug) Debug.Log($"[CROW REACTION] Crow too far from cannon fire ({distanceToFire:F1}m > {cannonReactionDistance}m), not reacting");
            return;
        }
        
        // Disable path following immediately when cannon fires (scatter from path)
        followPath = false;
        
        // Start or extend reaction
        isReactingToCannon = true;
        cannonReactionEndTime = Time.time + cannonReactionDuration;
        
        // Calculate direction away from firing position
        Vector3 crowPosition = transform.position;
        Vector3 directionAwayFromFire = (crowPosition - firePosition).normalized;
        
        // If direction is too horizontal, add some upward bias to prevent going underwater
        if (Mathf.Abs(directionAwayFromFire.y) < 0.2f)
        {
            directionAwayFromFire.y = 0.3f; // Add upward component
            directionAwayFromFire.Normalize();
        }
        
        // Store reaction direction
        cannonReactionDirection = directionAwayFromFire;
        
        // Set new direction and speed
        currentDirection = directionAwayFromFire;
        currentSpeed = normalSpeed * cannonReactionSpeedMultiplier;
        
        Debug.Log($"[CROW SCATTER] Crow at {transform.position} SCATTERING from cannon fire at {firePosition}! Distance: {distanceToFire:F1}m, FollowPath disabled, Fleeing at {currentSpeed:F1} speed for {cannonReactionDuration}s");
        if (showDebug) Debug.Log($"CrowController: Reacting to cannon fire at {firePosition}! Distance: {distanceToFire:F1}m, Fleeing at {currentSpeed:F1} speed for {cannonReactionDuration}s");
    }
    
    /// <summary>
    /// Called when cannon bullet hits - makes crow flee away from hit position
    /// </summary>
    private void OnCannonHit(Vector3 hitPosition)
    {
        // Check if crow is within reaction distance
        float distanceToHit = Vector3.Distance(transform.position, hitPosition);
        
        if (showDebug) Debug.Log($"[CROW REACTION] Crow at {transform.position} received cannon hit event at {hitPosition}. Distance: {distanceToHit:F1}m, Reaction Distance: {cannonReactionDistance}m, FollowPath: {followPath}");
        
        if (distanceToHit > cannonReactionDistance)
        {
            // Too far, don't react
            if (showDebug) Debug.Log($"[CROW REACTION] Crow too far from cannon hit ({distanceToHit:F1}m > {cannonReactionDistance}m), not reacting");
            return;
        }
        
        // Disable path following immediately when cannon hits (scatter from path)
        followPath = false;
        
        // Start or extend reaction
        isReactingToCannon = true;
        cannonReactionEndTime = Time.time + cannonReactionDuration;
        
        // Calculate direction away from hit position
        Vector3 crowPosition = transform.position;
        Vector3 directionAwayFromHit = (crowPosition - hitPosition).normalized;
        
        // If direction is too horizontal, add some upward bias to prevent going underwater
        if (Mathf.Abs(directionAwayFromHit.y) < 0.2f)
        {
            directionAwayFromHit.y = 0.3f; // Add upward component
            directionAwayFromHit.Normalize();
        }
        
        // Store reaction direction
        cannonReactionDirection = directionAwayFromHit;
        
        // Set new direction and speed
        currentDirection = directionAwayFromHit;
        currentSpeed = normalSpeed * cannonReactionSpeedMultiplier;
        
        Debug.Log($"[CROW SCATTER] Crow at {transform.position} SCATTERING from cannon hit at {hitPosition}! Distance: {distanceToHit:F1}m, FollowPath disabled, Fleeing at {currentSpeed:F1} speed for {cannonReactionDuration}s");
        if (showDebug) Debug.Log($"CrowController: Reacting to cannon hit at {hitPosition}! Distance: {distanceToHit:F1}m, Fleeing at {currentSpeed:F1} speed for {cannonReactionDuration}s");
    }
    
    /// <summary>
    /// Gets the minimum allowed height for the crow
    /// </summary>
    private float GetMinimumHeight()
    {
        if (useAbsoluteHeight)
        {
            return minAbsoluteHeight;
        }
        else
        {
            // Relative to camera
            if (mainCamera != null)
            {
                return mainCamera.transform.position.y + minHeightAboveCamera;
            }
            return transform.position.y; // Fallback to current position if no camera
        }
    }
    
    /// <summary>
    /// Enforces minimum height constraint on position
    /// </summary>
    private Vector3 EnforceMinimumHeight(Vector3 position)
    {
        float minHeight = GetMinimumHeight();
        
        if (position.y < minHeight)
        {
            position.y = minHeight;
            if (showDebug && Time.frameCount % 60 == 0) // Log once per second approximately
            {
                Debug.Log($"CrowController: Crow height constrained to minimum {minHeight:F1}m (was {position.y:F1}m)");
            }
        }
        
        return position;
    }
    
    /// <summary>
    /// Changes the crow's direction randomly
    /// </summary>
    private void ChangeDirection()
    {
        // Calculate random direction change
        float angleChange = Random.Range(-maxDirectionChange, maxDirectionChange) * Mathf.Deg2Rad;
        float verticalChange = Random.Range(-0.3f, 0.3f);
        
        // Check if crow is too low - if so, bias direction upward
        float currentHeight = transform.position.y;
        float minHeight = GetMinimumHeight();
        float heightDifference = currentHeight - minHeight;
        
        // If too close to minimum height, prevent downward movement
        if (heightDifference < 3f && verticalChange < 0)
        {
            verticalChange = Mathf.Abs(verticalChange); // Make it upward instead
            if (showDebug && Time.frameCount % 60 == 0)
            {
                Debug.Log($"CrowController: Crow too low ({currentHeight:F1}m), forcing upward direction change");
            }
        }
        
        // Rotate current direction
        Quaternion rotation = Quaternion.Euler(0, angleChange * Mathf.Rad2Deg, 0);
        Vector3 newDirection = rotation * currentDirection;
        newDirection.y += verticalChange;
        newDirection.Normalize();
        
        currentDirection = newDirection;
        
        // Occasionally change speed slightly
        if (Random.value < 0.3f)
        {
            float speedMultiplier = 1f + Random.Range(-speedVariation, speedVariation);
            currentSpeed = flySpeed * speedMultiplier;
        }
    }
    
    /// <summary>
    /// Makes the crow follow its assigned path with cluster formation
    /// </summary>
    private void FollowPath()
    {
        Vector3 pathVector = pathEnd - pathStart;
        float pathLength = pathVector.magnitude;
        
        if (pathLength < 0.01f) return; // Invalid path
        
        Vector3 normalizedPath = pathVector.normalized;
        
        // Calculate movement along path with speed variation for natural cluster movement
        float effectiveSpeed = currentSpeed + clusterSpeedVariation;
        float moveDistance = effectiveSpeed * Time.fixedDeltaTime;
        float moveT = moveDistance / pathLength;
        
        // Determine direction: pathDirection should be normalized direction vector
        // If pathDirection dot normalizedPath > 0, moving towards end, else towards start
        bool movingTowardsEnd = Vector3.Dot(pathDirection, normalizedPath) > 0;
        
        // Update path position - always move towards end (no reversing)
        pathT += moveT;
        
        // If reached end, destroy the crow (don't reverse)
        if (pathT >= 1f)
        {
            pathT = 1f;
            // Destroy crow when it reaches the end
            if (spawner != null)
            {
                spawner.RemoveCrow(gameObject);
            }
            Destroy(gameObject);
            return;
        }
        
        // Clamp pathT
        pathT = Mathf.Clamp01(pathT);
        
        // Calculate base position along path
        Vector3 basePosition = Vector3.Lerp(pathStart, pathEnd, pathT);
        
        // Apply cluster offset for natural formation (some crows ahead, some behind, some to sides)
        Vector3 targetPosition = basePosition + clusterOffset;
        
        // Add subtle movement variation for natural cluster behavior (horizontal and vertical)
        if (isInGroup)
        {
            // Small random offset that changes slowly for natural movement (includes vertical component)
            float timeOffset = Time.time * 0.3f + (groupID * 10f); // Different phase for each group
            Vector3 naturalVariation = new Vector3(
                Mathf.Sin(timeOffset) * 0.5f,
                Mathf.Cos(timeOffset * 0.7f) * 0.3f, // Vertical variation for natural cluster movement
                Mathf.Sin(timeOffset * 1.3f) * 0.4f
            );
            targetPosition += naturalVariation;
        }
        
        // Set direction to be along the path (with slight variation for natural look)
        Vector3 pathDir = pathDirection.normalized;
        if (isInGroup && clusterOffset.magnitude > 0.1f)
        {
            // Slight direction adjustment based on cluster offset for more natural movement
            Vector3 offsetDirection = clusterOffset.normalized * 0.1f;
            currentDirection = (pathDir + offsetDirection).normalized;
        }
        else
        {
            currentDirection = pathDir;
        }
        
        // Move to target position
        rb.MovePosition(targetPosition);
        
        // Rotate towards movement direction
        if (currentDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
            Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothedRotation);
        }
    }
    
    /// <summary>
    /// Checks lifetime and destroys crow if time has expired
    /// </summary>
    private void CheckLifetimeAndDestroy()
    {
        float timeAlive = Time.time - spawnTime;
        
        if (timeAlive >= actualLifetime)
        {
            if (showDebug) Debug.Log($"CrowController: Crow destroyed - lifetime expired ({timeAlive:F1}s >= {actualLifetime:F1}s)");
            
            // Notify spawner if it exists
            if (spawner != null)
            {
                spawner.RemoveCrow(gameObject);
            }
            
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Checks distance from camera and destroys crow if too far
    /// </summary>
    private void CheckDistanceAndDestroy()
    {
        if (mainCamera == null)
            return;
        
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        
        if (distance > destroyDistance)
        {
            if (showDebug) Debug.Log($"CrowController: Crow destroyed - too far from camera ({distance:F1}m > {destroyDistance}m)");
            
            // Notify spawner if it exists
            if (spawner != null)
            {
                spawner.RemoveCrow(gameObject);
            }
            
            Destroy(gameObject);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw cannon reaction distance sphere (yellow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow with transparency
        Gizmos.DrawWireSphere(transform.position, cannonReactionDistance);
        
        // Draw movement direction (blue)
        if (Application.isPlaying && currentDirection != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, currentDirection * 5f);
        }
        
        // Draw reaction direction if reacting (red)
        if (Application.isPlaying && isReactingToCannon && cannonReactionDirection != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, cannonReactionDirection * 8f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw destroy distance sphere (red, only when selected)
        if (mainCamera != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red with transparency
            Gizmos.DrawWireSphere(mainCamera.transform.position, destroyDistance);
        }
        
        // Draw cannon reaction distance sphere with fill (yellow, only when selected)
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Yellow with more transparency for fill
        Gizmos.DrawSphere(transform.position, cannonReactionDistance);
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f); // Yellow wireframe
        Gizmos.DrawWireSphere(transform.position, cannonReactionDistance);
        
        // Draw movement direction (blue, thicker when selected)
        if (Application.isPlaying && currentDirection != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, currentDirection * 8f);
        }
        
        // Draw reaction direction if reacting (red, thicker when selected)
        if (Application.isPlaying && isReactingToCannon && cannonReactionDirection != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, cannonReactionDirection * 10f);
        }
    }
}
