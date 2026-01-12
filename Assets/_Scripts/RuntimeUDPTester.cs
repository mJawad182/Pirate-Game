using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Runtime UDP Tester UI for testing PLC commands during gameplay.
/// Attach to a Canvas GameObject with UI elements.
/// </summary>
public class RuntimeUDPTester : MonoBehaviour
{
    [Header("UI References")]
    public InputField ipInputField;
    public InputField portInputField;
    public InputField messageInputField;
    public Button sendButton;
    public Text logText;
    public ScrollRect logScrollRect;
    
    [Header("Settings")]
    public string defaultIP = "127.0.0.1";
    public int defaultPort = 9876;
    
    [Header("Quick Buttons")]
    public Button[] quickButtons;
    public string[] quickCommands = new string[]
    {
        "press the letter Q",
        "press the letter E",
        "press space",
        "press enter"
    };
    
    private UdpClient udpClient;
    private List<string> logMessages = new List<string>();
    private const int maxLogLines = 50;
    
    void Start()
    {
        InitializeUI();
        InitializeUDP();
    }
    
    void InitializeUI()
    {
        // Set default values
        if (ipInputField != null)
        {
            ipInputField.text = defaultIP;
        }
        
        if (portInputField != null)
        {
            portInputField.text = defaultPort.ToString();
        }
        
        // Setup send button
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendCurrentMessage);
        }
        
        // Setup quick buttons
        if (quickButtons != null && quickCommands != null)
        {
            for (int i = 0; i < quickButtons.Length && i < quickCommands.Length; i++)
            {
                int index = i; // Capture for closure
                quickButtons[i].onClick.AddListener(() => SendUDPPacket(quickCommands[index]));
            }
        }
        
        // Setup enter key on message input
        if (messageInputField != null)
        {
            messageInputField.onEndEdit.AddListener((text) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    SendCurrentMessage();
                }
            });
        }
        
        AddLog("UDP Tester initialized. Ready to send commands.");
    }
    
    void InitializeUDP()
    {
        try
        {
            udpClient = new UdpClient();
        }
        catch (System.Exception e)
        {
            AddLog($"Error initializing UDP client: {e.Message}", true);
        }
    }
    
    void SendCurrentMessage()
    {
        if (messageInputField != null && !string.IsNullOrEmpty(messageInputField.text))
        {
            SendUDPPacket(messageInputField.text);
            messageInputField.text = ""; // Clear after sending
        }
    }
    
    public void SendUDPPacket(string message)
    {
        if (udpClient == null)
        {
            InitializeUDP();
            if (udpClient == null)
            {
                AddLog("UDP client not initialized!", true);
                return;
            }
        }
        
        try
        {
            // Get IP and port from UI or use defaults
            string ip = defaultIP;
            int port = defaultPort;
            
            if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
            {
                ip = ipInputField.text;
            }
            
            if (portInputField != null && !string.IsNullOrEmpty(portInputField.text))
            {
                if (!int.TryParse(portInputField.text, out port))
                {
                    AddLog("Invalid port number!", true);
                    return;
                }
            }
            
            IPAddress ipAddress;
            if (!IPAddress.TryParse(ip, out ipAddress))
            {
                AddLog($"Invalid IP address: {ip}", true);
                return;
            }
            
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, endPoint);
            
            AddLog($"Sent: {message} → {ip}:{port}");
            Debug.Log($"[Runtime UDP Tester] Sent: {message} to {ip}:{port}");
        }
        catch (System.Exception e)
        {
            AddLog($"Error sending packet: {e.Message}", true);
            Debug.LogError($"[Runtime UDP Tester] Error: {e.Message}");
        }
    }
    
    void AddLog(string message, bool isError = false)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        
        logMessages.Add(logEntry);
        
        // Keep only last N lines
        if (logMessages.Count > maxLogLines)
        {
            logMessages.RemoveAt(0);
        }
        
        // Update UI text
        if (logText != null)
        {
            logText.text = string.Join("\n", logMessages);
            if (isError)
            {
                logText.color = Color.red;
            }
            else
            {
                logText.color = Color.white;
            }
        }
        
        // Auto-scroll to bottom
        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    // Public methods for external scripts
    public void SendLetter(char letter)
    {
        SendUDPPacket($"press the letter {letter}");
    }
    
    public void SendKey(string keyName)
    {
        SendUDPPacket($"press {keyName}");
    }
    
    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}



