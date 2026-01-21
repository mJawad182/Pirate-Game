using UnityEngine;

/// <summary>
/// Handles text messages for enemy ships - activates text when ship arrives
/// </summary>
public class ShipTextMessageHandler : MonoBehaviour
{
    [Header("Ship Text Messages")]
    [Tooltip("Text GameObject for Ship 1 (will be activated when Ship 1 arrives)")]
    public GameObject ship1Text;
    
    [Tooltip("Text GameObject for Ship 2 (will be activated when Ship 2 arrives)")]
    public GameObject ship2Text;
    
    [Tooltip("Text GameObject for Ship 3 (will be activated when Ship 3 arrives)")]
    public GameObject ship3Text;
    
    [Tooltip("Text GameObject for Ship 4 (will be activated when Ship 4 arrives)")]
    public GameObject ship4Text;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebug = false;
    
    void Start()
    {
        // Ensure all text objects start inactive
        SetTextActive(ship1Text, false);
        SetTextActive(ship2Text, false);
        SetTextActive(ship3Text, false);
        SetTextActive(ship4Text, false);
        
        // Check if text objects are assigned
        Debug.Log($"[TEXT HANDLER INIT] Ship 1 Text: {(ship1Text != null ? ship1Text.name : "NOT ASSIGNED")}");
        Debug.Log($"[TEXT HANDLER INIT] Ship 2 Text: {(ship2Text != null ? ship2Text.name : "NOT ASSIGNED")}");
        Debug.Log($"[TEXT HANDLER INIT] Ship 3 Text: {(ship3Text != null ? ship3Text.name : "NOT ASSIGNED")}");
        Debug.Log($"[TEXT HANDLER INIT] Ship 4 Text: {(ship4Text != null ? ship4Text.name : "NOT ASSIGNED")}");
        
        // Subscribe to ship arrival events
        EventManager.OnEnemyShip1Arrive += OnShip1Arrive;
        EventManager.OnEnemyShip2Arrive += OnShip2Arrive;
        EventManager.OnEnemyShip3Arrive += OnShip3Arrive;
        EventManager.OnEnemyShip4Arrive += OnShip4Arrive;
        
        Debug.Log("[TEXT HANDLER INIT] Subscribed to all ship arrival events");
        if (showDebug) Debug.Log("ShipTextMessageHandler: Subscribed to all ship arrival events");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        EventManager.OnEnemyShip1Arrive -= OnShip1Arrive;
        EventManager.OnEnemyShip2Arrive -= OnShip2Arrive;
        EventManager.OnEnemyShip3Arrive -= OnShip3Arrive;
        EventManager.OnEnemyShip4Arrive -= OnShip4Arrive;
    }
    
    private void OnShip1Arrive()
    {
        Debug.Log("[TEXT HANDLER] Ship 1 arrival event received!");
        SetTextActive(ship1Text, true);
        Debug.Log($"[TEXT HANDLER] Ship 1 text activated! GameObject: {(ship1Text != null ? ship1Text.name : "NULL")}, Active: {(ship1Text != null ? ship1Text.activeSelf.ToString() : "NULL")}");
        
        // Notify input handler that Ship 1 has arrived
        CannonFireInputHandler.EnableCannonFireInput(1);
        
        if (showDebug) Debug.Log("ShipTextMessageHandler: Ship 1 arrived! Activating Ship 1 text.");
    }
    
    private void OnShip2Arrive()
    {
        Debug.Log("[TEXT HANDLER] Ship 2 arrival event received!");
        SetTextActive(ship2Text, true);
        Debug.Log($"[TEXT HANDLER] Ship 2 text activated! GameObject: {(ship2Text != null ? ship2Text.name : "NULL")}, Active: {(ship2Text != null ? ship2Text.activeSelf.ToString() : "NULL")}");
        
        // Notify input handler that Ship 2 has arrived
        CannonFireInputHandler.EnableCannonFireInput(2);
        
        if (showDebug) Debug.Log("ShipTextMessageHandler: Ship 2 arrived! Activating Ship 2 text.");
    }
    
    private void OnShip3Arrive()
    {
        Debug.Log("[TEXT HANDLER] Ship 3 arrival event received!");
        SetTextActive(ship3Text, true);
        Debug.Log($"[TEXT HANDLER] Ship 3 text activated! GameObject: {(ship3Text != null ? ship3Text.name : "NULL")}, Active: {(ship3Text != null ? ship3Text.activeSelf.ToString() : "NULL")}");
        
        // Notify input handler that Ship 3 has arrived
        CannonFireInputHandler.EnableCannonFireInput(3);
        
        if (showDebug) Debug.Log("ShipTextMessageHandler: Ship 3 arrived! Activating Ship 3 text.");
    }
    
    private void OnShip4Arrive()
    {
        Debug.Log("[TEXT HANDLER] Ship 4 arrival event received!");
        SetTextActive(ship4Text, true);
        Debug.Log($"[TEXT HANDLER] Ship 4 text activated! GameObject: {(ship4Text != null ? ship4Text.name : "NULL")}, Active: {(ship4Text != null ? ship4Text.activeSelf.ToString() : "NULL")}");
        
        // Notify input handler that Ship 4 has arrived
        CannonFireInputHandler.EnableCannonFireInput(4);
        
        if (showDebug) Debug.Log("ShipTextMessageHandler: Ship 4 arrived! Activating Ship 4 text.");
    }
    
    private void SetTextActive(GameObject textObject, bool active)
    {
        if (textObject != null)
        {
            // If activating, ensure all parent objects (Canvas, UI panels, etc.) are also active
            if (active)
            {
                // Enable all parent objects up to the root
                Transform parent = textObject.transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        Debug.Log($"[TEXT HANDLER] Enabling parent GameObject: {parent.name}");
                        parent.gameObject.SetActive(true);
                    }
                    parent = parent.parent;
                }
                
                // Also check for Canvas component and ensure it's enabled
                Canvas canvas = textObject.GetComponentInParent<Canvas>();
                if (canvas != null && !canvas.gameObject.activeSelf)
                {
                    Debug.Log($"[TEXT HANDLER] Enabling Canvas: {canvas.name}");
                    canvas.gameObject.SetActive(true);
                }
            }
            
            // Now set the text object active
            textObject.SetActive(active);
            Debug.Log($"[TEXT HANDLER] SetTextActive called: GameObject={textObject.name}, Active={active}, Result={textObject.activeSelf}, Visible={IsGameObjectVisible(textObject)}");
        }
        else
        {
            Debug.LogWarning($"[TEXT HANDLER] SetTextActive called but textObject is NULL! Cannot set active state to {active}");
        }
    }
    
    /// <summary>
    /// Checks if a GameObject is actually visible (active and all parents are active)
    /// </summary>
    private bool IsGameObjectVisible(GameObject obj)
    {
        if (obj == null) return false;
        if (!obj.activeSelf) return false;
        
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf) return false;
            parent = parent.parent;
        }
        
        return true;
    }
}
