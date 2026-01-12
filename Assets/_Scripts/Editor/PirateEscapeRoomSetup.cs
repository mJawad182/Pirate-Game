using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// One-Click Setup for Pirate Escape Room Game
/// Creates all necessary GameObjects and components, and wires them together.
/// Access via: Tools > Pirate Escape Room > One-Click Setup
/// </summary>
public class PirateEscapeRoomSetup : EditorWindow
{
    [MenuItem("Tools/Pirate Escape Room/One-Click Setup")]
    public static void ShowWindow()
    {
        GetWindow<PirateEscapeRoomSetup>("Pirate Escape Room Setup");
    }
    
    [MenuItem("Tools/Pirate Escape Room/One-Click Setup (Quick)")]
    public static void QuickSetup()
    {
        if (EditorUtility.DisplayDialog("One-Click Setup", 
            "This will create all necessary GameObjects and components for the Pirate Escape Room.\n\n" +
            "Continue?", "Yes", "Cancel"))
        {
            PerformSetup();
            EditorUtility.DisplayDialog("Setup Complete", 
                "Pirate Escape Room setup completed!\n\n" +
                "Please check the scene hierarchy and configure:\n" +
                "- Ship GameObject position\n" +
                "- Camera angles and distances\n" +
                "- Storm effects and audio\n" +
                "- Puzzle requirements", "OK");
        }
    }
    
    void OnGUI()
    {
        GUILayout.Label("Pirate Escape Room - One-Click Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This will create:", EditorStyles.label);
        GUILayout.Label("• GameManager (main coordinator)", EditorStyles.helpBox);
        GUILayout.Label("• MultiCameraManager (8-camera system)", EditorStyles.helpBox);
        GUILayout.Label("• PLCUDPListener (PLC integration)", EditorStyles.helpBox);
        GUILayout.Label("• StormController (approaching storm)", EditorStyles.helpBox);
        GUILayout.Label("• ShipInteraction (ship interactions)", EditorStyles.helpBox);
        GUILayout.Label("• Basic Ship GameObject (if missing)", EditorStyles.helpBox);
        GUILayout.Label("• UI Canvas (if missing)", EditorStyles.helpBox);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run Setup", GUILayout.Height(40)))
        {
            PerformSetup();
            EditorUtility.DisplayDialog("Setup Complete", 
                "Pirate Escape Room setup completed!\n\n" +
                "Please check the scene hierarchy and configure:\n" +
                "- Ship GameObject position\n" +
                "- Camera angles and distances\n" +
                "- Storm effects and audio\n" +
                "- Puzzle requirements", "OK");
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Note: This will create new GameObjects if they don't exist, " +
            "or update existing ones if found.", MessageType.Info);
    }
    
    static void PerformSetup()
    {
        Undo.SetCurrentGroupName("Pirate Escape Room Setup");
        int undoGroup = Undo.GetCurrentGroup();
        
        // Find or create GameManager
        GameManager gameManager = FindOrCreateComponent<GameManager>("GameManager");
        
        // Find or create MultiCameraManager
        MultiCameraManager cameraManager = FindOrCreateComponent<MultiCameraManager>("MultiCameraManager");
        
        // Find or create PLCUDPListener
        PLCUDPListener plcListener = FindOrCreateComponent<PLCUDPListener>("PLCUDPListener");
        
        // Find or create StormController
        StormController stormController = FindOrCreateComponent<StormController>("StormController");
        
        // Find or create ShipInteraction
        ShipInteraction shipInteraction = FindOrCreateComponent<ShipInteraction>("ShipInteraction");
        
        // Find or create Ship GameObject
        GameObject shipObject = FindOrCreateShip();
        
        // Setup camera manager
        if (cameraManager != null && shipObject != null)
        {
            Undo.RecordObject(cameraManager, "Setup Camera Manager");
            cameraManager.shipTransform = shipObject.transform;
            EditorUtility.SetDirty(cameraManager);
        }
        
        // Setup ship interaction
        if (shipInteraction != null)
        {
            Undo.RecordObject(shipInteraction, "Setup Ship Interaction");
            shipInteraction.plcListener = plcListener;
            EditorUtility.SetDirty(shipInteraction);
        }
        
        // Setup GameManager references
        if (gameManager != null)
        {
            Undo.RecordObject(gameManager, "Setup Game Manager");
            gameManager.cameraManager = cameraManager;
            gameManager.plcListener = plcListener;
            gameManager.stormController = stormController;
            gameManager.shipInteraction = shipInteraction;
            gameManager.shipObject = shipObject;
            EditorUtility.SetDirty(gameManager);
        }
        
        // Create UI Canvas if missing
        CreateUICanvas();
        
        // Add CrestWaterInteraction to ship if it has Rigidbody
        if (shipObject != null)
        {
            Rigidbody rb = shipObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                CrestWaterInteraction waterInteraction = shipObject.GetComponent<CrestWaterInteraction>();
                if (waterInteraction == null)
                {
                    Undo.AddComponent<CrestWaterInteraction>(shipObject);
                    Debug.Log("Added CrestWaterInteraction to Ship");
                }
            }
        }
        
        // Select GameManager in hierarchy
        if (gameManager != null)
        {
            Selection.activeGameObject = gameManager.gameObject;
        }
        
        Undo.CollapseUndoOperations(undoGroup);
        
        Debug.Log("=== Pirate Escape Room Setup Complete ===");
        Debug.Log("All systems have been created and configured!");
    }
    
    static T FindOrCreateComponent<T>(string objectName) where T : Component
    {
        // Try to find existing component
        T component = FindObjectOfType<T>();
        
        if (component != null)
        {
            Debug.Log($"Found existing {typeof(T).Name} on '{component.gameObject.name}'");
            return component;
        }
        
        // Create new GameObject with component
        GameObject obj = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(obj, $"Create {objectName}");
        component = obj.AddComponent<T>();
        
        Debug.Log($"Created {objectName} with {typeof(T).Name} component");
        
        return component;
    }
    
    static GameObject FindOrCreateShip()
    {
        // Try to find existing ship
        GameObject ship = GameObject.FindGameObjectWithTag("Player");
        if (ship == null)
        {
            ship = GameObject.Find("Ship");
        }
        if (ship == null)
        {
            // Search for objects with "ship" in name (case insensitive)
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains("ship"))
                {
                    ship = obj;
                    break;
                }
            }
        }
        
        if (ship != null)
        {
            Debug.Log($"Found existing Ship: '{ship.name}'");
            return ship;
        }
        
        // Create basic ship GameObject
        ship = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ship.name = "Ship";
        ship.tag = "Player";
        Undo.RegisterCreatedObjectUndo(ship, "Create Ship");
        
        // Add Rigidbody for physics
        Rigidbody rb = ship.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(ship);
        }
        rb.mass = 1000f;
        
        // Handle both Unity 5.x and Unity 6+ physics API
        #if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        #else
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        #endif
        
        // Position ship at origin
        ship.transform.position = Vector3.zero;
        
        // Add visual material (optional - you can replace with your ship model)
        Renderer renderer = ship.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material shipMaterial = new Material(Shader.Find("Standard"));
            shipMaterial.color = new Color(0.4f, 0.2f, 0.1f); // Brown ship color
            renderer.material = shipMaterial;
        }
        
        Debug.Log("Created basic Ship GameObject (replace with your ship model)");
        
        return ship;
    }
    
    static void CreateUICanvas()
    {
        // Check if canvas already exists
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Debug.Log("UI Canvas already exists");
            return;
        }
        
        // Create Canvas
        GameObject canvasObj = new GameObject("UI Canvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create UI Canvas");
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create EventSystem if missing
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Create interaction prompt UI
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(canvasObj.transform, false);
        Undo.RegisterCreatedObjectUndo(promptObj, "Create Interaction Prompt");
        
        UnityEngine.UI.Text promptText = promptObj.AddComponent<UnityEngine.UI.Text>();
        promptText.text = "Press E to interact";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 24;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rectTransform = promptObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.1f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.1f);
        rectTransform.sizeDelta = new Vector2(300, 50);
        rectTransform.anchoredPosition = Vector2.zero;
        
        promptObj.SetActive(false); // Hidden by default
        
        // Link to ShipInteraction if it exists
        ShipInteraction shipInteraction = FindObjectOfType<ShipInteraction>();
        if (shipInteraction != null)
        {
            Undo.RecordObject(shipInteraction, "Link UI Prompt");
            shipInteraction.interactionPromptUI = promptObj;
            EditorUtility.SetDirty(shipInteraction);
        }
        
        Debug.Log("Created UI Canvas with Interaction Prompt");
    }
}

