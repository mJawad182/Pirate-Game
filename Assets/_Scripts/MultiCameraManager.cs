using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages 8 first-person cameras positioned around the ship to cover 360 degrees.
/// Each camera renders to a different display (Display 1-8).
/// </summary>
public class MultiCameraManager : MonoBehaviour
{
    [Header("Camera Setup")]
    [Tooltip("The ship/player object that cameras will orbit around")]
    public Transform shipTransform;
    
    [Tooltip("Height offset from ship center for camera position")]
    public float cameraHeight = 2f;
    
    [Tooltip("Distance from ship center for camera position")]
    public float cameraDistance = 5f;
    
    [Header("Camera Settings")]
    public float fieldOfView = 75f;
    public float nearClipPlane = 0.1f;
    public float farClipPlane = 1000f;
    
    [Header("Camera Angles (Degrees)")]
    [Tooltip("Angles for each camera around the ship (0-360 degrees)")]
    public float[] cameraAngles = new float[] 
    { 
        0f,    // Front
        45f,   // Front-Right
        90f,   // Right
        135f,  // Back-Right
        180f,  // Back
        225f,  // Back-Left
        270f,  // Left
        315f   // Front-Left
    };
    
    private List<Camera> cameras = new List<Camera>();
    private List<GameObject> cameraObjects = new List<GameObject>();
    
    void Start()
    {
        InitializeDisplays();
        CreateCameras();
    }
    
    /// <summary>
    /// Activates all available displays (up to 8)
    /// </summary>
    void InitializeDisplays()
    {
        int displayCount = Display.displays.Length;
        Debug.Log($"Found {displayCount} display(s)");
        
        // Activate all displays (Unity only activates Display 0 by default)
        for (int i = 1; i < displayCount && i < 8; i++)
        {
            Display.displays[i].Activate();
            Debug.Log($"Activated Display {i}");
        }
        
        if (displayCount < 8)
        {
            Debug.LogWarning($"Only {displayCount} display(s) detected. Expected 8 displays for full setup.");
        }
    }
    
    /// <summary>
    /// Creates 8 cameras positioned around the ship
    /// </summary>
    void CreateCameras()
    {
        if (shipTransform == null)
        {
            Debug.LogError("Ship Transform is not assigned!");
            return;
        }
        
        // Clear existing cameras if any
        foreach (var camObj in cameraObjects)
        {
            if (camObj != null)
                DestroyImmediate(camObj);
        }
        cameras.Clear();
        cameraObjects.Clear();
        
        // Create 8 cameras
        for (int i = 0; i < 8; i++)
        {
            CreateCamera(i, cameraAngles[i]);
        }
        
        Debug.Log($"Created {cameras.Count} cameras");
    }
    
    /// <summary>
    /// Creates a single camera at the specified angle
    /// </summary>
    void CreateCamera(int index, float angle)
    {
        // Create camera GameObject
        GameObject cameraObj = new GameObject($"Camera_{index}_Angle_{angle}");
        cameraObj.transform.SetParent(transform);
        
        // Add Camera component
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = nearClipPlane;
        cam.farClipPlane = farClipPlane;
        
        // Set target display (Display 1-8, Unity uses 0-based indexing but displays are 1-8)
        // Display 0 is the main display, so we use index+1
        cam.targetDisplay = index;
        
        // Position camera around the ship
        float angleRad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(angleRad) * cameraDistance,
            cameraHeight,
            Mathf.Cos(angleRad) * cameraDistance
        );
        
        cameraObj.transform.position = shipTransform.position + offset;
        
        // Make camera look at ship
        cameraObj.transform.LookAt(shipTransform.position + Vector3.up * cameraHeight);
        
        // Add Audio Listener to first camera only (to avoid multiple listeners)
        if (index == 0)
        {
            cameraObj.AddComponent<AudioListener>();
        }
        
        cameras.Add(cam);
        cameraObjects.Add(cameraObj);
        
        Debug.Log($"Created Camera {index} at angle {angle}° targeting Display {index}");
    }
    
    /// <summary>
    /// Updates camera positions if ship moves (call this from Update if ship moves)
    /// </summary>
    public void UpdateCameraPositions()
    {
        if (shipTransform == null) return;
        
        for (int i = 0; i < cameras.Count && i < cameraAngles.Length; i++)
        {
            if (cameraObjects[i] != null)
            {
                float angleRad = cameraAngles[i] * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Sin(angleRad) * cameraDistance,
                    cameraHeight,
                    Mathf.Cos(angleRad) * cameraDistance
                );
                
                cameraObjects[i].transform.position = shipTransform.position + offset;
                cameraObjects[i].transform.LookAt(shipTransform.position + Vector3.up * cameraHeight);
            }
        }
    }
    
    /// <summary>
    /// Gets a specific camera by index
    /// </summary>
    public Camera GetCamera(int index)
    {
        if (index >= 0 && index < cameras.Count)
            return cameras[index];
        return null;
    }
    
    /// <summary>
    /// Gets all cameras
    /// </summary>
    public List<Camera> GetAllCameras()
    {
        return cameras;
    }
    
    void Update()
    {
        // Update camera positions if ship is moving
        if (shipTransform != null && shipTransform.hasChanged)
        {
            UpdateCameraPositions();
            shipTransform.hasChanged = false;
        }
    }
    
    void OnDestroy()
    {
        foreach (var camObj in cameraObjects)
        {
            if (camObj != null)
                Destroy(camObj);
        }
    }
}



