using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float turnSpeed = 30f;
    
    [Header("Auto-Move (Optional)")]
    [Tooltip("If enabled, ship will automatically move forward")]
    public bool autoMoveForward = false;
    [Tooltip("Speed for auto-move (independent of moveSpeed). Lower values = slower auto-move")]
    public float autoMoveSpeed = 3f;
    
    [Header("Components")]
    private Rigidbody rb;
    
    // Input variables
    private Vector2 moveInput;
    
    void Start()
    {
        // Get Rigidbody if not assigned
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        // Ensure Rigidbody exists
        if (rb == null)
        {
            Debug.LogError("ShipController: No Rigidbody found! Please add a Rigidbody component.");
            enabled = false;
            return;
        }
        
        // Configure Rigidbody for water physics
        rb.useGravity = false; // FloatingObject handles buoyancy
        rb.linearDamping = 1f; // Some drag for stability
        rb.angularDamping = 2f; // Angular drag to prevent excessive rotation
    }

    void Update()
    {
        // Get input based on Input System
#if ENABLE_INPUT_SYSTEM
        // New Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float throttle = 0f;
            float steering = 0f;
            
            // Forward/Backward (W/S or Up/Down arrows)
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                throttle = 1f;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                throttle = -1f;
            
            // Left/Right (A/D or Left/Right arrows)
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                steering = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                steering = 1f;
            
            moveInput = new Vector2(steering, throttle);
        }
#else
        // Legacy Input System (fallback)
        moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif
    }
    
    void FixedUpdate()
    {
        float throttle = moveInput.y;
        float steering = moveInput.x;
        bool isAutoMoving = false;
        
        // Auto-move forward if enabled
        if (autoMoveForward && throttle == 0f)
        {
            throttle = 1f; // Full throttle for auto-move
            isAutoMoving = true;
        }
        
        // Apply movement
        if (throttle != 0f || steering != 0f)
        {
            MoveShip(throttle, steering, isAutoMoving);
        }
    }
    
    void MoveShip(float throttle, float steering, bool isAutoMoving = false)
    {
        if (rb == null) return;
        
        // Apply forward/backward force
        if (throttle != 0f)
        {
            // Use autoMoveSpeed for auto-move, otherwise use moveSpeed
            float speedToUse = isAutoMoving ? autoMoveSpeed : moveSpeed;
            Vector3 forwardForce = transform.forward * throttle * speedToUse;
            rb.AddForce(forwardForce, ForceMode.Acceleration);
        }
        
        // Apply turning torque
        if (steering != 0f)
        {
            Vector3 torque = Vector3.up * steering * turnSpeed;
            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }
    
    // Optional: Method to be called from Input System Actions
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
