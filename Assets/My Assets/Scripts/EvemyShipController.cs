using UnityEngine;

public class EvemyShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the enemy ship moves towards the stop position (units per second)")]
    [Range(0.1f, 50f)]
    public float movementSpeed = 5f;
    
    [Tooltip("Smooth interpolation speed when approaching stop position (higher = faster, smoother)")]
    [Range(0.1f, 10f)]
    public float smoothApproachSpeed = 2f;
    
    [Tooltip("Distance at which to start smooth deceleration (ship will slow down gradually)")]
    [Range(5f, 30f)]
    public float decelerationStartDistance = 10f;
    
    [Tooltip("Arrival range/radius - ship triggers arrival event when within this distance of stop position")]
    [Range(0.1f, 50f)]
    public float arrivalRange = 20f;
    
    [Tooltip("Minimum distance before ship stops completely (prevents jitter)")]
    [Range(0.01f, 1f)]
    public float stopThreshold = 0.1f;
    
    [Tooltip("Rotation speed for facing the player")]
    [Range(1f, 20f)]
    public float rotationSpeed = 5f;
    
    [Tooltip("Stop position where the ship will halt (assign in Inspector)")]
    public Transform stopPosition;
    
    [Tooltip("Target to face towards (Main Camera/Player - will find automatically if not set)")]
    public Transform targetToFace;
    
    [Header("Ship Identification")]
    [Tooltip("Ship ID (1-4) - determines which event to fire when ship arrives")]
    [Range(1, 4)]
    public int shipID = 1; // Made public so CannonFireInputHandler can find ships by ID
    
    [Header("Crest Floating Object")]
    [Tooltip("Crest FloatingObject component (will auto-find if not assigned)")]
    public WaveHarmonic.Crest.FloatingObject floatingObject;
    
    [Tooltip("Distance from player to storm threshold (when storm is within this, set forceStrength to 10)")]
    [Range(50f, 500f)]
    public float stormDistanceThreshold = 200f;
    
    [Tooltip("Speed of forceStrength fluctuation (how fast it oscillates between 9-11)")]
    [Range(1f, 20f)]
    public float fluctuationSpeed = 5f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private bool shouldMove = false;
    private bool hasArrived = false;
    private Camera mainCamera;
    private float lastDebugLogTime = 0f;
    private const float DEBUG_LOG_INTERVAL = 2f; // Log distance every 2 seconds
    
    // Crest floating object control
    private WeatherProgressionController weatherController;
    private Transform stormTransform;
    private float originalForceStrength = 10f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Validate ship ID
        if (shipID < 1 || shipID > 4)
        {
            Debug.LogError($"[SHIP {shipID} ERROR] Invalid shipID {shipID}! Must be between 1-4. GameObject: {gameObject.name}");
        }
        else
        {
            Debug.Log($"[SHIP {shipID} INIT] Ship {shipID} initialized. GameObject: {gameObject.name}, Stop Position: {(stopPosition != null ? stopPosition.name : "NOT ASSIGNED")}");
        }
        
        // Find main camera if target not set
        if (targetToFace == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                targetToFace = mainCamera.transform;
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetToFace = player.transform;
                }
            }
        }
        
        // Subscribe to EventManager's OnParrotDestroyed event (static event, can subscribe without Instance)
        EventManager.OnParrotDestroyed += StartMovingTowardsPlayer;
        if (showDebug) Debug.Log("EvemyShipController: Subscribed to EventManager.OnParrotDestroyed event");
        
        // Find WeatherProgressionController to get storm transform reference
        weatherController = FindObjectOfType<WeatherProgressionController>();
        if (weatherController != null && weatherController.transformToMove != null)
        {
            stormTransform = weatherController.transformToMove;
            if (showDebug) Debug.Log($"EvemyShipController: Found storm transform: {stormTransform.name}");
        }
        else
        {
            if (showDebug) Debug.LogWarning("EvemyShipController: WeatherProgressionController or storm transform not found. ForceStrength fluctuation may not work.");
        }
        
        // Find Crest FloatingObject component
        if (floatingObject == null)
        {
            floatingObject = GetComponent<WaveHarmonic.Crest.FloatingObject>();
            if (floatingObject == null)
            {
                floatingObject = GetComponentInChildren<WaveHarmonic.Crest.FloatingObject>();
            }
        }
        
        if (floatingObject != null)
        {
            originalForceStrength = floatingObject.BuoyancyForceStrength;
            if (showDebug) Debug.Log($"EvemyShipController: Found FloatingObject component. Original forceStrength: {originalForceStrength}");
        }
        else
        {
            Debug.LogWarning($"EvemyShipController: FloatingObject component not found on ship {shipID}! Cannot fluctuate forceStrength.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from event (static event, can unsubscribe without Instance)
        EventManager.OnParrotDestroyed -= StartMovingTowardsPlayer;
    }

    // Update is called once per frame
    void Update()
    {
        // Check arrival status continuously (even if not moving) - this ensures event fires reliably
        if (!hasArrived && stopPosition != null)
        {
            CheckArrivalStatus();
        }
        
        // Move towards stop position (only when shouldMove is true)
        if (shouldMove)
        {
            if (stopPosition != null)
            {
                MoveTowardsStopPosition();
            }
            else
            {
                if (showDebug) Debug.LogWarning("EvemyShipController: Stop position not assigned! Ship cannot move.");
            }
        }
        
        // Face towards player (always, even after reaching stop position)
        if (targetToFace != null)
        {
            FaceTowardsPlayer();
        }
        
        // Fluctuate Crest FloatingObject forceStrength when ship is stopped and storm is approaching
        if (hasArrived && !shouldMove && floatingObject != null)
        {
            UpdateFloatingObjectForceStrength();
        }
    }
    
    /// <summary>
    /// Checks if ship has arrived within range and fires event if so (called every frame)
    /// </summary>
    private void CheckArrivalStatus()
    {
        if (stopPosition == null || hasArrived) return;
        
        Vector3 targetPosition = stopPosition.position;
        Vector3 currentPosition = transform.position;
        float distance = Vector3.Distance(currentPosition, targetPosition);
        
        // Fire arrival event when within arrival range (only once)
        // Use a small tolerance to account for floating point precision
        if (distance <= arrivalRange + 0.4f)
        {
            hasArrived = true;
            shouldMove = false; // Stop movement when arrival event fires
            FireShipArrivalEvent();
            Debug.Log($"[SHIP {shipID} ARRIVED] Ship {shipID} reached stop position! Distance: {distance:F2}m (within range of {arrivalRange}m). Movement stopped, event fired.");
        }
        // If ship is close but not quite there, ensure it keeps moving
        else if (distance > arrivalRange && !shouldMove && distance < arrivalRange * 2f)
        {
            // Re-enable movement if ship stopped prematurely but is still close
            shouldMove = true;
            Debug.Log($"[SHIP {shipID} RECOVERY] Ship {shipID} was stopped but is close ({distance:F2}m). Re-enabling movement to reach arrival range.");
        }
    }
    
    /// <summary>
    /// Called when parrot is destroyed - starts enemy ship movement
    /// </summary>
    private void StartMovingTowardsPlayer()
    {
        shouldMove = true;
        if (showDebug) Debug.Log("EvemyShipController: Parrot destroyed! Enemy ship starting to move towards player.");
    }
    
    /// <summary>
    /// Moves the ship towards the stop position with smooth deceleration
    /// </summary>
    private void MoveTowardsStopPosition()
    {
        if (stopPosition == null)
        {
            Debug.LogWarning($"EvemyShipController: Ship {shipID} - Stop position is null!");
            return;
        }
        
        Vector3 targetPosition = stopPosition.position;
        Vector3 currentPosition = transform.position;
        Vector3 direction = (targetPosition - currentPosition);
        float distance = direction.magnitude;
        
        // Debug logging periodically to track ship progress
        if (Time.time - lastDebugLogTime >= DEBUG_LOG_INTERVAL)
        {
            Debug.Log($"[SHIP {shipID} PROGRESS] Distance to stop: {distance:F2}m, Position: {currentPosition}, Stop Position: {targetPosition}, Arrival Range: {arrivalRange}m, HasArrived: {hasArrived}");
            lastDebugLogTime = Time.time;
        }
        
        // Stop completely if very close to target (prevents jitter)
        // BUT only if we've already fired the arrival event
        if (distance <= stopThreshold && hasArrived)
        {
            transform.position = targetPosition; // Snap to exact position
            shouldMove = false;
            return;
        }
        // If we're very close but haven't fired arrival event yet, fire it now
        else if (distance <= stopThreshold && !hasArrived)
        {
            hasArrived = true;
            shouldMove = false;
            transform.position = targetPosition; // Snap to exact position
            FireShipArrivalEvent();
            Debug.Log($"[SHIP {shipID} ARRIVED] Ship {shipID} reached stop position! Distance: {distance:F2}m (within stopThreshold). Event fired.");
            return;
        }
        
        // Calculate speed multiplier based on distance (smooth deceleration curve)
        float speedMultiplier = 1f;
        if (distance <= decelerationStartDistance)
        {
            // Smooth deceleration: speed reduces as distance decreases
            // Use a smooth curve (ease-out) for natural deceleration
            float normalizedDistance = Mathf.Clamp01(distance / decelerationStartDistance);
            // Ease-out curve: starts fast, slows down smoothly
            speedMultiplier = normalizedDistance * normalizedDistance; // Quadratic ease-out
            // Ensure minimum speed for smooth approach
            speedMultiplier = Mathf.Max(speedMultiplier, 0.1f);
        }
        
        // Calculate effective speed
        float effectiveSpeed = movementSpeed * speedMultiplier;
        
        // Move towards target with smooth deceleration
        if (distance > stopThreshold)
        {
            direction.Normalize();
            Vector3 movement = direction * effectiveSpeed * Time.deltaTime;
            
            // Don't overshoot the target
            if (movement.magnitude > distance)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position += movement;
            }
        }
    }
    
    /// <summary>
    /// Rotates the ship to face towards the player
    /// </summary>
    private void FaceTowardsPlayer()
    {
        Vector3 directionToPlayer = (targetToFace.position - transform.position);
        directionToPlayer.y = 0; // Keep rotation horizontal (only Y-axis rotation)
        
        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Fires the appropriate arrival event based on ship ID
    /// </summary>
    private void FireShipArrivalEvent()
    {
        Debug.Log($"[SHIP ARRIVAL] Ship {shipID} is firing arrival event! Position: {transform.position}, Distance to stop: {Vector3.Distance(transform.position, stopPosition != null ? stopPosition.position : Vector3.zero)}");
        
        switch (shipID)
        {
            case 1:
                Debug.Log("[SHIP ARRIVAL] Firing OnEnemyShip1Arrive event");
                EventManager.OnEnemyShip1Arrive?.Invoke();
                break;
            case 2:
                Debug.Log("[SHIP ARRIVAL] Firing OnEnemyShip2Arrive event");
                EventManager.OnEnemyShip2Arrive?.Invoke();
                break;
            case 3:
                Debug.Log("[SHIP ARRIVAL] Firing OnEnemyShip3Arrive event");
                EventManager.OnEnemyShip3Arrive?.Invoke();
                break;
            case 4:
                Debug.Log("[SHIP ARRIVAL] Firing OnEnemyShip4Arrive event");
                EventManager.OnEnemyShip4Arrive?.Invoke();
                break;
            default:
                Debug.LogWarning($"EvemyShipController: Invalid ship ID {shipID}! Must be 1-4.");
                break;
        }
    }
    
    /// <summary>
    /// Updates Crest FloatingObject forceStrength based on storm distance
    /// Fluctuates between 9-11 when storm is approaching, sets to 10 when storm is in range
    /// </summary>
    private void UpdateFloatingObjectForceStrength()
    {
        if (floatingObject == null) return;
        
        // Check if storm is available
        if (stormTransform == null || mainCamera == null)
        {
            // Try to find main camera if not set
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // Try to get storm transform from weather controller
            if (stormTransform == null && weatherController != null && weatherController.transformToMove != null)
            {
                stormTransform = weatherController.transformToMove;
            }
            
            // If still no storm transform, skip
            if (stormTransform == null || mainCamera == null)
            {
                return;
            }
        }
        
        // Calculate distance from player/camera to storm
        float stormDistance = Vector3.Distance(mainCamera.transform.position, stormTransform.position);
        
        // When storm is in range (within threshold), set forceStrength to 10
        if (stormDistance <= stormDistanceThreshold)
        {
            floatingObject.BuoyancyForceStrength = 10f;
            
            if (showDebug && Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.Log($"[SHIP {shipID} FLOATING] Storm in range ({stormDistance:F1}m). ForceStrength set to 10");
            }
        }
        else
        {
            // Storm is approaching but not in range - fluctuate between 9 and 11 rapidly
            float fluctuation = Mathf.Sin(Time.time * fluctuationSpeed) * 1f; // Oscillates between -1 and 1
            float forceStrength = 10f + fluctuation; // Oscillates between 9 and 11
            floatingObject.BuoyancyForceStrength = forceStrength;
            
            if (showDebug && Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.Log($"[SHIP {shipID} FLOATING] Storm approaching ({stormDistance:F1}m). ForceStrength fluctuating: {forceStrength:F2}");
            }
        }
    }
}
