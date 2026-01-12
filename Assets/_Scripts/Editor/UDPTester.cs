using UnityEngine;
using UnityEditor;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP Tester Tool for testing PLC commands without a physical PLC.
/// Access via: Tools > Pirate Escape Room > UDP Tester
/// </summary>
public class UDPTester : EditorWindow
{
    private string targetIP = "127.0.0.1"; // Localhost by default
    private int targetPort = 9876;
    private string messageToSend = "press the letter Q";
    private Vector2 scrollPosition;
    private System.Collections.Generic.List<string> sentMessages = new System.Collections.Generic.List<string>();
    private UdpClient udpClient;
    
    // Preset commands
    private string[] presetCommands = new string[]
    {
        "press the letter Q",
        "press Q",
        "press the letter E",
        "press E",
        "press space",
        "press enter",
        "press escape",
        "press the letter A",
        "press the letter B",
        "press the letter C",
        "press the letter D",
        "press the letter F",
        "press the letter G",
        "press the letter H",
        "press the letter I",
        "press the letter J",
        "press the letter K",
        "press the letter L",
        "press the letter M",
        "press the letter N",
        "press the letter O",
        "press the letter P",
        "press the letter R",
        "press the letter S",
        "press the letter T",
        "press the letter U",
        "press the letter V",
        "press the letter W",
        "press the letter X",
        "press the letter Y",
        "press the letter Z"
    };
    
    [MenuItem("Tools/Pirate Escape Room/UDP Tester")]
    public static void ShowWindow()
    {
        GetWindow<UDPTester>("UDP Tester");
    }
    
    void OnEnable()
    {
        // Initialize UDP client
        try
        {
            udpClient = new UdpClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize UDP client: {e.Message}");
        }
    }
    
    void OnDisable()
    {
        // Clean up UDP client
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
    
    void OnGUI()
    {
        GUILayout.Label("UDP Tester for PLC Commands", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool sends UDP packets to test PLC integration.\n" +
            "Make sure Unity is running in Play Mode and PLCUDPListener is active!", 
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Connection settings
        EditorGUILayout.LabelField("Connection Settings", EditorStyles.boldLabel);
        targetIP = EditorGUILayout.TextField("Target IP:", targetIP);
        targetPort = EditorGUILayout.IntField("Target Port:", targetPort);
        
        EditorGUILayout.Space(10);
        
        // Message input
        EditorGUILayout.LabelField("Message to Send", EditorStyles.boldLabel);
        messageToSend = EditorGUILayout.TextField("Command:", messageToSend);
        
        EditorGUILayout.Space(5);
        
        // Send button
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("Send UDP Packet", GUILayout.Height(30)))
        {
            SendUDPPacket(messageToSend);
        }
        EditorGUI.EndDisabledGroup();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Unity must be in Play Mode to send packets!", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Preset commands
        EditorGUILayout.LabelField("Preset Commands", EditorStyles.boldLabel);
        EditorGUILayout.BeginScrollView(new Vector2(0, 0), GUILayout.Height(200));
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Letters A-Z", GUILayout.Width(100)))
        {
            ShowPresetSection("Letters");
        }
        if (GUILayout.Button("Numbers 0-9", GUILayout.Width(100)))
        {
            ShowPresetSection("Numbers");
        }
        if (GUILayout.Button("Special Keys", GUILayout.Width(100)))
        {
            ShowPresetSection("Special");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Quick send buttons for common commands
        EditorGUILayout.LabelField("Quick Send:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Q", GUILayout.Width(40)))
        {
            SendUDPPacket("press the letter Q");
        }
        if (GUILayout.Button("E", GUILayout.Width(40)))
        {
            SendUDPPacket("press the letter E");
        }
        if (GUILayout.Button("Space", GUILayout.Width(60)))
        {
            SendUDPPacket("press space");
        }
        if (GUILayout.Button("Enter", GUILayout.Width(60)))
        {
            SendUDPPacket("press enter");
        }
        if (GUILayout.Button("Esc", GUILayout.Width(50)))
        {
            SendUDPPacket("press escape");
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // All letters grid
        EditorGUILayout.LabelField("All Letters:", EditorStyles.miniLabel);
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int buttonsPerRow = 13;
        for (int i = 0; i < letters.Length; i += buttonsPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < buttonsPerRow && (i + j) < letters.Length; j++)
            {
                char letter = letters[i + j];
                if (GUILayout.Button(letter.ToString(), GUILayout.Width(25)))
                {
                    SendUDPPacket($"press the letter {letter}");
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        // Sent messages log
        EditorGUILayout.LabelField($"Sent Messages ({sentMessages.Count})", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        
        for (int i = sentMessages.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}. {sentMessages[i]}", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Resend", GUILayout.Width(60)))
            {
                SendUDPPacket(sentMessages[i]);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("Clear Log"))
        {
            sentMessages.Clear();
        }
        
        EditorGUILayout.Space(10);
        
        // Instructions
        EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Start Unity in Play Mode\n" +
            "2. Ensure PLCUDPListener component is active in scene\n" +
            "3. Verify port matches PLCUDPListener port (default: 9876)\n" +
            "4. Type command or click preset buttons\n" +
            "5. Check Unity Console for received messages\n" +
            "6. Check PLCUDPListener debugMode for detailed logs", 
            MessageType.Info);
    }
    
    void SendUDPPacket(string message)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "Unity must be in Play Mode to send UDP packets!", "OK");
            return;
        }
        
        if (udpClient == null)
        {
            try
            {
                udpClient = new UdpClient();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create UDP client: {e.Message}");
                return;
            }
        }
        
        try
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(targetIP, out ipAddress))
            {
                EditorUtility.DisplayDialog("Error", $"Invalid IP address: {targetIP}", "OK");
                return;
            }
            
            IPEndPoint endPoint = new IPEndPoint(ipAddress, targetPort);
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, endPoint);
            
            sentMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[UDP Tester] Sent to {targetIP}:{targetPort} - {message}");
            
            // Keep only last 50 messages
            if (sentMessages.Count > 50)
            {
                sentMessages.RemoveAt(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UDP Tester] Failed to send packet: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to send UDP packet:\n{e.Message}", "OK");
        }
    }
    
    void ShowPresetSection(string section)
    {
        // This could be expanded to show different preset categories
        Debug.Log($"Showing {section} presets");
    }
}



