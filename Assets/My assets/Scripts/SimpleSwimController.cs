using UnityEngine;

/// <summary>
/// Simple swim controller that moves forward automatically
/// Uses Rigidbody to work properly with Crest water interaction
/// Uses physics-based movement instead of transform.Translate to maintain proper velocity tracking
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleSwimController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of forward movement")]
    public float moveSpeed = 5f;
    
    [Header("Auto-Move")]
    [Tooltip("If enabled, character will automatically move forward")]
    public bool autoMoveForward = true;
    
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

    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("SimpleSwimController: No Rigidbody found! Adding Rigidbody component is required.");
            enabled = false;
            return;
        }
        
        // Configure Rigidbody for water physics compatibility
        rb.sleepThreshold = 0.0001f; // VERY LOW threshold to prevent sleep (critical for wake)
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
    }
    
    void Update()
    {
        // Rotate character to face movement direction
        if (rotateToMovementDirection && rb != null)
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
            Quaternion targetRotation = Quaternion.LookRotation(direction);
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
        
        // Calculate movement direction and apply movement
        if (autoMoveForward)
        {
            // Move forward in local space (along the transform's forward direction)
            Vector3 targetDirection = transform.forward;
            
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
            // Stop movement when auto-move is disabled
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
}
