# Pirate Escape Room Game - Setup Guide

## Overview
This Unity project is designed for a physical pirate escape room with 8 display outputs, PLC integration, reactive water, and interactive ship mechanics.

## Scripts Overview

### 1. **MultiCameraManager.cs**
Manages 8 first-person cameras positioned around the ship, each rendering to a different display.

**Setup:**
- Attach to an empty GameObject in your scene
- Assign the ship/player Transform to `shipTransform`
- Configure camera angles (default: 8 cameras at 45° intervals)
- Adjust `cameraHeight` and `cameraDistance` as needed
- Ensure your computer has 8 displays connected and configured in Windows

**Camera Angles (Default):**
- Camera 0: 0° (Front)
- Camera 1: 45° (Front-Right)
- Camera 2: 90° (Right)
- Camera 3: 135° (Back-Right)
- Camera 4: 180° (Back)
- Camera 5: 225° (Back-Left)
- Camera 6: 270° (Left)
- Camera 7: 315° (Front-Left)

### 2. **PLCUDPListener.cs**
Listens for UDP packets from your PLC and simulates keyboard input.

**Setup:**
- Attach to an empty GameObject
- Set `listenPort` to match your PLC's sending port (default: 9876)
- Enable `debugMode` to see received messages
- Other scripts can check `IsSimulatedKeyDown(KeyCode)` to detect PLC inputs

**PLC Message Format:**
- "press the letter Q" - Simulates pressing Q
- "press Q" - Simulates pressing Q
- "press space" - Simulates pressing Space
- Supports letters A-Z, numbers 0-9, and common keys (space, enter, escape, etc.)

**Network Configuration:**
- Ensure Unity and PLC are on the same network
- Configure firewall to allow UDP on the specified port
- Test with a UDP sender tool before connecting PLC

### 3. **CrestWaterInteraction.cs**
Integrates with Crest Ocean System for reactive water physics.

**Setup:**
- Attach to any object that should interact with water (ship, debris, etc.)
- Requires Rigidbody component
- Assign `oceanRenderer` if you have Crest OceanRenderer in scene
- Adjust `buoyancy`, `waterDrag`, and `waterAngularDrag` for desired behavior
- Set `waveInteractionStrength` > 0 to make objects create waves

**Crest Integration:**
- Works with Crest Ocean System (if installed)
- Falls back to simple water level if Crest is not available
- Uses reflection to access Crest API without hard dependencies

### 4. **StormController.cs**
Controls the approaching storm with visual and audio effects.

**Setup:**
- Attach to an empty GameObject
- Assign `shipTransform` (or it will auto-find)
- Create/assign storm cloud prefab or particle systems
- Configure `stormApproachSpeed` and `initialStormDistance`
- Assign audio sources for thunder and wind
- Connect to wave controller for dynamic wave effects

**Features:**
- Storm approaches ship over time
- Lightning flashes increase as storm nears
- Thunder sounds synchronized with lightning
- Wave amplitude increases with storm intensity
- Triggers `OnStormArrived` event when storm reaches minimum distance

### 5. **ShipInteraction.cs**
Handles interactions with other ships in the scene.

**Setup:**
- Attach to player/ship GameObject
- Set `interactionRange` for detection distance
- Assign UI prompt GameObject (optional)
- Create ships with `InteractableShip` component
- Assign `plcListener` reference for PLC input support

**InteractableShip Component:**
- Add to any ship GameObject
- Set `shipName` and `interactionType`
- Use `interactionData` for puzzle-specific information
- `OnInteracted` event fires when ship is interacted with

**Interaction Types:**
- Signal: Flag/lights communication
- Trade: Item exchange
- Battle: Combat interaction
- Puzzle: Puzzle-solving interaction

### 6. **GameManager.cs**
Main coordinator for all game systems.

**Setup:**
- Attach to an empty GameObject (singleton)
- Assign references to all other managers
- Create `EscapeRoomPuzzle` instances in the inspector
- Configure `timeLimit` if desired
- Assign UI GameObjects

**Puzzle System:**
- Create puzzles in the inspector
- Set requirements (ship name, key, PLC command)
- Puzzles auto-solve when requirements are met
- `OnPuzzleSolved` event fires for each solved puzzle
- Game wins when all puzzles are solved

## Scene Setup

### Required GameObjects:
1. **Ship** - Main player ship with Transform
2. **OceanRenderer** - Crest Ocean System (if using Crest)
3. **Storm System** - StormController with effects
4. **Other Ships** - GameObjects with InteractableShip component
5. **Camera Manager** - GameObject with MultiCameraManager
6. **PLC Listener** - GameObject with PLCUDPListener
7. **Game Manager** - GameObject with GameManager (singleton)

### Display Setup:
1. Connect 8 displays to your computer
2. Configure displays in Windows Display Settings
3. Arrange displays in a circle around the physical room
4. Ensure all displays are active and recognized
5. Unity will activate displays 1-7 on Start (Display 0 is always active)

## PLC Integration

### UDP Packet Format:
Send plain text UDP packets to Unity's IP address on the configured port.

**Examples:**
```
"press the letter Q"
"press Q"
"press space"
"press enter"
```

### Testing:
Use a UDP sender tool (like Packet Sender) to test before connecting PLC:
- IP: Computer running Unity's IP address
- Port: 9876 (or configured port)
- Protocol: UDP
- Message: "press the letter Q"

## Crest Water System

### Installation:
1. Download Crest Ocean System from GitHub
2. Import into Unity project
3. Add OceanRenderer prefab to scene
4. Configure ocean settings (waves, wind, etc.)

### Reactive Water:
- Objects with `CrestWaterInteraction` automatically interact with water
- Adjust `waveInteractionStrength` to control wave generation
- Objects create dynamic waves as they move through water

## Testing

### Debug Mode:
Enable `debugMode` on scripts to see detailed logs:
- PLCUDPListener: Shows all received UDP messages
- MultiCameraManager: Shows camera creation and display activation
- GameManager: Shows game state changes and puzzle solving

### Keyboard Shortcuts (Debug):
- **R**: Restart game
- **P**: Solve all puzzles (debug only)

## Troubleshooting

### Cameras Not Showing on Displays:
- Check Windows Display Settings - all displays must be active
- Verify display count: `Display.displays.Length` should be 8
- Check camera `targetDisplay` settings (0-7)
- Ensure cameras are enabled and rendering

### PLC Not Communicating:
- Verify network connectivity between PLC and Unity computer
- Check firewall settings (allow UDP on port)
- Verify port number matches in both systems
- Enable `debugMode` to see if packets are received
- Test with UDP sender tool first

### Water Not Reacting:
- Ensure Crest Ocean System is installed and OceanRenderer is in scene
- Check `CrestWaterInteraction` component is attached to objects
- Verify Rigidbody is present on objects
- Adjust `waveInteractionStrength` if waves are too weak

### Storm Not Moving:
- Check `stormActive` is enabled
- Verify `shipTransform` is assigned
- Check `stormApproachSpeed` is > 0
- Ensure storm prefab/object exists

## Performance Considerations

- 8 cameras rendering simultaneously is resource-intensive
- Consider reducing `farClipPlane` on cameras
- Lower `fieldOfView` if needed
- Use LOD (Level of Detail) for distant objects
- Optimize ocean rendering settings in Crest
- Consider using occlusion culling

## Next Steps

1. Set up your 3D scene with ship, ocean, and islands
2. Configure all 8 displays
3. Test PLC communication
4. Add interactable ships with puzzles
5. Configure storm timing for your escape room duration
6. Test with players!



