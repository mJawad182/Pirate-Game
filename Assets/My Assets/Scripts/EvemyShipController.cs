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
    
    [Tooltip("Distance at which to start smooth interpolation to stop position")]
    [Range(1f, 20f)]
    public float smoothApproachDistance = 5f;
    
    [Tooltip("Arrival range/radius - ship triggers arrival event when within this distance of stop position")]
    [Range(0.1f, 10f)]
    public float arrivalRange = 2f;
    
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
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private bool shouldMove = false;
    private bool hasArrived = false;
    private Camera mainCamera;
    private float lastDebugLogTime = 0f;
    private const float DEBUG_LOG_INTERVAL = 2f; // Log distance every 2 seconds
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
    }
    
    void OnDestroy()
    {
        // Unsubscribe from event (static event, can unsubscribe without Instance)
        EventManager.OnParrotDestroyed -= StartMovingTowardsPlayer;
    }

    // Update is called once per frame
    void Update()
    {
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
    /// Moves the ship towards the stop position, updating all axes including Y
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
            Debug.Log($"[SHIP {shipID} PROGRESS] Distance to stop: {distance:F2}m, Position: {currentPosition}, Stop Position: {targetPosition}, Arrival Range: {arrivalRange}m");
            lastDebugLogTime = Time.time;
        }
        
        // Fire arrival event when within arrival range (only once)
        if (!hasArrived && distance <= arrivalRange)
        {
            hasArrived = true;
            shouldMove = false;
            FireShipArrivalEvent();
            Debug.Log($"[SHIP {shipID} ARRIVED] Ship {shipID} reached stop position! Distance: {distance:F2}m (within range of {arrivalRange}m)");
            return;
        }
        
        // Stop moving if already arrived
        if (hasArrived)
        {
            shouldMove = false;
            return;
        }
        
        // Use smooth interpolation when close to stop position
        if (distance <= smoothApproachDistance)
        {
            // Smooth Lerp approach - prevents oscillation
            float lerpFactor = smoothApproachSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(currentPosition, targetPosition, lerpFactor);
        }
        else
        {
            // Normal movement when far away
            direction.Normalize();
            Vector3 movement = direction * movementSpeed * Time.deltaTime;
            transform.position += movement;
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
}
