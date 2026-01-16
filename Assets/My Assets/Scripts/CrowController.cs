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
    
    [Header("Cannon Fire Reaction")]
    [Tooltip("Speed multiplier when reacting to cannon fire (how fast crow flees)")]
    [Range(1f, 5f)]
    public float cannonReactionSpeedMultiplier = 2f;
    
    [Tooltip("Duration of cannon reaction (how long crow flees before returning to normal)")]
    [Range(1f, 30f)]
    public float cannonReactionDuration = 5f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private Rigidbody rb;
    private Vector3 currentDirection;
    private float currentSpeed;
    private float lastDirectionChangeTime;
    private float lastDistanceCheckTime;
    private CrowSpawner spawner;
    private float spawnTime;
    private float actualLifetime;
    private bool hasReactedToCannon = false;
    private bool isReactingToCannon = false;
    private float cannonReactionEndTime = 0f;
    private float normalSpeed;
    
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
        
        // Subscribe to cannon fire event
        EventManager.OnCannonFired += OnCannonFired;
        
        if (showDebug) Debug.Log($"CrowController: Initialized crow at {transform.position}");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from cannon fire event
        EventManager.OnCannonFired -= OnCannonFired;
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
        // Don't change direction during cannon reaction (crow is fleeing)
        if (!isReactingToCannon && Time.time - lastDirectionChangeTime >= directionChangeInterval)
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
            }
        }
        
        // Rotate towards movement direction
        if (movementDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothedRotation);
        }
    }
    
    /// <summary>
    /// Called when cannon is fired - makes crow flee away from camera
    /// </summary>
    private void OnCannonFired()
    {
        // Only react once per crow
        if (hasReactedToCannon)
        {
            if (showDebug) Debug.Log("CrowController: Crow already reacted to cannon fire, ignoring");
            return;
        }
        
        if (mainCamera == null)
        {
            if (showDebug) Debug.LogWarning("CrowController: Cannot react to cannon - main camera not found");
            return;
        }
        
        // Mark as reacted
        hasReactedToCannon = true;
        isReactingToCannon = true;
        cannonReactionEndTime = Time.time + cannonReactionDuration;
        
        // Calculate direction away from camera
        Vector3 crowPosition = transform.position;
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 directionAwayFromCamera = (crowPosition - cameraPosition).normalized;
        
        // Ensure horizontal component (keep some upward bias to avoid going underwater)
        directionAwayFromCamera.y = Mathf.Max(0.1f, directionAwayFromCamera.y); // Keep some upward component
        directionAwayFromCamera.Normalize();
        
        // Set new direction and speed
        currentDirection = directionAwayFromCamera;
        currentSpeed = normalSpeed * cannonReactionSpeedMultiplier;
        
        if (showDebug) Debug.Log($"CrowController: Reacting to cannon fire! Fleeing away from camera at {currentSpeed:F1} speed for {cannonReactionDuration}s");
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
    
    void OnDrawGizmosSelected()
    {
        // Draw destroy distance sphere
        if (mainCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mainCamera.transform.position, destroyDistance);
        }
        
        // Draw movement direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, currentDirection * 5f);
    }
}
