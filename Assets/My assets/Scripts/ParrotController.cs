using UnityEngine;

/// <summary>
/// Controls parrot behavior - flies towards hat, picks it up, and stops
/// Attach this to the parrot prefab GameObject
/// </summary>
public class ParrotController : MonoBehaviour
{
    // Event fired when parrot is destroyed
    public static System.Action OnParrotDestroyed;
    [Header("References")]
    [Tooltip("The hat GameObject that the parrot should fly towards and pick up")]
    public GameObject hatObject;
    
    [Tooltip("Return position after picking up the hat")]
    public Transform returnPosition;
    
    [Header("Movement Settings")]
    [Tooltip("Speed at which the parrot flies towards the hat")]
    [Range(1f, 20f)]
    public float flySpeed = 8f;
    
    [Tooltip("Rotation speed for facing the hat")]
    [Range(1f, 20f)]
    public float rotationSpeed = 5f;
    
    [Tooltip("Distance threshold to pick up the hat (meters)")]
    [Range(0.1f, 2f)]
    public float pickupDistance = 0.5f;
    
    [Tooltip("U-turn radius (how wide the turn is)")]
    [Range(1f, 10f)]
    public float uTurnRadius = 3f;
    
    [Tooltip("U-turn speed multiplier (how fast the turn is)")]
    [Range(0.5f, 3f)]
    public float uTurnSpeed = 1.5f;
    
    [Tooltip("Vertical smoothness for going up/down (higher = smoother)")]
    [Range(0.1f, 5f)]
    public float verticalSmoothness = 2f;
    
    [Tooltip("Upward offset when picking up hat (to avoid going underwater)")]
    [Range(0f, 10f)]
    public float pickupHeightOffset = 2.5f;
    
    [Tooltip("Minimum height above hat when picking up (meters)")]
    [Range(0f, 10f)]
    public float minHeightAboveHat = 1.5f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private bool hasPickedUpHat = false;
    private bool isReturning = false;
    private bool isUTurning = false;
    private Rigidbody parrotRigidbody;
    private Vector3 uTurnCenter;
    private Vector3 uTurnStartDirection;
    private float uTurnProgress = 0f;
    
    void Start()
    {
        
        // Get or add Rigidbody for physics-based movement
        parrotRigidbody = GetComponent<Rigidbody>();
        if (parrotRigidbody == null)
        {
            parrotRigidbody = gameObject.AddComponent<Rigidbody>();
            parrotRigidbody.useGravity = false; // Parrot flies, doesn't fall
            parrotRigidbody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        }
        
        // Try to find hat by tag if not assigned
        if (hatObject == null)
        {
            GameObject hatByTag = GameObject.FindGameObjectWithTag("Hat");
            if (hatByTag != null)
            {
                hatObject = hatByTag;
                if (showDebug) Debug.Log("ParrotController: Found hat by tag 'Hat'");
            }
        }
        
        if (hatObject == null)
        {
            Debug.LogWarning("ParrotController: Hat object not assigned and could not be found by tag 'Hat'!");
        }
        
        // Try to find return position by tag if not assigned
        if (returnPosition == null)
        {
            GameObject returnByTag = GameObject.FindGameObjectWithTag("Return");
            if (returnByTag != null)
            {
                returnPosition = returnByTag.transform;
                if (showDebug) Debug.Log("ParrotController: Found return position by tag 'Return'");
            }
        }
        
        if (returnPosition == null)
        {
            Debug.LogWarning("ParrotController: Return position not assigned and could not be found by tag 'Return'!");
        }
    }
    
    void Update()
    {
        if (isUTurning)
        {
            PerformUTurn();
            return;
        }
        
        if (isReturning)
        {
            MoveToReturnPosition();
            return;
        }
        
        if (hasPickedUpHat) return;
        if (hatObject == null) return;
        
        // Move towards hat and face it
        MoveTowardsHat();
    }
    
    /// <summary>
    /// Moves the parrot towards the hat and faces it with smooth vertical path
    /// </summary>
    private void MoveTowardsHat()
    {
        if (hatObject == null) return;
        
        Vector3 hatPosition = hatObject.transform.position;
        Vector3 parrotPosition = transform.position;
        
        // Calculate horizontal direction to hat
        Vector3 horizontalDirection = hatPosition - parrotPosition;
        horizontalDirection.y = 0; // Keep horizontal
        float horizontalDistance = horizontalDirection.magnitude;
        
        // Calculate vertical difference for smooth descent
        float verticalDifference = hatPosition.y - parrotPosition.y;
        
        // Calculate horizontal distance (for pickup check)
        float horizontalDistanceToHat = horizontalDistance;
        
        // Check if close enough horizontally to pick up hat (use horizontal distance, not 3D)
        // This allows pickup even if parrot is slightly above hat
        if (horizontalDistanceToHat <= pickupDistance)
        {
            PickUpHat();
            return;
        }
        
        // Normalize horizontal direction
        if (horizontalDistance > 0.1f)
        {
            horizontalDirection.Normalize();
        }
        else
        {
            horizontalDirection = transform.forward;
            horizontalDirection.y = 0;
            horizontalDirection.Normalize();
        }
        
        // Smooth vertical movement - gradually descend towards hat, but maintain minimum height above hat
        // Calculate desired height (hat position + minimum height offset)
        float desiredHeight = hatPosition.y + minHeightAboveHat;
        float heightDifference = desiredHeight - parrotPosition.y;
        
        // Add upward offset when close to hat to avoid going underwater
        float proximityFactor = Mathf.Clamp01(1f - (horizontalDistanceToHat / (pickupDistance * 4f))); // Start lifting when within 4x pickup distance
        float additionalLift = pickupHeightOffset * proximityFactor;
        
        // Use the higher of: maintaining minimum height OR following hat with offset
        float targetHeightDifference = Mathf.Max(heightDifference, verticalDifference + additionalLift);
        
        float verticalSpeed = Mathf.Clamp(targetHeightDifference * verticalSmoothness, -flySpeed, flySpeed);
        Vector3 verticalMovement = Vector3.up * verticalSpeed * Time.deltaTime;
        
        // Horizontal movement
        Vector3 horizontalMovement = horizontalDirection * flySpeed * Time.deltaTime;
        
        // Combine movements
        Vector3 movement = horizontalMovement + verticalMovement;
        
        // Calculate target direction (including vertical component for smooth look)
        Vector3 targetDirection = (hatPosition - parrotPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        // Apply movement
        transform.position += movement;
        
        // Update Rigidbody position if using physics
        if (parrotRigidbody != null && !parrotRigidbody.isKinematic)
        {
            parrotRigidbody.MovePosition(transform.position);
        }
    }
    
    /// <summary>
    /// Performs a smooth U-turn like a bird - rotates from current position
    /// </summary>
    private void PerformUTurn()
    {
        uTurnProgress += uTurnSpeed * Time.deltaTime;
        
        if (uTurnProgress >= 1f)
        {
            // U-turn complete, start returning
            isUTurning = false;
            isReturning = true;
            uTurnProgress = 0f;
            if (showDebug) Debug.Log("ParrotController: U-turn complete, returning to position");
            return;
        }
        
        // Calculate rotation angle (180 degree turn)
        float rotationAngle = Mathf.Lerp(0f, 180f, uTurnProgress);
        
        // Rotate smoothly from current forward direction
        Quaternion startRotation = Quaternion.LookRotation(uTurnStartDirection);
        Quaternion endRotation = Quaternion.LookRotation(uTurnStartDirection) * Quaternion.Euler(0f, 180f, 0f);
        Quaternion currentRotation = Quaternion.Lerp(startRotation, endRotation, uTurnProgress);
        
        // Apply rotation first
        transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation, rotationSpeed * 2f * Time.deltaTime);
        
        // Move forward along the arc while rotating
        // Calculate forward movement based on current rotation
        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0; // Keep horizontal
        forwardDirection.Normalize();
        
        // Move forward along the arc path
        Vector3 movement = forwardDirection * flySpeed * Time.deltaTime;
        transform.position += movement;
        
        // Update Rigidbody if using physics
        if (parrotRigidbody != null && !parrotRigidbody.isKinematic)
        {
            parrotRigidbody.MovePosition(transform.position);
        }
    }
    
    /// <summary>
    /// Moves parrot smoothly back to return position (going up)
    /// </summary>
    private void MoveToReturnPosition()
    {
        if (returnPosition == null)
        {
            if (showDebug) Debug.LogWarning("ParrotController: Return position not assigned!");
            return;
        }
        
        Vector3 targetPosition = returnPosition.position;
        Vector3 currentPosition = transform.position;
        
        // Calculate direction
        Vector3 direction = targetPosition - currentPosition;
        float distance = direction.magnitude;
        
        // Check if reached return position
        if (distance < 0.5f)
        {
            // Reached destination - destroy the parrot
            if (parrotRigidbody != null)
            {
                parrotRigidbody.linearVelocity = Vector3.zero;
                parrotRigidbody.isKinematic = true;
            }
            if (showDebug) Debug.Log("ParrotController: Reached return position! Destroying parrot.");
            
            // Fire event before destroying
            OnParrotDestroyed?.Invoke();
            
            Destroy(gameObject);
            return;
        }
        
        // Normalize direction
        direction.Normalize();
        
        // Smooth vertical ascent - gradually climb up
        float verticalDifference = targetPosition.y - currentPosition.y;
        float verticalSpeed = Mathf.Clamp(verticalDifference * verticalSmoothness, -flySpeed, flySpeed);
        Vector3 verticalMovement = Vector3.up * verticalSpeed * Time.deltaTime;
        
        // Horizontal movement
        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();
        Vector3 horizontalMovement = horizontalDirection * flySpeed * Time.deltaTime;
        
        // Combine movements
        Vector3 movement = horizontalMovement + verticalMovement;
        
        // Face the target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        // Apply movement
        transform.position += movement;
        
        // Update Rigidbody if using physics
        if (parrotRigidbody != null && !parrotRigidbody.isKinematic)
        {
            parrotRigidbody.MovePosition(transform.position);
        }
    }
    
    /// <summary>
    /// Picks up the hat - makes hat a child of parrot and starts U-turn
    /// </summary>
    private void PickUpHat()
    {
        if (hatObject == null || hasPickedUpHat) return;
        
        // Make hat a child of parrot
        hatObject.transform.SetParent(transform);
        
        // Set hat local position (Z axis: 3.45)
        hatObject.transform.localPosition = new Vector3(0f, -0.3f, 14.3f);
        
        // Set hat local rotation (X axis: 58.39 degrees)
        hatObject.transform.localRotation = Quaternion.Euler(58.39f, 0f, 0f);
        
        // Disable hat physics (stop it from falling)
        Rigidbody hatRigidbody = hatObject.GetComponent<Rigidbody>();
        if (hatRigidbody != null)
        {
            hatRigidbody.isKinematic = true;
            hatRigidbody.linearVelocity = Vector3.zero;
            hatRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Mark hat as picked up
        hasPickedUpHat = true;
        
        // Start U-turn
        StartUTurn();
        
        if (showDebug) Debug.Log("ParrotController: Parrot picked up the hat, starting U-turn!");
    }
    
    /// <summary>
    /// Starts the U-turn maneuver
    /// </summary>
    private void StartUTurn()
    {
        isUTurning = true;
        uTurnProgress = 0f;
        
        // Store current forward direction
        uTurnStartDirection = transform.forward;
        uTurnStartDirection.y = 0;
        uTurnStartDirection.Normalize();
        
        // Calculate U-turn center (perpendicular to current direction, offset by radius)
        Vector3 perpendicular = Vector3.Cross(uTurnStartDirection, Vector3.up).normalized;
        uTurnCenter = transform.position + perpendicular * uTurnRadius;
        
        // Keep U-turn center at current Y level
        uTurnCenter.y = transform.position.y;
    }
}
