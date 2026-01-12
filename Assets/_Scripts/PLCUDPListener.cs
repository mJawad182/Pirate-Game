using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// Listens for UDP packets from PLC and simulates keyboard input in Unity.
/// Handles commands like "press the letter Q" and converts them to Unity input events.
/// </summary>
public class PLCUDPListener : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Port number to listen for UDP packets")]
    public int listenPort = 9876;
    
    [Tooltip("Enable/disable UDP listener")]
    public bool enableListener = true;
    
    [Header("Debug")]
    [Tooltip("Log all received messages")]
    public bool debugMode = true;
    
    private UdpClient udpClient;
    private Thread udpThread;
    private bool isListening = false;
    
    // Queue for thread-safe message processing
    private Queue<string> messageQueue = new Queue<string>();
    private object queueLock = new object();
    
    // Dictionary to map commands to key codes
    private Dictionary<string, KeyCode> keyCodeMap = new Dictionary<string, KeyCode>();
    
    // Current simulated key states
    private HashSet<KeyCode> simulatedKeysDown = new HashSet<KeyCode>();
    
    void Start()
    {
        InitializeKeyCodeMap();
        
        if (enableListener)
        {
            StartUDPListener();
        }
    }
    
    /// <summary>
    /// Initialize the mapping of letter commands to Unity KeyCode
    /// </summary>
    void InitializeKeyCodeMap()
    {
        // Map letters A-Z
        for (char c = 'A'; c <= 'Z'; c++)
        {
            string letter = c.ToString();
            keyCodeMap[letter.ToLower()] = (KeyCode)System.Enum.Parse(typeof(KeyCode), letter);
            keyCodeMap[letter.ToUpper()] = (KeyCode)System.Enum.Parse(typeof(KeyCode), letter);
        }
        
        // Map numbers 0-9
        for (int i = 0; i <= 9; i++)
        {
            keyCodeMap[i.ToString()] = (KeyCode)System.Enum.Parse(typeof(KeyCode), "Alpha" + i);
        }
        
        // Map common special keys
        keyCodeMap["space"] = KeyCode.Space;
        keyCodeMap["enter"] = KeyCode.Return;
        keyCodeMap["escape"] = KeyCode.Escape;
        keyCodeMap["shift"] = KeyCode.LeftShift;
        keyCodeMap["ctrl"] = KeyCode.LeftControl;
        keyCodeMap["alt"] = KeyCode.LeftAlt;
    }
    
    /// <summary>
    /// Starts the UDP listener thread
    /// </summary>
    void StartUDPListener()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
            isListening = true;
            udpThread = new Thread(new ThreadStart(ReceiveData));
            udpThread.IsBackground = true;
            udpThread.Start();
            
            Debug.Log($"UDP Listener started on port {listenPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start UDP listener: {e.Message}");
            enableListener = false;
        }
    }
    
    /// <summary>
    /// Thread function that receives UDP packets
    /// </summary>
    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
        
        while (isListening)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data).Trim();
                
                if (debugMode)
                {
                    Debug.Log($"[UDP] Received from {remoteEndPoint}: {message}");
                }
                
                // Add message to queue for processing on main thread
                lock (queueLock)
                {
                    messageQueue.Enqueue(message);
                }
            }
            catch (SocketException ex)
            {
                if (isListening)
                {
                    Debug.LogError($"UDP SocketException: {ex.Message}");
                }
            }
            catch (System.Exception ex)
            {
                if (isListening)
                {
                    Debug.LogError($"UDP Error: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// Process messages from the queue on the main thread
    /// </summary>
    void Update()
    {
        // Process all queued messages
        while (messageQueue.Count > 0)
        {
            string message;
            lock (queueLock)
            {
                if (messageQueue.Count > 0)
                    message = messageQueue.Dequeue();
                else
                    break;
            }
            
            ProcessMessage(message);
        }
        
        // Simulate key presses (Unity's Input system doesn't support direct key simulation,
        // so we'll use a custom input system that other scripts can check)
    }
    
    /// <summary>
    /// Process incoming UDP message and extract commands
    /// </summary>
    private void ProcessMessage(string message)
    {
        message = message.ToLower().Trim();
        
        // Handle "press the letter X" format
        if (message.Contains("press the letter"))
        {
            string letter = ExtractLetterFromMessage(message);
            if (!string.IsNullOrEmpty(letter))
            {
                SimulateKeyPress(letter);
            }
        }
        // Handle "press X" format
        else if (message.StartsWith("press "))
        {
            string key = message.Substring(6).Trim();
            SimulateKeyPress(key);
        }
        // Handle direct key name
        else if (keyCodeMap.ContainsKey(message))
        {
            SimulateKeyPress(message);
        }
        else
        {
            Debug.LogWarning($"Unknown UDP command: {message}");
        }
    }
    
    /// <summary>
    /// Extract letter from "press the letter X" message
    /// </summary>
    private string ExtractLetterFromMessage(string message)
    {
        // Look for patterns like "press the letter Q" or "press letter Q"
        string[] parts = message.Split(' ');
        foreach (string part in parts)
        {
            if (part.Length == 1 && char.IsLetter(part[0]))
            {
                return part;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Simulate a key press
    /// </summary>
    private void SimulateKeyPress(string keyName)
    {
        if (keyCodeMap.ContainsKey(keyName))
        {
            KeyCode keyCode = keyCodeMap[keyName];
            simulatedKeysDown.Add(keyCode);
            
            // Trigger key press event
            OnSimulatedKeyPressed(keyCode);
            
            Debug.Log($"Simulated key press: {keyName} (KeyCode: {keyCode})");
            
            // Remove key after a frame (simulating key down then key up)
            StartCoroutine(RemoveKeyAfterFrame(keyCode));
        }
        else
        {
            Debug.LogWarning($"Unknown key: {keyName}");
        }
    }
    
    /// <summary>
    /// Remove key from simulated keys after one frame
    /// </summary>
    private System.Collections.IEnumerator RemoveKeyAfterFrame(KeyCode keyCode)
    {
        yield return null; // Wait one frame
        simulatedKeysDown.Remove(keyCode);
        OnSimulatedKeyReleased(keyCode);
    }
    
    /// <summary>
    /// Check if a key is currently being simulated as pressed
    /// </summary>
    public bool IsSimulatedKeyDown(KeyCode keyCode)
    {
        return simulatedKeysDown.Contains(keyCode);
    }
    
    /// <summary>
    /// Check if a key was just pressed this frame (simulated)
    /// Note: This is a simplified version. For more accurate detection,
    /// you'd need to track frame-by-frame state changes.
    /// </summary>
    public bool IsSimulatedKeyPressed(KeyCode keyCode)
    {
        return simulatedKeysDown.Contains(keyCode);
    }
    
    /// <summary>
    /// Event called when a key is simulated as pressed
    /// </summary>
    public System.Action<KeyCode> OnSimulatedKeyPressed;
    
    /// <summary>
    /// Event called when a key is simulated as released
    /// </summary>
    public System.Action<KeyCode> OnSimulatedKeyReleased;
    
    void OnApplicationQuit()
    {
        StopUDPListener();
    }
    
    void OnDestroy()
    {
        StopUDPListener();
    }
    
    /// <summary>
    /// Stop the UDP listener
    /// </summary>
    void StopUDPListener()
    {
        isListening = false;
        
        if (udpThread != null && udpThread.IsAlive)
        {
            udpThread.Abort();
        }
        
        if (udpClient != null)
        {
            udpClient.Close();
        }
        
        Debug.Log("UDP Listener stopped");
    }
}



