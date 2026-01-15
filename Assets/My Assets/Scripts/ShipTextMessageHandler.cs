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
        
        // Subscribe to ship arrival events
        EventManager.OnEnemyShip1Arrive += OnShip1Arrive;
        EventManager.OnEnemyShip2Arrive += OnShip2Arrive;
        EventManager.OnEnemyShip3Arrive += OnShip3Arrive;
        EventManager.OnEnemyShip4Arrive += OnShip4Arrive;
        
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
            textObject.SetActive(active);
        }
    }
}
