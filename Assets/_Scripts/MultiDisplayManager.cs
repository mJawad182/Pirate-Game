using UnityEngine;

/// <summary>
/// Legacy multi-display manager. 
/// For full 8-camera setup, use MultiCameraManager instead.
/// This script is kept for simple multi-display activation only.
/// </summary>
public class MultiDisplayManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Displays found: " + Display.displays.Length);

        // Activate all available displays (up to 8)
        for (int i = 1; i < Display.displays.Length && i < 8; i++)
        {
            Display.displays[i].Activate();
            Debug.Log($"Activated Display {i}");
        }
        
        Debug.LogWarning("Note: For full 8-camera setup, use MultiCameraManager component instead.");
    }
}
