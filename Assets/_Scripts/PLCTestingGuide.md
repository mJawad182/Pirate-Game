# PLC Testing Guide - UDP Command Testing

## Overview
This guide explains how to test PLC-controlled physical puzzles that send UDP commands to Unity, without needing the actual PLC hardware.

## Testing Methods

### Method 1: Unity Editor UDP Tester (Recommended)

**Access:** `Tools > Pirate Escape Room > UDP Tester`

**Steps:**
1. **Start Unity in Play Mode**
   - Your scene must be running for UDP packets to be received
   - Ensure `PLCUDPListener` component is active in the scene

2. **Open UDP Tester Window**
   - Go to: `Tools > Pirate Escape Room > UDP Tester`
   - The window will open showing connection settings and controls

3. **Configure Settings**
   - **Target IP:** Usually `127.0.0.1` (localhost) for testing on same machine
   - **Target Port:** Default is `9876` (must match PLCUDPListener port)

4. **Send Test Commands**
   - Type a command in the message field (e.g., "press the letter Q")
   - Click "Send UDP Packet" button
   - Or use quick buttons for common commands
   - Or click letter buttons (A-Z) for quick testing

5. **Verify Reception**
   - Check Unity Console for received messages
   - Enable `debugMode` on PLCUDPListener for detailed logs
   - Your game scripts should respond to the simulated key presses

**Features:**
- ✅ Preset commands for all letters A-Z
- ✅ Quick buttons for common keys (Q, E, Space, Enter, Esc)
- ✅ Message log showing all sent commands
- ✅ Resend functionality
- ✅ Works in Editor Play Mode

### Method 2: Runtime UDP Tester UI

**Setup:**
1. Add `RuntimeUDPTester` component to a Canvas GameObject
2. Create UI elements:
   - InputField for IP address
   - InputField for port number
   - InputField for message
   - Button to send
   - Text/ScrollRect for log display
   - Optional: Quick action buttons

3. Assign UI references in the inspector

**Usage:**
- Test UDP commands during gameplay
- Useful for in-game testing and debugging
- Can be hidden/disabled for production builds

### Method 3: External UDP Sender Tools

**Recommended Tools:**
- **Packet Sender** (Free, cross-platform)
  - Download: https://packetsender.com/
  - Simple GUI for sending UDP packets
  - Supports saving presets

- **Netcat (nc)** - Command line tool
  ```bash
  # Windows (PowerShell)
  $udpClient = New-Object System.Net.Sockets.UdpClient
  $endPoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse("127.0.0.1"), 9876)
  $bytes = [System.Text.Encoding]::UTF8.GetBytes("press the letter Q")
  $udpClient.Send($bytes, $bytes.Length, $endPoint)
  $udpClient.Close()
  
  # Linux/Mac
  echo "press the letter Q" | nc -u 127.0.0.1 9876
  ```

- **Python Script**
  ```python
  import socket
  UDP_IP = "127.0.0.1"
  UDP_PORT = 9876
  MESSAGE = "press the letter Q"
  
  sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
  sock.sendto(MESSAGE.encode(), (UDP_IP, UDP_PORT))
  ```

## Command Format

### Supported Formats:
```
"press the letter Q"    ✅ Recommended format
"press Q"              ✅ Also works
"press space"          ✅ Special keys
"press enter"          ✅ Special keys
"press escape"         ✅ Special keys
```

### Supported Keys:
- **Letters:** A-Z (case insensitive)
- **Numbers:** 0-9
- **Special Keys:** space, enter, escape, shift, ctrl, alt

## Testing Checklist

### Pre-Testing Setup:
- [ ] Unity scene is running in Play Mode
- [ ] PLCUDPListener component is active
- [ ] PLCUDPListener port matches sender port (default: 9876)
- [ ] PLCUDPListener `debugMode` is enabled (for logging)
- [ ] Firewall allows UDP on the port (if testing across network)

### Basic Functionality Test:
- [ ] Send "press the letter Q" → Check console for reception
- [ ] Send "press space" → Verify space key simulation
- [ ] Send multiple rapid commands → Verify all are received
- [ ] Check PLCUDPListener logs for each command

### Integration Test:
- [ ] Verify simulated keys trigger game actions
- [ ] Test with ShipInteraction (press E to interact)
- [ ] Test with puzzle triggers
- [ ] Verify no duplicate key presses
- [ ] Test error handling (invalid commands)

### Network Test (if using separate machine):
- [ ] Test with localhost (127.0.0.1)
- [ ] Test with local network IP
- [ ] Verify firewall settings
- [ ] Test packet loss scenarios
- [ ] Test with multiple simultaneous senders

## Troubleshooting

### "No messages received"
1. **Check Unity is in Play Mode**
   - UDP listener only works during Play Mode

2. **Verify Port Number**
   - Default is 9876
   - Check PLCUDPListener inspector
   - Ensure sender uses same port

3. **Check Firewall**
   - Windows Firewall may block UDP
   - Add exception for Unity or the port

4. **Verify IP Address**
   - Use `127.0.0.1` for same machine
   - Use actual IP for network testing
   - Check `ipconfig` (Windows) or `ifconfig` (Linux/Mac)

5. **Enable Debug Mode**
   - Set `debugMode = true` on PLCUDPListener
   - Check Console for detailed logs

### "Messages received but no action"
1. **Check Key Code Mapping**
   - Verify the key is in the keyCodeMap
   - Check console for "Unknown key" warnings

2. **Verify Game Scripts**
   - Ensure scripts check `IsSimulatedKeyDown()`
   - Or subscribe to `OnSimulatedKeyPressed` event

3. **Check Command Format**
   - Use exact format: "press the letter Q"
   - Case doesn't matter, but spelling does

### "Port already in use"
1. **Close other Unity instances**
2. **Check for other UDP listeners**
3. **Change port number** in PLCUDPListener

## Testing Physical Puzzles

### Simulating Puzzle States:

**Puzzle 1: Button Press**
```
Send: "press the letter Q"
Expected: Ship interaction or puzzle trigger
```

**Puzzle 2: Sequence Input**
```
Send: "press the letter A"
Send: "press the letter B"
Send: "press the letter C"
Expected: Sequence recognition
```

**Puzzle 3: Multiple Buttons**
```
Send: "press the letter E" (interact)
Send: "press space" (confirm)
Expected: Multi-step puzzle progression
```

### Testing Scenarios:

1. **Single Button Press**
   - Send one command
   - Verify immediate response
   - Check for duplicate triggers

2. **Rapid Fire**
   - Send multiple commands quickly
   - Verify all are processed
   - Check for missed commands

3. **Invalid Commands**
   - Send malformed messages
   - Verify error handling
   - Check console warnings

4. **Network Latency**
   - Test with network delay
   - Verify command queue works
   - Check for command loss

## Production Checklist

Before deploying to physical escape room:

- [ ] Test all puzzle commands
- [ ] Verify network connectivity
- [ ] Test with actual PLC hardware
- [ ] Verify firewall rules
- [ ] Test failover scenarios
- [ ] Document IP addresses and ports
- [ ] Create command reference sheet
- [ ] Test emergency stop commands
- [ ] Verify logging is adequate
- [ ] Test with multiple players

## Example Test Script

Create a test script to automate testing:

```csharp
public class PLCTestAutomation : MonoBehaviour
{
    public PLCUDPListener plcListener;
    private int testIndex = 0;
    private string[] testCommands = { "press the letter Q", "press E", "press space" };
    
    void Start()
    {
        if (plcListener != null)
        {
            plcListener.OnSimulatedKeyPressed += OnKeyPressed;
        }
    }
    
    void OnKeyPressed(KeyCode key)
    {
        Debug.Log($"Test: Received key {key}");
        // Verify expected behavior
    }
    
    [ContextMenu("Run Test Sequence")]
    void RunTestSequence()
    {
        StartCoroutine(SendTestCommands());
    }
    
    IEnumerator SendTestCommands()
    {
        foreach (string cmd in testCommands)
        {
            // Send via UDP tester or external tool
            yield return new WaitForSeconds(1f);
        }
    }
}
```

## Quick Reference

**Default Settings:**
- Port: `9876`
- IP (local): `127.0.0.1`
- Format: `"press the letter X"`

**Common Commands:**
- Interact: `"press the letter E"`
- Confirm: `"press space"` or `"press enter"`
- Cancel: `"press escape"`
- Puzzle triggers: `"press the letter Q"`, `"press the letter A"`, etc.

**Debug Commands:**
- Enable `debugMode` on PLCUDPListener
- Check Unity Console for logs
- Use UDP Tester window for visual feedback

---

**Need Help?** Use the built-in UDP Tester tool: `Tools > Pirate Escape Room > UDP Tester`



