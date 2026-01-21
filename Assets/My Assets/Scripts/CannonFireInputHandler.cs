using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles CannonFire input actions and shows messages when keys are pressed
/// </summary>
public class CannonFireInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Input Action Asset (InputSystem_Actions) - assign in Inspector or leave empty to auto-find")]
    public InputActionAsset inputActionAsset;
    
    [Tooltip("Input Action Reference for CannonFire1 (Q key) - optional, will use asset if not set")]
    public InputActionReference cannonFire1Action;
    
    [Tooltip("Input Action Reference for CannonFire2 (W key) - optional, will use asset if not set")]
    public InputActionReference cannonFire2Action;
    
    [Tooltip("Input Action Reference for CannonFire3 (E key) - optional, will use asset if not set")]
    public InputActionReference cannonFire3Action;
    
    [Tooltip("Input Action Reference for CannonFire4 (R key) - optional, will use asset if not set")]
    public InputActionReference cannonFire4Action;
    
    [Header("Cannon Bullet Settings")]
    [Tooltip("Cannon bullet prefab to spawn")]
    public GameObject cannonBulletPrefab;
    
    [Header("Particle Effects")]
    [Tooltip("Particle effect prefab to spawn when cannon fires (e.g., muzzle flash, smoke)")]
    public GameObject firingParticleEffectPrefab;
    
    [Header("Audio")]
    [Tooltip("Audio clip to play when cannon fires")]
    public AudioClip cannonFireSound;
    
    [Tooltip("Volume of the cannon fire sound")]
    [Range(0f, 1f)]
    public float cannonFireSoundVolume = 0.7f;
    
    [Header("Cannon Fire Cooldown")]
    [Tooltip("Cooldown period between cannon fires (seconds)")]
    [Range(0f, 10f)]
    public float cannonFireCooldown = 3f;
    
    [Tooltip("Firing position 1 (for Q key / Ship 1)")]
    public Transform firePosition1;
    
    [Tooltip("Firing position 2 (for W key / Ship 2)")]
    public Transform firePosition2;
    
    [Tooltip("Firing position 3 (for E key / Ship 3)")]
    public Transform firePosition3;
    
    [Tooltip("Firing position 4 (for R key / Ship 4)")]
    public Transform firePosition4;
    
    [Tooltip("Target Ship 1 (for Q key)")]
    public Transform targetShip1;
    
    [Tooltip("Target Ship 2 (for W key)")]
    public Transform targetShip2;
    
    [Tooltip("Target Ship 3 (for E key)")]
    public Transform targetShip3;
    
    [Tooltip("Target Ship 4 (for R key)")]
    public Transform targetShip4;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    private InputActionMap playerActionMap;
    private InputAction cannonFire1;
    private InputAction cannonFire2;
    private InputAction cannonFire3;
    private InputAction cannonFire4;
    
    private static CannonFireInputHandler instance;
    private AudioSource audioSource;
    private float lastCannonFireTime = 0f;
    
    // Track which ships have arrived (input actions disabled until ship arrives)
    private bool ship1Arrived = false;
    private bool ship2Arrived = false;
    private bool ship3Arrived = false;
    private bool ship4Arrived = false;
    
    void Start()
    {
        instance = this;
        
        // Setup audio source for cannon fire sound
        SetupAudio();
        
        // Auto-find target ships if not assigned
        AutoFindTargetShips();
        
#if ENABLE_INPUT_SYSTEM
        // If InputActionReferences are provided, use them
        if (cannonFire1Action != null && cannonFire2Action != null && 
            cannonFire3Action != null && cannonFire4Action != null)
        {
            SubscribeToInputActionReferences();
            // Disable all inputs initially - they will be enabled when ships arrive
            DisableAllInputs();
            if (showDebug) Debug.Log("CannonFireInputHandler: Using provided InputActionReferences - All inputs disabled until ships arrive");
            return;
        }
        
        // Otherwise, try to use InputActionAsset
        if (inputActionAsset == null)
        {
            // Try to load from Resources
            inputActionAsset = Resources.Load<InputActionAsset>("InputSystem_Actions");
        }
        
        if (inputActionAsset != null)
        {
            playerActionMap = inputActionAsset.FindActionMap("Player");
            
            if (playerActionMap != null)
            {
                // Create action map instance
                playerActionMap = playerActionMap.Clone();
                playerActionMap.Enable();
                
                // Get actions
                cannonFire1 = playerActionMap.FindAction("CannonFire1");
                cannonFire2 = playerActionMap.FindAction("CannonFire2");
                cannonFire3 = playerActionMap.FindAction("CannonFire3");
                cannonFire4 = playerActionMap.FindAction("CannonFire4");
                
                // Subscribe to actions
                SubscribeToInputActions();
                
                // Disable all inputs initially - they will be enabled when ships arrive
                DisableAllInputs();
                
                if (showDebug) Debug.Log("CannonFireInputHandler: Using InputActionAsset and subscribed to actions - All inputs disabled until ships arrive");
            }
            else
            {
                Debug.LogWarning("CannonFireInputHandler: Could not find 'Player' action map in InputActionAsset!");
            }
        }
        else
        {
            Debug.LogWarning("CannonFireInputHandler: InputActionAsset not found! Please assign it in Inspector or create InputActionReferences.");
        }
#else
        Debug.LogWarning("CannonFireInputHandler: Input System is not enabled! Please enable it in Project Settings.");
#endif
    }
    
    void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        UnsubscribeFromInputActions();
        
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
#endif
    }
    
#if ENABLE_INPUT_SYSTEM
    private void SubscribeToInputActionReferences()
    {
        if (cannonFire1Action != null)
        {
            cannonFire1Action.action.performed += OnCannonFire1;
            cannonFire1Action.action.Enable();
        }
        
        if (cannonFire2Action != null)
        {
            cannonFire2Action.action.performed += OnCannonFire2;
            cannonFire2Action.action.Enable();
        }
        
        if (cannonFire3Action != null)
        {
            cannonFire3Action.action.performed += OnCannonFire3;
            cannonFire3Action.action.Enable();
        }
        
        if (cannonFire4Action != null)
        {
            cannonFire4Action.action.performed += OnCannonFire4;
            cannonFire4Action.action.Enable();
        }
    }
    
    private void SubscribeToInputActions()
    {
        if (cannonFire1 != null)
        {
            cannonFire1.performed += OnCannonFire1;
            cannonFire1.Enable();
        }
        
        if (cannonFire2 != null)
        {
            cannonFire2.performed += OnCannonFire2;
            cannonFire2.Enable();
        }
        
        if (cannonFire3 != null)
        {
            cannonFire3.performed += OnCannonFire3;
            cannonFire3.Enable();
        }
        
        if (cannonFire4 != null)
        {
            cannonFire4.performed += OnCannonFire4;
            cannonFire4.Enable();
        }
    }
    
    private void UnsubscribeFromInputActions()
    {
        if (cannonFire1Action != null && cannonFire1Action.action != null)
        {
            cannonFire1Action.action.performed -= OnCannonFire1;
        }
        
        if (cannonFire2Action != null && cannonFire2Action.action != null)
        {
            cannonFire2Action.action.performed -= OnCannonFire2;
        }
        
        if (cannonFire3Action != null && cannonFire3Action.action != null)
        {
            cannonFire3Action.action.performed -= OnCannonFire3;
        }
        
        if (cannonFire4Action != null && cannonFire4Action.action != null)
        {
            cannonFire4Action.action.performed -= OnCannonFire4;
        }
        
        if (cannonFire1 != null) cannonFire1.performed -= OnCannonFire1;
        if (cannonFire2 != null) cannonFire2.performed -= OnCannonFire2;
        if (cannonFire3 != null) cannonFire3.performed -= OnCannonFire3;
        if (cannonFire4 != null) cannonFire4.performed -= OnCannonFire4;
        
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
            playerActionMap.Dispose();
        }
    }
    
    private void OnCannonFire1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"[CANNON FIRE] Q key pressed! Ship1Arrived: {ship1Arrived}, Prefab: {(cannonBulletPrefab != null ? cannonBulletPrefab.name : "NULL")}, FirePos1: {(firePosition1 != null ? firePosition1.name : "NULL")}, TargetShip1: {(targetShip1 != null ? targetShip1.name : "NULL")}");
            
            if (!ship1Arrived)
            {
                Debug.LogWarning("[CANNON FIRE] Ship 1 has not arrived yet! Cannot fire.");
                return;
            }
            
            FireCannon(1, firePosition1, targetShip1);
        }
    }
    
    private void OnCannonFire2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"[CANNON FIRE] W key pressed! Ship2Arrived: {ship2Arrived}, Prefab: {(cannonBulletPrefab != null ? cannonBulletPrefab.name : "NULL")}, FirePos2: {(firePosition2 != null ? firePosition2.name : "NULL")}, TargetShip2: {(targetShip2 != null ? targetShip2.name : "NULL")}");
            
            if (!ship2Arrived)
            {
                Debug.LogWarning("[CANNON FIRE] Ship 2 has not arrived yet! Cannot fire.");
                return;
            }
            
            FireCannon(2, firePosition2, targetShip2);
        }
    }
    
    private void OnCannonFire3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"[CANNON FIRE] E key pressed! Ship3Arrived: {ship3Arrived}, Prefab: {(cannonBulletPrefab != null ? cannonBulletPrefab.name : "NULL")}, FirePos3: {(firePosition3 != null ? firePosition3.name : "NULL")}, TargetShip3: {(targetShip3 != null ? targetShip3.name : "NULL")}");
            
            if (!ship3Arrived)
            {
                Debug.LogWarning("[CANNON FIRE] Ship 3 has not arrived yet! Cannot fire.");
                return;
            }
            
            FireCannon(3, firePosition3, targetShip3);
        }
    }
    
    private void OnCannonFire4(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"[CANNON FIRE] R key pressed! Ship4Arrived: {ship4Arrived}, Prefab: {(cannonBulletPrefab != null ? cannonBulletPrefab.name : "NULL")}, FirePos4: {(firePosition4 != null ? firePosition4.name : "NULL")}, TargetShip4: {(targetShip4 != null ? targetShip4.name : "NULL")}");
            
            if (!ship4Arrived)
            {
                Debug.LogWarning("[CANNON FIRE] Ship 4 has not arrived yet! Cannot fire.");
                return;
            }
            
            FireCannon(4, firePosition4, targetShip4);
        }
    }
    
    /// <summary>
    /// Spawns and fires a cannon bullet from the firing position towards the target ship
    /// </summary>
    private void FireCannon(int cannonID, Transform firePosition, Transform targetShip)
    {
        // Check cooldown
        float timeSinceLastFire = Time.time - lastCannonFireTime;
        if (timeSinceLastFire < cannonFireCooldown)
        {
            float remainingCooldown = cannonFireCooldown - timeSinceLastFire;
            if (showDebug) Debug.Log($"[FIRE CANNON] Cannon {cannonID} is on cooldown! {remainingCooldown:F2}s remaining");
            return;
        }
        
        Debug.Log($"[FIRE CANNON] Attempting to fire cannon {cannonID}...");
        
        if (cannonBulletPrefab == null)
        {
            Debug.LogError($"[FIRE CANNON] Cannon bullet prefab not assigned! Cannot fire cannon {cannonID}.");
            return;
        }
        
        if (firePosition == null)
        {
            Debug.LogError($"[FIRE CANNON] Fire position {cannonID} not assigned! Cannot fire cannon {cannonID}.");
            return;
        }
        
        if (targetShip == null)
        {
            Debug.LogError($"[FIRE CANNON] Target ship {cannonID} not assigned! Cannot fire cannon {cannonID}.");
            return;
        }
        
        Debug.Log($"[FIRE CANNON] All checks passed! Spawning bullet at {firePosition.position} towards {targetShip.name} at {targetShip.position}");
        
        // Fire cannon fired event for crow reactions (pass firing position)
        Debug.Log($"[FIRE CANNON] Firing OnCannonFired event with position: {firePosition.position}");
        if (EventManager.OnCannonFired != null)
        {
            Debug.Log($"[FIRE CANNON] Event has {EventManager.OnCannonFired.GetInvocationList().Length} subscribers");
            EventManager.OnCannonFired.Invoke(firePosition.position);
            Debug.Log($"[FIRE CANNON] Event invoked successfully!");
        }
        else
        {
            Debug.LogWarning($"[FIRE CANNON] OnCannonFired event is NULL! No subscribers!");
        }
        
        // Play cannon fire sound (if assigned)
        if (cannonFireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cannonFireSound, cannonFireSoundVolume);
            if (showDebug) Debug.Log("CannonFireInputHandler: Playing cannon fire sound");
        }
        
        // Play firing sound (from CannonAudioHandler)
        CannonAudioHandler.PlayFire();
        
        // Spawn firing particle effect
        SpawnFiringParticleEffect(firePosition);
        
        // Spawn bullet at firing position
        GameObject bullet = Instantiate(cannonBulletPrefab, firePosition.position, firePosition.rotation);
        
        if (bullet == null)
        {
            Debug.LogError($"[FIRE CANNON] Failed to instantiate bullet prefab!");
            return;
        }
        
        Debug.Log($"[FIRE CANNON] Bullet instantiated: {bullet.name} at position {bullet.transform.position}");
        
        // Set target ship for the bullet
        CannonBullet bulletScript = bullet.GetComponent<CannonBullet>();
        if (bulletScript != null)
        {
            bulletScript.targetShip = targetShip;
            // Tell bullet to ignore collisions with the firing position
            bulletScript.SetIgnoreCollision(firePosition.gameObject);
            Debug.Log($"[FIRE CANNON] Successfully fired cannon {cannonID} from {firePosition.name} towards {targetShip.name} at {targetShip.position}!");
            if (showDebug) Debug.Log($"CannonFireInputHandler: Fired cannon {cannonID} from {firePosition.name} towards {targetShip.name}");
        }
        else
        {
            Debug.LogError($"[FIRE CANNON] Spawned bullet '{bullet.name}' doesn't have CannonBullet component! Cannon {cannonID}. Adding component...");
            bulletScript = bullet.AddComponent<CannonBullet>();
            bulletScript.targetShip = targetShip;
            bulletScript.SetIgnoreCollision(firePosition.gameObject);
            Debug.Log($"[FIRE CANNON] Added CannonBullet component and set target!");
        }
        
        // Update last fire time for cooldown
        lastCannonFireTime = Time.time;
        if (showDebug) Debug.Log($"[FIRE CANNON] Cannon {cannonID} fired! Cooldown started ({cannonFireCooldown}s)");
    }
    
    /// <summary>
    /// Spawns the firing particle effect at the firing position
    /// </summary>
    private void SpawnFiringParticleEffect(Transform firePosition)
    {
        if (firingParticleEffectPrefab == null)
        {
            if (showDebug) Debug.LogWarning("[FIRE CANNON] Firing particle effect prefab not assigned, skipping particle effect.");
            return;
        }
        
        GameObject particleEffect = Instantiate(firingParticleEffectPrefab, firePosition.position, firePosition.rotation);
        
        if (particleEffect != null)
        {
            // Add auto-destroy component to handle cleanup
            ParticleEffectAutoDestroy autoDestroy = particleEffect.GetComponent<ParticleEffectAutoDestroy>();
            if (autoDestroy == null)
            {
                autoDestroy = particleEffect.AddComponent<ParticleEffectAutoDestroy>();
            }
            
            // Try to get ParticleSystem component and play it if it exists
            ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                if (showDebug) Debug.Log($"[FIRE CANNON] Spawned and played firing particle effect at {firePosition.position}");
            }
            else
            {
                // Check if there's a ParticleSystem in children
                ps = particleEffect.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    if (showDebug) Debug.Log($"[FIRE CANNON] Spawned and played firing particle effect (from child) at {firePosition.position}");
                }
                else
                {
                    if (showDebug) Debug.Log($"[FIRE CANNON] Spawned firing particle effect (no ParticleSystem found) at {firePosition.position}");
                }
            }
        }
        else
        {
            Debug.LogError("[FIRE CANNON] Failed to instantiate firing particle effect prefab!");
        }
    }
#endif
    
    /// <summary>
    /// Disables all cannon fire inputs initially
    /// </summary>
    private void DisableAllInputs()
    {
#if ENABLE_INPUT_SYSTEM
        if (cannonFire1 != null) cannonFire1.Disable();
        if (cannonFire2 != null) cannonFire2.Disable();
        if (cannonFire3 != null) cannonFire3.Disable();
        if (cannonFire4 != null) cannonFire4.Disable();
        
        if (cannonFire1Action != null && cannonFire1Action.action != null) cannonFire1Action.action.Disable();
        if (cannonFire2Action != null && cannonFire2Action.action != null) cannonFire2Action.action.Disable();
        if (cannonFire3Action != null && cannonFire3Action.action != null) cannonFire3Action.action.Disable();
        if (cannonFire4Action != null && cannonFire4Action.action != null) cannonFire4Action.action.Disable();
#endif
    }
    
    /// <summary>
    /// Enables cannon fire input for a specific ship (called when ship arrives)
    /// </summary>
    public static void EnableCannonFireInput(int shipID)
    {
        if (instance == null)
        {
            Debug.LogWarning($"CannonFireInputHandler: Cannot enable input for Ship {shipID} - instance is null!");
            return;
        }
        
        instance.EnableInputForShip(shipID);
    }
    
    private void EnableInputForShip(int shipID)
    {
#if ENABLE_INPUT_SYSTEM
        Debug.Log($"[INPUT HANDLER] Enabling CannonFire input for Ship {shipID}");
        
        switch (shipID)
        {
            case 1:
                ship1Arrived = true;
                if (cannonFire1 != null)
                {
                    cannonFire1.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire1 (Q) input enabled!");
                }
                if (cannonFire1Action != null && cannonFire1Action.action != null)
                {
                    cannonFire1Action.action.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire1 (Q) input enabled via reference!");
                }
                break;
            case 2:
                ship2Arrived = true;
                if (cannonFire2 != null)
                {
                    cannonFire2.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire2 (W) input enabled!");
                }
                if (cannonFire2Action != null && cannonFire2Action.action != null)
                {
                    cannonFire2Action.action.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire2 (W) input enabled via reference!");
                }
                break;
            case 3:
                ship3Arrived = true;
                if (cannonFire3 != null)
                {
                    cannonFire3.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire3 (E) input enabled!");
                }
                if (cannonFire3Action != null && cannonFire3Action.action != null)
                {
                    cannonFire3Action.action.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire3 (E) input enabled via reference!");
                }
                break;
            case 4:
                ship4Arrived = true;
                if (cannonFire4 != null)
                {
                    cannonFire4.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire4 (R) input enabled!");
                }
                if (cannonFire4Action != null && cannonFire4Action.action != null)
                {
                    cannonFire4Action.action.Enable();
                    Debug.Log("[INPUT HANDLER] CannonFire4 (R) input enabled via reference!");
                }
                break;
            default:
                Debug.LogWarning($"CannonFireInputHandler: Invalid ship ID {shipID}! Must be 1-4.");
                break;
        }
#endif
    }
    
    /// <summary>
    /// Automatically finds target ships by their shipID if not assigned
    /// </summary>
    private void AutoFindTargetShips()
    {
        Debug.Log("[AUTO-FIND] Starting auto-find for target ships...");
        
        if (targetShip1 == null)
        {
            EvemyShipController ship1 = FindShipByID(1);
            if (ship1 != null)
            {
                targetShip1 = ship1.transform;
                Debug.Log($"[AUTO-FIND] Found Ship 1: {targetShip1.name}");
            }
            else
            {
                Debug.LogWarning("[AUTO-FIND] Ship 1 not found!");
            }
        }
        else
        {
            Debug.Log($"[AUTO-FIND] Ship 1 already assigned: {targetShip1.name}");
        }
        
        if (targetShip2 == null)
        {
            EvemyShipController ship2 = FindShipByID(2);
            if (ship2 != null)
            {
                targetShip2 = ship2.transform;
                Debug.Log($"[AUTO-FIND] Found Ship 2: {targetShip2.name}");
            }
            else
            {
                Debug.LogWarning("[AUTO-FIND] Ship 2 not found!");
            }
        }
        else
        {
            Debug.Log($"[AUTO-FIND] Ship 2 already assigned: {targetShip2.name}");
        }
        
        if (targetShip3 == null)
        {
            EvemyShipController ship3 = FindShipByID(3);
            if (ship3 != null)
            {
                targetShip3 = ship3.transform;
                Debug.Log($"[AUTO-FIND] Found Ship 3: {targetShip3.name}");
            }
            else
            {
                Debug.LogWarning("[AUTO-FIND] Ship 3 not found!");
            }
        }
        else
        {
            Debug.Log($"[AUTO-FIND] Ship 3 already assigned: {targetShip3.name}");
        }
        
        if (targetShip4 == null)
        {
            EvemyShipController ship4 = FindShipByID(4);
            if (ship4 != null)
            {
                targetShip4 = ship4.transform;
                Debug.Log($"[AUTO-FIND] Found Ship 4: {targetShip4.name}");
            }
            else
            {
                Debug.LogWarning("[AUTO-FIND] Ship 4 not found!");
            }
        }
        else
        {
            Debug.Log($"[AUTO-FIND] Ship 4 already assigned: {targetShip4.name}");
        }
        
        Debug.Log($"[AUTO-FIND] Final status - Ship1: {(targetShip1 != null ? targetShip1.name : "NULL")}, " +
                 $"Ship2: {(targetShip2 != null ? targetShip2.name : "NULL")}, " +
                 $"Ship3: {(targetShip3 != null ? targetShip3.name : "NULL")}, " +
                 $"Ship4: {(targetShip4 != null ? targetShip4.name : "NULL")}");
    }
    
    /// <summary>
    /// Finds a ship by its shipID
    /// </summary>
    private EvemyShipController FindShipByID(int shipID)
    {
        EvemyShipController[] ships = FindObjectsOfType<EvemyShipController>();
        foreach (EvemyShipController ship in ships)
        {
            if (ship.shipID == shipID)
            {
                return ship;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Sets up the AudioSource component for playing cannon fire sound
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
        audioSource.spatialBlend = 0f; // 2D sound (or 1f for 3D)
        audioSource.volume = cannonFireSoundVolume;
    }
}
