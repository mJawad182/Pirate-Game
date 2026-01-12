using UnityEngine;

/// <summary>
/// Controls ship floating animation on the sea with configurable speed and range.
/// Handles vertical bobbing, pitch, roll, and yaw animations for realistic ship movement.
/// </summary>
public class ShipFloatingController : MonoBehaviour
{
    [Header("Vertical Floating (Bobbing)")]
    [Tooltip("Enable vertical bobbing animation")]
    public bool enableVerticalBobbing = true;
    
    [Tooltip("Range (amplitude) of vertical movement in meters")]
    [Range(0f, 5f)]
    public float verticalRange = 0.5f;
    
    [Tooltip("Speed of vertical bobbing animation")]
    [Range(0.1f, 5f)]
    public float verticalSpeed = 1f;
    
    [Tooltip("Vertical offset from base position")]
    public float verticalOffset = 0f;
    
    [Header("Horizontal Movement (Drift/Sway)")]
    [Tooltip("Enable horizontal X/Z drift animation (swaying on waves)")]
    public bool enableHorizontalDrift = false;
    
    [Tooltip("Range of horizontal drift in meters (X axis)")]
    [Range(0f, 5f)]
    public float horizontalXRange = 0.3f;
    
    [Tooltip("Range of horizontal drift in meters (Z axis)")]
    [Range(0f, 5f)]
    public float horizontalZRange = 0.3f;
    
    [Tooltip("Speed of horizontal drift animation")]
    [Range(0.1f, 5f)]
    public float horizontalSpeed = 0.6f;
    
    [Tooltip("Allow external scripts to update X/Z position (disables horizontal drift)")]
    public bool allowExternalPositionControl = false;
    
    [Header("Pitch Animation (Forward/Backward Tilt)")]
    [Tooltip("Enable pitch rotation animation")]
    public bool enablePitch = true;
    
    [Tooltip("Range of pitch rotation in degrees")]
    [Range(0f, 30f)]
    public float pitchRange = 5f;
    
    [Tooltip("Speed of pitch animation")]
    [Range(0.1f, 5f)]
    public float pitchSpeed = 0.8f;
    
    [Tooltip("Pitch offset in degrees")]
    public float pitchOffset = 0f;
    
    [Header("Roll Animation (Left/Right Tilt)")]
    [Tooltip("Enable roll rotation animation")]
    public bool enableRoll = true;
    
    [Tooltip("Range of roll rotation in degrees")]
    [Range(0f, 30f)]
    public float rollRange = 8f;
    
    [Tooltip("Speed of roll animation")]
    [Range(0.1f, 5f)]
    public float rollSpeed = 1.2f;
    
    [Tooltip("Roll offset in degrees")]
    public float rollOffset = 0f;
    
    [Header("Yaw Animation (Rotation)")]
    [Tooltip("Enable yaw rotation animation")]
    public bool enableYaw = false;
    
    [Tooltip("Range of yaw rotation in degrees")]
    [Range(0f, 15f)]
    public float yawRange = 2f;
    
    [Tooltip("Speed of yaw animation")]
    [Range(0.1f, 5f)]
    public float yawSpeed = 0.5f;
    
    [Tooltip("Yaw offset in degrees")]
    public float yawOffset = 0f;
    
    [Header("Animation Settings")]
    [Tooltip("Use different wave frequencies for more natural movement")]
    public bool useMultipleWaves = true;
    
    [Tooltip("Second wave frequency multiplier (for complex motion)")]
    [Range(0.1f, 2f)]
    public float secondaryWaveMultiplier = 0.7f;
    
    [Tooltip("Second wave amplitude multiplier")]
    [Range(0f, 1f)]
    public float secondaryWaveAmplitude = 0.5f;
    
    [Tooltip("Smoothing factor for animation transitions (0 = no smoothing, 1 = maximum smoothing)")]
    [Range(0f, 1f)]
    public float smoothing = 0.1f;
    
    [Header("Advanced Settings")]
    [Tooltip("Use physics-based movement (requires Rigidbody)")]
    public bool usePhysics = false;
    
    [Tooltip("Base position reference (leave empty to use Start position)")]
    public Transform basePositionReference;
    
    [Tooltip("Apply animation relative to this transform (useful for nested objects)")]
    public Transform animationParent;
    
    [Header("Debug")]
    [Tooltip("Show debug information and gizmos")]
    public bool showDebug = false;
    
    // Private variables
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    
    private float verticalTime = 0f;
    private float pitchTime = 0f;
    private float rollTime = 0f;
    private float yawTime = 0f;
    private float horizontalTime = 0f;
    
    private Rigidbody rb;
    private bool hasRigidbody = false;
    
    void Start()
    {
        // Store base position and rotation
        if (basePositionReference != null)
        {
            basePosition = basePositionReference.position;
            baseRotation = basePositionReference.rotation;
        }
        else
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }
        
        currentPosition = basePosition;
        currentRotation = baseRotation;
        
        // Check for Rigidbody
        rb = GetComponent<Rigidbody>();
        hasRigidbody = (rb != null);
        
        // Initialize time offsets for varied animation
        verticalTime = Random.Range(0f, Mathf.PI * 2f);
        pitchTime = Random.Range(0f, Mathf.PI * 2f);
        rollTime = Random.Range(0f, Mathf.PI * 2f);
        yawTime = Random.Range(0f, Mathf.PI * 2f);
        horizontalTime = Random.Range(0f, Mathf.PI * 2f);
        
        if (showDebug)
        {
            Debug.Log($"ShipFloatingController initialized. Base Position: {basePosition}, Has Rigidbody: {hasRigidbody}");
        }
    }
    
    void Update()
    {
        if (!usePhysics)
        {
            // Update animation time
            float deltaTime = Time.deltaTime;
            verticalTime += deltaTime * verticalSpeed;
            pitchTime += deltaTime * pitchSpeed;
            rollTime += deltaTime * rollSpeed;
            yawTime += deltaTime * yawSpeed;
            if (enableHorizontalDrift && !allowExternalPositionControl)
            {
                horizontalTime += deltaTime * horizontalSpeed;
            }
            
            // Calculate new position and rotation
            Vector3 targetPosition = CalculateTargetPosition();
            Quaternion targetRotation = CalculateTargetRotation();
            
            // Apply smoothing
            if (smoothing > 0f)
            {
                currentPosition = Vector3.Lerp(currentPosition, targetPosition, 1f - smoothing);
                currentRotation = Quaternion.Lerp(currentRotation, targetRotation, 1f - smoothing);
            }
            else
            {
                currentPosition = targetPosition;
                currentRotation = targetRotation;
            }
            
            // Apply to transform
            if (animationParent != null)
            {
                animationParent.localPosition = currentPosition - basePosition;
                animationParent.localRotation = Quaternion.Inverse(baseRotation) * currentRotation;
            }
            else
            {
                transform.position = currentPosition;
                transform.rotation = currentRotation;
            }
        }
    }
    
    void FixedUpdate()
    {
        if (usePhysics && hasRigidbody)
        {
            // Update animation time
            float deltaTime = Time.fixedDeltaTime;
            verticalTime += deltaTime * verticalSpeed;
            pitchTime += deltaTime * pitchSpeed;
            rollTime += deltaTime * rollSpeed;
            yawTime += deltaTime * yawSpeed;
            if (enableHorizontalDrift && !allowExternalPositionControl)
            {
                horizontalTime += deltaTime * horizontalSpeed;
            }
            
            // Calculate target position and rotation
            Vector3 targetPosition = CalculateTargetPosition();
            Quaternion targetRotation = CalculateTargetRotation();
            
            // Apply forces/velocities to move towards target
            Vector3 positionDifference = targetPosition - transform.position;
            Vector3 velocity = positionDifference / Time.fixedDeltaTime;
            
            // Apply smoothing to velocity
            if (smoothing > 0f)
            {
                velocity *= (1f - smoothing);
            }
            
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocity.y, rb.linearVelocity.z);
            
            // Apply rotation
            Quaternion rotationDifference = targetRotation * Quaternion.Inverse(transform.rotation);
            rotationDifference.ToAngleAxis(out float angle, out Vector3 axis);
            
            if (angle > 180f) angle -= 360f;
            
            Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime);
            
            // Apply smoothing to angular velocity
            if (smoothing > 0f)
            {
                angularVelocity *= (1f - smoothing);
            }
            
            rb.angularVelocity = angularVelocity;
        }
    }
    
    /// <summary>
    /// Calculate target position based on animation parameters
    /// </summary>
    Vector3 CalculateTargetPosition()
    {
        Vector3 position = basePosition;
        
        // Vertical movement (Y axis)
        if (enableVerticalBobbing)
        {
            float verticalOffset = 0f;
            
            if (useMultipleWaves)
            {
                // Primary wave
                float primaryWave = Mathf.Sin(verticalTime) * verticalRange;
                // Secondary wave for more complex motion
                float secondaryWave = Mathf.Sin(verticalTime * secondaryWaveMultiplier) * verticalRange * secondaryWaveAmplitude;
                verticalOffset = primaryWave + secondaryWave;
            }
            else
            {
                verticalOffset = Mathf.Sin(verticalTime) * verticalRange;
            }
            
            position.y = basePosition.y + verticalOffset + this.verticalOffset;
        }
        else
        {
            position.y = basePosition.y + verticalOffset;
        }
        
        // Horizontal movement (X and Z axis) - only if enabled and not externally controlled
        if (enableHorizontalDrift && !allowExternalPositionControl)
        {
            float xOffset = 0f;
            float zOffset = 0f;
            
            if (useMultipleWaves)
            {
                // Use different phases for X and Z to create circular/elliptical drift
                float primaryX = Mathf.Sin(horizontalTime) * horizontalXRange;
                float secondaryX = Mathf.Sin(horizontalTime * secondaryWaveMultiplier) * horizontalXRange * secondaryWaveAmplitude;
                xOffset = primaryX + secondaryX;
                
                // Z uses cosine for 90-degree phase shift (circular motion)
                float primaryZ = Mathf.Cos(horizontalTime) * horizontalZRange;
                float secondaryZ = Mathf.Cos(horizontalTime * secondaryWaveMultiplier) * horizontalZRange * secondaryWaveAmplitude;
                zOffset = primaryZ + secondaryZ;
            }
            else
            {
                xOffset = Mathf.Sin(horizontalTime) * horizontalXRange;
                zOffset = Mathf.Cos(horizontalTime) * horizontalZRange;
            }
            
            position.x = basePosition.x + xOffset;
            position.z = basePosition.z + zOffset;
        }
        else if (allowExternalPositionControl)
        {
            // If external control is enabled, use current transform position for X/Z
            // This allows other scripts (like ShipController) to move the ship
            position.x = transform.position.x;
            position.z = transform.position.z;
        }
        // If horizontal drift is disabled and external control is disabled, X/Z stay at basePosition (fixed)
        
        return position;
    }
    
    /// <summary>
    /// Calculate target rotation based on animation parameters
    /// </summary>
    Quaternion CalculateTargetRotation()
    {
        Quaternion rotation = baseRotation;
        
        float pitch = 0f;
        float roll = 0f;
        float yaw = 0f;
        
        if (enablePitch)
        {
            if (useMultipleWaves)
            {
                float primaryPitch = Mathf.Sin(pitchTime) * pitchRange;
                float secondaryPitch = Mathf.Sin(pitchTime * secondaryWaveMultiplier) * pitchRange * secondaryWaveAmplitude;
                pitch = primaryPitch + secondaryPitch + pitchOffset;
            }
            else
            {
                pitch = Mathf.Sin(pitchTime) * pitchRange + pitchOffset;
            }
        }
        else
        {
            pitch = pitchOffset;
        }
        
        if (enableRoll)
        {
            if (useMultipleWaves)
            {
                float primaryRoll = Mathf.Sin(rollTime) * rollRange;
                float secondaryRoll = Mathf.Sin(rollTime * secondaryWaveMultiplier) * rollRange * secondaryWaveAmplitude;
                roll = primaryRoll + secondaryRoll + rollOffset;
            }
            else
            {
                roll = Mathf.Sin(rollTime) * rollRange + rollOffset;
            }
        }
        else
        {
            roll = rollOffset;
        }
        
        if (enableYaw)
        {
            if (useMultipleWaves)
            {
                float primaryYaw = Mathf.Sin(yawTime) * yawRange;
                float secondaryYaw = Mathf.Sin(yawTime * secondaryWaveMultiplier) * yawRange * secondaryWaveAmplitude;
                yaw = primaryYaw + secondaryYaw + yawOffset;
            }
            else
            {
                yaw = Mathf.Sin(yawTime) * yawRange + yawOffset;
            }
        }
        else
        {
            yaw = yawOffset;
        }
        
        // Apply rotations in order: pitch, roll, yaw
        rotation *= Quaternion.Euler(pitch, yaw, roll);
        
        return rotation;
    }
    
    /// <summary>
    /// Reset animation to base position and rotation
    /// </summary>
    public void ResetToBase()
    {
        verticalTime = 0f;
        pitchTime = 0f;
        rollTime = 0f;
        yawTime = 0f;
        
        currentPosition = basePosition;
        currentRotation = baseRotation;
        
        if (!usePhysics)
        {
            transform.position = basePosition;
            transform.rotation = baseRotation;
        }
    }
    
    /// <summary>
    /// Set new base position (useful for dynamic positioning)
    /// </summary>
    public void SetBasePosition(Vector3 newBasePosition)
    {
        basePosition = newBasePosition;
    }
    
    /// <summary>
    /// Set new base position but keep current X/Z if external control is enabled
    /// </summary>
    public void SetBasePositionKeepHorizontal(Vector3 newBasePosition)
    {
        if (allowExternalPositionControl)
        {
            basePosition = new Vector3(transform.position.x, newBasePosition.y, transform.position.z);
        }
        else
        {
            basePosition = newBasePosition;
        }
    }
    
    /// <summary>
    /// Update base position to current position (useful when ship moves externally)
    /// </summary>
    public void UpdateBasePosition()
    {
        basePosition = transform.position;
        baseRotation = transform.rotation;
    }
    
    /// <summary>
    /// Set new base rotation (useful for dynamic rotation)
    /// </summary>
    public void SetBaseRotation(Quaternion newBaseRotation)
    {
        baseRotation = newBaseRotation;
    }
    
    /// <summary>
    /// Get current animation progress (0 to 1) for vertical bobbing
    /// </summary>
    public float GetVerticalAnimationProgress()
    {
        return (Mathf.Sin(verticalTime) + 1f) * 0.5f;
    }
    
    /// <summary>
    /// Get current animation progress (0 to 1) for pitch
    /// </summary>
    public float GetPitchAnimationProgress()
    {
        return (Mathf.Sin(pitchTime) + 1f) * 0.5f;
    }
    
    /// <summary>
    /// Get current animation progress (0 to 1) for roll
    /// </summary>
    public float GetRollAnimationProgress()
    {
        return (Mathf.Sin(rollTime) + 1f) * 0.5f;
    }
    
    void OnDrawGizmosSelected()
    {
        if (showDebug)
        {
            // Draw base position
            Gizmos.color = Color.green;
            Vector3 basePos = basePositionReference != null ? basePositionReference.position : transform.position;
            Gizmos.DrawWireSphere(basePos, 0.2f);
            
            // Draw vertical range
            if (enableVerticalBobbing)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(
                    basePos + Vector3.down * verticalRange,
                    basePos + Vector3.up * verticalRange
                );
                Gizmos.DrawWireSphere(basePos + Vector3.down * verticalRange, 0.1f);
                Gizmos.DrawWireSphere(basePos + Vector3.up * verticalRange, 0.1f);
            }
            
            // Draw rotation ranges
            Gizmos.color = Color.yellow;
            float gizmoLength = 2f;
            
            if (enablePitch)
            {
                Vector3 forward = transform.forward;
                Gizmos.DrawLine(basePos, basePos + Quaternion.Euler(pitchRange, 0, 0) * forward * gizmoLength);
                Gizmos.DrawLine(basePos, basePos + Quaternion.Euler(-pitchRange, 0, 0) * forward * gizmoLength);
            }
            
            if (enableRoll)
            {
                Vector3 right = transform.right;
                Gizmos.DrawLine(basePos, basePos + Quaternion.Euler(0, 0, rollRange) * right * gizmoLength);
                Gizmos.DrawLine(basePos, basePos + Quaternion.Euler(0, 0, -rollRange) * right * gizmoLength);
            }
        }
    }
}


