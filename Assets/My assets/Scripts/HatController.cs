using UnityEngine;
using System.Collections;

/// <summary>
/// Controls hat switching when swimmer reaches a certain point
/// Turns off the original hat's SkinnedMeshRenderer and activates a separate hat GameObject
/// </summary>
public class HatController : MonoBehaviour
{
    [Header("Hat References")]
    [Tooltip("The SkinnedMeshRenderer of the original hat on the swimmer's mesh")]
    public SkinnedMeshRenderer originalHatRenderer;
    
    [Tooltip("The separate hat GameObject (child of swimmer) to activate")]
    public GameObject separateHatObject;
    
    [Header("Trigger Point Settings")]
    [Tooltip("How to detect when to switch the hat")]
    public TriggerMode triggerMode = TriggerMode.DistanceToPoint;
    
    [Tooltip("Position to check distance against (if using DistanceToPoint mode)")]
    public Transform triggerPoint;
    
    [Tooltip("Distance threshold to trigger hat switch (meters)")]
    [Range(0.1f, 50f)]
    public float triggerDistance = 5f;
    
    [Tooltip("Waypoint index to trigger at (if using WaypointIndex mode)")]
    public int triggerWaypointIndex = 0;
    
    [Header("Hat Physics Settings")]
    [Tooltip("Backward force applied to hat when it falls off (relative to swimmer's forward direction)")]
    [Range(0f, 20f)]
    public float backwardForce = 5f;
    
    [Tooltip("Upward force applied initially to make hat lift off")]
    [Range(0f, 15f)]
    public float upwardForce = 3f;
    
    [Tooltip("Rotational force (tumbling) applied to hat")]
    [Range(0f, 50f)]
    public float rotationForce = 20f;
    
    [Tooltip("Mass of the hat (affects how it falls)")]
    [Range(0.1f, 5f)]
    public float hatMass = 0.5f;
    
    [Tooltip("Drag coefficient (higher = more air resistance, slower fall)")]
    [Range(0f, 5f)]
    public float airDrag = 1f;
    
    [Tooltip("Angular drag (affects rotation speed)")]
    [Range(0f, 10f)]
    public float angularDrag = 2f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    // Event for when hat starts falling (for ParrotController)
    public System.Action OnHatStartedFalling;
    
    private bool hatSwitched = false;
    private SwimController swimController;
    private PathFollower pathFollower;
    private Rigidbody hatRigidbodyRef;
    private Vector3 flipAxisRef;
    private bool maintainFlipRotation = false;
    
    public enum TriggerMode
    {
        DistanceToPoint,
        WaypointIndex
    }
    
    void Start()
    {
        // Get swimmer controller for waypoint tracking
        swimController = GetComponent<SwimController>();
        if (swimController == null)
        {
            swimController = GetComponentInParent<SwimController>();
        }
        
        // Get path follower if available
        if (swimController != null && swimController.pathToFollow != null)
        {
            pathFollower = swimController.pathToFollow;
        }
        
        // Ensure separate hat starts inactive
        if (separateHatObject != null)
        {
            separateHatObject.SetActive(false);
        }
        
        // Validate references
        if (originalHatRenderer == null)
        {
            Debug.LogWarning("HatController: Original hat SkinnedMeshRenderer not assigned!");
        }
        
        if (separateHatObject == null)
        {
            Debug.LogWarning("HatController: Separate hat GameObject not assigned!");
        }
    }
    
    void Update()
    {
        // Don't check if already switched
        if (hatSwitched) return;
        
        // Check if we should switch the hat
        bool shouldSwitch = false;
        
        if (triggerMode == TriggerMode.DistanceToPoint)
        {
            if (triggerPoint != null)
            {
                float distance = Vector3.Distance(transform.position, triggerPoint.position);
                if (distance <= triggerDistance)
                {
                    shouldSwitch = true;
                    if (showDebug) Debug.Log($"HatController: Swimmer reached trigger point (distance: {distance:F2}m)");
                }
            }
        }
        else if (triggerMode == TriggerMode.WaypointIndex)
        {
            if (pathFollower != null && swimController != null)
            {
                // Access the current waypoint index through reflection or public method
                // For now, we'll check if swimmer has reached a certain waypoint
                // This might need adjustment based on your SwimController implementation
                if (showDebug) Debug.LogWarning("HatController: WaypointIndex mode may need SwimController modification to access current waypoint index");
            }
        }
        
        if (shouldSwitch)
        {
            SwitchHat();
        }
    }
    
    void FixedUpdate()
    {
        // Maintain flip rotation - constrain angular velocity to only the Z axis
        if (maintainFlipRotation && hatRigidbodyRef != null)
        {
            // Project angular velocity onto Z axis to remove X and Y rotation components
            Vector3 currentAngularVel = hatRigidbodyRef.angularVelocity;
            Vector3 zAxis = Vector3.forward;
            Vector3 projectedAngularVel = Vector3.Project(currentAngularVel, zAxis);
            
            // Only keep rotation along the Z axis (prevents circular spinning)
            hatRigidbodyRef.angularVelocity = projectedAngularVel;
        }
    }
    
    /// <summary>
    /// Switches from original hat to separate hat and makes it fall realistically
    /// </summary>
    public void SwitchHat()
    {
        if (hatSwitched) return;
        
        // Turn off original hat renderer
        if (originalHatRenderer != null)
        {
            originalHatRenderer.enabled = false;
            if (showDebug) Debug.Log("HatController: Original hat renderer disabled");
        }
        
        // Activate, unparent separate hat FIRST, then apply physics after a frame delay
        if (separateHatObject != null)
        {
            // Store world position and rotation BEFORE doing anything
            Vector3 worldPosition = separateHatObject.transform.position;
            Quaternion worldRotation = separateHatObject.transform.rotation;
            
            // Activate the hat (needed to access transform)
            separateHatObject.SetActive(true);
            
            // CRITICAL: Unparent FIRST before applying any physics
            // This ensures the hat is completely independent
            separateHatObject.transform.SetParent(null);
            
            // Restore world position and rotation immediately after unparenting
            separateHatObject.transform.position = worldPosition;
            separateHatObject.transform.rotation = worldRotation;
            
            // Wait one frame for Unity to process the unparenting, then apply physics
            // This prevents any parent-child relationship conflicts
            StartCoroutine(ApplyPhysicsAfterUnparent());
            
            // Notify that hat started falling (for ParrotController)
            OnHatStartedFalling?.Invoke();
            
            if (showDebug) Debug.Log("HatController: Separate hat activated and unparented");
        }
        
        hatSwitched = true;
    }
    
    /// <summary>
    /// Sets up physics components and applies forces for realistic hat fall
    /// </summary>
    private void SetupHatPhysics()
    {
        if (separateHatObject == null) return;
        
        // Ensure hat is still unparented (safety check)
        if (separateHatObject.transform.parent != null)
        {
            separateHatObject.transform.SetParent(null);
            if (showDebug) Debug.LogWarning("HatController: Hat was still parented! Unparenting now.");
        }
        
        // Get or add Rigidbody component
        Rigidbody hatRigidbody = separateHatObject.GetComponent<Rigidbody>();
        if (hatRigidbody == null)
        {
            hatRigidbody = separateHatObject.AddComponent<Rigidbody>();
        }
        
        // IMPORTANT: Clear all constraints first - ensure hat is completely free
        hatRigidbody.constraints = RigidbodyConstraints.None;
        
        // Configure Rigidbody for realistic physics
        hatRigidbody.mass = hatMass;
        hatRigidbody.linearDamping = airDrag;
        hatRigidbody.angularDamping = angularDrag;
        hatRigidbody.useGravity = true;
        
        // Clear any existing velocities to start fresh
        hatRigidbody.linearVelocity = Vector3.zero;
        hatRigidbody.angularVelocity = Vector3.zero;
        
        // Get swimmer's forward direction (or use transform forward if swimmer not available)
        Vector3 swimmerForward = transform.forward;
        if (swimController != null)
        {
            swimmerForward = transform.forward;
        }
        
        // Calculate backward direction (opposite of swimmer's forward)
        Vector3 backwardDirection = -swimmerForward;
        backwardDirection.y = 0; // Keep horizontal
        
        // Apply backward force (blown off by wind/movement)
        Vector3 backwardForceVector = backwardDirection.normalized * backwardForce;
        hatRigidbody.AddForce(backwardForceVector, ForceMode.VelocityChange);
        
        // Apply upward force (initial lift)
        hatRigidbody.AddForce(Vector3.up * upwardForce, ForceMode.VelocityChange);
        
        // Apply rotation around Z axis for flipping
        // This creates a clean end-over-end flip effect
        Vector3 flipAxis = Vector3.forward; // Z axis
        
        // Ensure constraints are cleared (already done above, but double-check)
        hatRigidbody.constraints = RigidbodyConstraints.None;
        
        // Apply torque around Z axis for backward flip
        // Negative rotation for backward flip (flips away from swimmer)
        Vector3 rotationForceVector = flipAxis * -rotationForce;
        hatRigidbody.AddTorque(rotationForceVector, ForceMode.VelocityChange);
        
        if (showDebug)
        {
            Debug.Log($"HatController: Applied rotation around Z axis: {rotationForceVector}");
        }
        
        // Store references to maintain flip rotation (Z axis)
        hatRigidbodyRef = hatRigidbody;
        flipAxisRef = Vector3.forward; // Z axis
        maintainFlipRotation = true;
        
        if (showDebug)
        {
            Debug.Log($"HatController: Applied physics forces - Backward: {backwardForce}, Upward: {upwardForce}, Rotation: {rotationForce}");
        }
    }
    
    /// <summary>
    /// Waits one frame after unparenting before applying physics to avoid conflicts
    /// </summary>
    private IEnumerator ApplyPhysicsAfterUnparent()
    {
        // Wait one frame to ensure Unity has processed the unparenting
        yield return null;
        
        // Now apply physics - hat is fully independent
        SetupHatPhysics();
    }
    
    /// <summary>
    /// Manually trigger hat switch (can be called from other scripts)
    /// </summary>
    public void ManualSwitchHat()
    {
        SwitchHat();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw trigger point and distance
        if (triggerMode == TriggerMode.DistanceToPoint && triggerPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(triggerPoint.position, triggerDistance);
            Gizmos.DrawLine(transform.position, triggerPoint.position);
        }
    }
}
