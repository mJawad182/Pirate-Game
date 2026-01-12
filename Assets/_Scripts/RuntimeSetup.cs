using UnityEngine;

/// <summary>
/// Runtime setup component that can be attached to any GameObject.
/// Provides a button in the inspector to run one-click setup.
/// </summary>
public class RuntimeSetup : MonoBehaviour
{
    //[Header("One-Click Setup")]
    [Tooltip("Note: Use the Editor menu 'Tools > Pirate Escape Room > One-Click Setup' for full setup")]
    [ContextMenu("Run Runtime Setup (Play Mode Only)")]
    public void RunRuntimeSetup()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Runtime Setup should be run in Play Mode! For Edit Mode setup, use: Tools > Pirate Escape Room > One-Click Setup");
            return;
        }
        
        AutoSetup();
    }
    
    void Start()
    {
        // Auto-setup can be triggered here if needed
        // Uncomment the line below to auto-setup when entering Play Mode
        // AutoSetup();
    }
    
    /// <summary>
    /// Automatically setup systems at runtime (for testing)
    /// </summary>
    void AutoSetup()
    {
        // Find or get GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gameManager = gmObj.AddComponent<GameManager>();
        }
        
        // Find or get other managers
        if (gameManager.cameraManager == null)
        {
            MultiCameraManager camMgr = FindObjectOfType<MultiCameraManager>();
            if (camMgr == null)
            {
                GameObject camObj = new GameObject("MultiCameraManager");
                camMgr = camObj.AddComponent<MultiCameraManager>();
            }
            gameManager.cameraManager = camMgr;
        }
        
        if (gameManager.plcListener == null)
        {
            PLCUDPListener plc = FindObjectOfType<PLCUDPListener>();
            if (plc == null)
            {
                GameObject plcObj = new GameObject("PLCUDPListener");
                plc = plcObj.AddComponent<PLCUDPListener>();
            }
            gameManager.plcListener = plc;
        }
        
        if (gameManager.stormController == null)
        {
            StormController storm = FindObjectOfType<StormController>();
            if (storm == null)
            {
                GameObject stormObj = new GameObject("StormController");
                storm = stormObj.AddComponent<StormController>();
            }
            gameManager.stormController = storm;
        }
        
        if (gameManager.shipInteraction == null)
        {
            ShipInteraction shipInt = FindObjectOfType<ShipInteraction>();
            if (shipInt == null)
            {
                GameObject shipObj = GameObject.FindGameObjectWithTag("Player");
                if (shipObj == null) shipObj = GameObject.Find("Ship");
                if (shipObj == null)
                {
                    shipObj = new GameObject("Ship");
                    shipObj.tag = "Player";
                }
                shipInt = shipObj.GetComponent<ShipInteraction>();
                if (shipInt == null)
                {
                    shipInt = shipObj.AddComponent<ShipInteraction>();
                }
            }
            gameManager.shipInteraction = shipInt;
        }
        
        // Setup references
        if (gameManager.shipObject == null)
        {
            gameManager.shipObject = GameObject.FindGameObjectWithTag("Player");
            if (gameManager.shipObject == null)
            {
                gameManager.shipObject = GameObject.Find("Ship");
            }
        }
        
        if (gameManager.cameraManager != null && gameManager.shipObject != null)
        {
            gameManager.cameraManager.shipTransform = gameManager.shipObject.transform;
        }
        
        if (gameManager.shipInteraction != null && gameManager.plcListener != null)
        {
            gameManager.shipInteraction.plcListener = gameManager.plcListener;
        }
        
        Debug.Log("Runtime Setup Complete!");
    }
}

