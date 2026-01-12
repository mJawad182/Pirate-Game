using UnityEngine;

/// <summary>
/// Controls character movement using Rigidbody to work properly with Crest water interaction
/// Can move forward automatically or follow a defined path
/// Uses physics-based movement instead of transform.Translate to maintain proper velocity tracking
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SwimController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of forward movement")]
    public float moveSpeed = 5f;
    
    [Header("Movement Mode")]
    [Tooltip("Move forward automatically, or follow a path")]
    public MovementMode movementMode = MovementMode.Forward;
    
    [Header("Path Following")]
    [Tooltip("Path to follow (PathFollower component)")]
    public PathFollower pathToFollow;
    
    [Tooltip("Distance threshold to reach waypoint (m)")]
    [Range(0.1f, 5f)]
    public float waypointReachDistance = 1f;
    
    [Tooltip("Look ahead distance for smoother path following")]
    [Range(0.5f, 10f)]
    public float lookAheadDistance = 2f;
    
    [Header("Movement Mode")]
    [Tooltip("Use MovePosition (interpolated) or AddForce (physics-based). AddForce works better with water interaction.")]
    public bool useForceBasedMovement = true;
    
    [Tooltip("Acceleration multiplier for force-based movement (higher = faster response)")]
    [Range(1f, 50f)]
    public float accelerationMultiplier = 10f;
    
    [Header("Rotation Settings")]
    [Tooltip("Rotate character to face movement direction")]
    public bool rotateToMovementDirection = true;
    
    [Tooltip("Rotation speed (higher = faster rotation)")]
    [Range(0.5f, 10f)]
    public float rotationSpeed = 2f;
    
    [Tooltip("Flip rotation 180 degrees (if character model faces backwards)")]
    public bool flipRotation = false;
    
    [Header("Enviro Compatibility")]
    [Tooltip("Lock rotation to horizontal plane only (prevents Enviro from tilting the swimmer)")]
    public bool lockHorizontalRotation = true;
    
    [Tooltip("Use initial forward direction instead of current transform.forward (prevents weather changes from affecting direction)")]
    public bool useInitialForwardDirection = true;
    
    public enum MovementMode
    {
        Forward,
        FollowPath
    }

    private Rigidbody rb;
    private int currentWaypointIndex = 0;
    private Vector3 targetDirection;
    private Vector3 initialForwardDirection; // Store initial forward direction to prevent Enviro from changing it
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("SwimController: No Rigidbody found! Adding Rigidbody component is required.");
            enabled = false;
            return;
        }
        
        // Configure Rigidbody for water physics compatibility
        rb.sleepThreshold = 0.0001f; // VERY LOW threshold to prevent sleep (critical for wake)
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        
        // Store initial forward direction (horizontal only) to prevent Enviro weather changes from affecting it
        Vector3 forward = transform.forward;
        forward.y = 0;
        if (forward.magnitude > 0.01f)
        {
            initialForwardDirection = forward.normalized;
        }
        else
        {
            initialForwardDirection = Vector3.forward; // Fallback
        }
    }
    
    void Update()
    {
        // Update target direction based on movement mode
        if (movementMode == MovementMode.FollowPath && pathToFollow != null)
        {
            UpdatePathFollowing();
        }
        else
        {
            // Move forward - use initial direction if enabled (prevents Enviro weather changes from affecting direction)
            if (useInitialForwardDirection)
            {
                targetDirection = initialForwardDirection;
            }
            else
            {
                // Move forward in local space (along the transform's forward direction)
                // Extract only horizontal component to prevent diagonal movement
                Vector3 forward = transform.forward;
                forward.y = 0; // Remove vertical component
                if (forward.magnitude > 0.01f)
                {
                    targetDirection = forward.normalized;
                }
                else
                {
                    // Fallback if forward is pointing straight up/down
                    targetDirection = Vector3.forward;
                }
            }
        }
    }
    
    void LateUpdate()
    {
        // Rotate character to face movement direction (for forward mode)
        // Use LateUpdate to avoid conflicts with Enviro's LateUpdate
        if (rotateToMovementDirection && movementMode == MovementMode.Forward && rb != null)
        {
            UpdateRotation();
        }
    }
    
    void UpdateRotation()
    {
        // Get velocity direction (horizontal only)
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0;
        
        // Only rotate if moving
        if (velocity.magnitude > 0.1f)
        {
            Vector3 direction = velocity.normalized;
            
            // Flip direction if needed
            if (flipRotation)
            {
                direction = -direction;
            }
            
            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            
            // Lock rotation to horizontal plane if enabled (prevents Enviro interference)
            if (lockHorizontalRotation)
            {
                Vector3 euler = targetRotation.eulerAngles;
                euler.x = 0; // Lock pitch
                euler.z = 0; // Lock roll
                targetRotation = Quaternion.Euler(euler);
            }
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    void UpdatePathFollowing()
    {
        if (pathToFollow == null || pathToFollow.WaypointCount == 0)
        {
            // Fallback to forward - extract horizontal component
            Vector3 forward = transform.forward;
            forward.y = 0;
            targetDirection = forward.normalized;
            return;
        }
        
        // Get current target waypoint
        Vector3 targetWaypoint = pathToFollow.GetWaypoint(currentWaypointIndex);
        Vector3 currentPos = transform.position;
        
        // Calculate direction to waypoint
        Vector3 directionToWaypoint = (targetWaypoint - currentPos);
        directionToWaypoint.y = 0; // Keep horizontal only
        
        float distanceToWaypoint = directionToWaypoint.magnitude;
        
        // Check if we've reached the current waypoint
        if (distanceToWaypoint < waypointReachDistance)
        {
            // Move to next waypoint
            currentWaypointIndex++;
            
            // Check if we've completed the path
            if (currentWaypointIndex >= pathToFollow.WaypointCount)
            {
                pathToFollow.OnPathComplete();
                if (pathToFollow.loopPath)
                {
                    currentWaypointIndex = 0; // Loop back to start
                }
                else
                {
                    // Stop at end
                    targetDirection = Vector3.zero;
                    return;
                }
            }
            
            // Get new target waypoint
            targetWaypoint = pathToFollow.GetWaypoint(currentWaypointIndex);
            directionToWaypoint = (targetWaypoint - currentPos);
            directionToWaypoint.y = 0;
        }
        
        // Use look ahead for smoother following
        if (lookAheadDistance > 0 && distanceToWaypoint > lookAheadDistance)
        {
            Vector3 lookAheadPoint = currentPos + directionToWaypoint.normalized * lookAheadDistance;
            targetDirection = (lookAheadPoint - currentPos).normalized;
        }
        else
        {
            targetDirection = directionToWaypoint.normalized;
        }
        
        // Rotate character to face movement direction
        if (rotateToMovementDirection && targetDirection.magnitude > 0.1f)
        {
            Vector3 direction = targetDirection;
            
            // Flip direction if needed
            if (flipRotation)
            {
                direction = -direction;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            
            // Lock rotation to horizontal plane if enabled (prevents Enviro interference)
            if (lockHorizontalRotation)
            {
                Vector3 euler = targetRotation.eulerAngles;
                euler.x = 0; // Lock pitch
                euler.z = 0; // Lock roll
                targetRotation = Quaternion.Euler(euler);
            }
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    void FixedUpdate()
    {
        if (rb == null) return;
        
        // Keep Rigidbody awake to maintain proper velocity tracking
        if (rb.IsSleeping())
        {
            rb.WakeUp();
        }
        
        // Apply movement based on target direction
        if (targetDirection.magnitude > 0.1f)
        {
            if (useForceBasedMovement)
            {
                // Use AddForce - better for water interaction as it properly updates velocity
                // This ensures SphereWaterInteraction can track velocity correctly
                // Target velocity in X and Z only, preserve Y for FloatingObject
                Vector3 targetVelocity = targetDirection * moveSpeed;
                targetVelocity.y = rb.linearVelocity.y; // Preserve Y velocity for FloatingObject
                
                // Calculate force needed to reach target velocity (smoothly)
                Vector3 velocityDifference = targetVelocity - rb.linearVelocity;
                velocityDifference.y = 0; // Don't interfere with FloatingObject vertical forces
                
                // Apply force smoothly (scaled by mass for proper physics)
                // Using Acceleration mode which scales with mass automatically
                rb.AddForce(velocityDifference * accelerationMultiplier, ForceMode.Acceleration);
            }
            else
            {
                // Use MovePosition - smoother but can interfere with velocity tracking
                // Only move X and Z, let FloatingObject handle Y
                Vector3 movement = targetDirection * moveSpeed * Time.fixedDeltaTime;
                movement.y = 0; // Preserve Y for FloatingObject
                Vector3 targetPosition = transform.position + movement;
                
                // Preserve Y position for FloatingObject
                targetPosition.y = transform.position.y;
                
                rb.MovePosition(targetPosition);
            }
        }
        else
        {
            // Stop movement
            if (useForceBasedMovement)
            {
                // Dampen horizontal velocity
                Vector3 velocity = rb.linearVelocity;
                velocity.x *= 0.9f;
                velocity.z *= 0.9f;
                velocity.y = rb.linearVelocity.y; // Preserve Y for FloatingObject
                rb.linearVelocity = velocity;
            }
        }
    }
    
    /// <summary>
    /// Reset path following to start
    /// </summary>
    public void ResetPath()
    {
        currentWaypointIndex = 0;
    }
    
    /// <summary>
    /// Set path to follow
    /// </summary>
    public void SetPath(PathFollower path)
    {
        pathToFollow = path;
        currentWaypointIndex = 0;
        movementMode = MovementMode.FollowPath;
    }
}
