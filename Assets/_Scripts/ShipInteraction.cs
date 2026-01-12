using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles interactions with other ships in the pirate escape room.
/// Manages ship detection, interaction prompts, and puzzle triggers.
/// </summary>
public class ShipInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance to interact with ships")]
    public float interactionRange = 20f;
    
    [Tooltip("Layer mask for ship objects")]
    public LayerMask shipLayerMask = -1;
    
    [Tooltip("Key to press for interaction (can be overridden by PLC)")]
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("UI")]
    [Tooltip("UI text/prompt to show when ship is interactable")]
    public GameObject interactionPromptUI;
    
    [Tooltip("Text to display in interaction prompt")]
    public string interactionPromptText = "Press E to interact";
    
    [Header("Ship Settings")]
    [Tooltip("List of ships that can be interacted with")]
    public List<InteractableShip> interactableShips = new List<InteractableShip>();
    
    [Header("PLC Integration")]
    [Tooltip("Reference to PLC UDP Listener for simulated inputs")]
    public PLCUDPListener plcListener;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    private InteractableShip currentInteractableShip;
    private Camera playerCamera;
    
    void Start()
    {
        // Find player camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        // Find PLC listener if not assigned
        if (plcListener == null)
        {
            plcListener = FindObjectOfType<PLCUDPListener>();
        }
        
        // Find all interactable ships if list is empty
        if (interactableShips.Count == 0)
        {
            FindInteractableShips();
        }
    }
    
    /// <summary>
    /// Find all ships with InteractableShip component
    /// </summary>
    void FindInteractableShips()
    {
        InteractableShip[] ships = FindObjectsOfType<InteractableShip>();
        interactableShips.AddRange(ships);
        
        if (showDebug)
        {
            Debug.Log($"Found {interactableShips.Count} interactable ships");
        }
    }
    
    void Update()
    {
        CheckForInteractableShips();
        HandleInteractionInput();
    }
    
    /// <summary>
    /// Check for ships within interaction range
    /// </summary>
    void CheckForInteractableShips()
    {
        InteractableShip closestShip = null;
        float closestDistance = float.MaxValue;
        
        foreach (var ship in interactableShips)
        {
            if (ship == null || !ship.gameObject.activeInHierarchy) continue;
            
            float distance = Vector3.Distance(transform.position, ship.transform.position);
            
            if (distance <= interactionRange && distance < closestDistance)
            {
                // Check if ship is in front of player (optional)
                if (playerCamera != null)
                {
                    Vector3 screenPoint = playerCamera.WorldToViewportPoint(ship.transform.position);
                    if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
                    {
                        closestShip = ship;
                        closestDistance = distance;
                    }
                }
                else
                {
                    closestShip = ship;
                    closestDistance = distance;
                }
            }
        }
        
        // Update current interactable ship
        if (closestShip != currentInteractableShip)
        {
            if (currentInteractableShip != null)
            {
                OnShipInteractionLost(currentInteractableShip);
            }
            
            currentInteractableShip = closestShip;
            
            if (currentInteractableShip != null)
            {
                OnShipInteractionAvailable(currentInteractableShip);
            }
            else
            {
                HideInteractionPrompt();
            }
        }
    }
    
    /// <summary>
    /// Handle interaction input (keyboard or PLC simulated)
    /// </summary>
    void HandleInteractionInput()
    {
        bool interactionPressed = false;
        
        // Check keyboard input
        if (Input.GetKeyDown(interactionKey))
        {
            interactionPressed = true;
        }
        
        // Check PLC simulated input
        if (plcListener != null && plcListener.IsSimulatedKeyPressed(interactionKey))
        {
            interactionPressed = true;
        }
        
        if (interactionPressed && currentInteractableShip != null)
        {
            InteractWithShip(currentInteractableShip);
        }
    }
    
    /// <summary>
    /// Called when a ship becomes available for interaction
    /// </summary>
    void OnShipInteractionAvailable(InteractableShip ship)
    {
        ShowInteractionPrompt(ship);
        
        if (showDebug)
        {
            Debug.Log($"Ship '{ship.shipName}' is now interactable");
        }
    }
    
    /// <summary>
    /// Called when interaction with a ship is lost
    /// </summary>
    void OnShipInteractionLost(InteractableShip ship)
    {
        HideInteractionPrompt();
        
        if (showDebug)
        {
            Debug.Log($"Lost interaction with ship '{ship.shipName}'");
        }
    }
    
    /// <summary>
    /// Interact with the specified ship
    /// </summary>
    void InteractWithShip(InteractableShip ship)
    {
        if (ship == null) return;
        
        Debug.Log($"Interacting with ship: {ship.shipName}");
        
        // Trigger ship's interaction event
        ship.OnInteracted?.Invoke();
        
        // Handle ship-specific interaction
        switch (ship.interactionType)
        {
            case ShipInteractionType.Signal:
                HandleSignalInteraction(ship);
                break;
            case ShipInteractionType.Trade:
                HandleTradeInteraction(ship);
                break;
            case ShipInteractionType.Battle:
                HandleBattleInteraction(ship);
                break;
            case ShipInteractionType.Puzzle:
                HandlePuzzleInteraction(ship);
                break;
        }
        
        // Trigger global event
        OnShipInteracted?.Invoke(ship);
    }
    
    /// <summary>
    /// Handle signal interaction (flags, lights, etc.)
    /// </summary>
    void HandleSignalInteraction(InteractableShip ship)
    {
        Debug.Log($"Signaling ship: {ship.shipName}");
        // Implement signaling logic
    }
    
    /// <summary>
    /// Handle trade interaction
    /// </summary>
    void HandleTradeInteraction(InteractableShip ship)
    {
        Debug.Log($"Trading with ship: {ship.shipName}");
        // Implement trade logic
    }
    
    /// <summary>
    /// Handle battle interaction
    /// </summary>
    void HandleBattleInteraction(InteractableShip ship)
    {
        Debug.Log($"Battling ship: {ship.shipName}");
        // Implement battle logic
    }
    
    /// <summary>
    /// Handle puzzle interaction
    /// </summary>
    void HandlePuzzleInteraction(InteractableShip ship)
    {
        Debug.Log($"Solving puzzle with ship: {ship.shipName}");
        // Implement puzzle logic
    }
    
    /// <summary>
    /// Show interaction prompt UI
    /// </summary>
    void ShowInteractionPrompt(InteractableShip ship)
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            
            // Update prompt text if available
            var textComponent = interactionPromptUI.GetComponent<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = $"{interactionPromptText} - {ship.shipName}";
            }
        }
    }
    
    /// <summary>
    /// Hide interaction prompt UI
    /// </summary>
    void HideInteractionPrompt()
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// Event triggered when any ship is interacted with
    /// </summary>
    public System.Action<InteractableShip> OnShipInteracted;
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw lines to interactable ships
        Gizmos.color = Color.green;
        foreach (var ship in interactableShips)
        {
            if (ship != null)
            {
                float distance = Vector3.Distance(transform.position, ship.transform.position);
                if (distance <= interactionRange)
                {
                    Gizmos.DrawLine(transform.position, ship.transform.position);
                }
            }
        }
    }
}

/// <summary>
/// Component to attach to ships that can be interacted with
/// </summary>
public class InteractableShip : MonoBehaviour
{
    [Header("Ship Info")]
    public string shipName = "Unknown Ship";
    
    [Header("Interaction Type")]
    public ShipInteractionType interactionType = ShipInteractionType.Puzzle;
    
    [Header("Interaction Data")]
    [Tooltip("Custom data for this interaction (puzzle ID, item ID, etc.)")]
    public string interactionData;
    
    /// <summary>
    /// Event triggered when ship is interacted with
    /// </summary>
    public System.Action OnInteracted;
}

/// <summary>
/// Types of ship interactions
/// </summary>
public enum ShipInteractionType
{
    Signal,  // Signal with flags/lights
    Trade,   // Trade items
    Battle,  // Combat interaction
    Puzzle   // Puzzle solving
}



