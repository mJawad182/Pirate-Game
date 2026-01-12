# Quick Start - One-Click Setup

## 🚀 One-Click Setup

The Pirate Escape Room game now includes a **One-Click Setup** system that automatically creates and configures all necessary components!

### How to Use:

#### Method 1: Menu Bar (Recommended)
1. In Unity Editor, go to: **Tools > Pirate Escape Room > One-Click Setup (Quick)**
2. Click "Yes" when prompted
3. Done! All systems are now set up

#### Method 2: Setup Window
1. Go to: **Tools > Pirate Escape Room > One-Click Setup**
2. Review the setup information
3. Click "Run Setup" button
4. All systems are configured automatically

#### Method 3: Inspector Button
1. Add `RuntimeSetup` component to any GameObject
2. Right-click the component in Inspector
3. Select "Run Runtime Setup (Play Mode Only)" - for runtime testing only

### What Gets Created:

✅ **GameManager** - Main game coordinator (singleton)  
✅ **MultiCameraManager** - 8-camera system for multi-display  
✅ **PLCUDPListener** - UDP listener for PLC communication  
✅ **StormController** - Approaching storm system  
✅ **ShipInteraction** - Ship interaction handler  
✅ **Ship GameObject** - Basic ship (if missing) with Rigidbody  
✅ **UI Canvas** - Interaction prompt UI  
✅ **CrestWaterInteraction** - Added to ship if it has Rigidbody  

### What Gets Configured:

✅ All component references are automatically linked  
✅ Camera manager connected to ship  
✅ Ship interaction connected to PLC listener  
✅ GameManager references all systems  
✅ UI prompt linked to ship interaction  

### After Setup:

1. **Replace Ship Model**: The setup creates a basic capsule - replace with your ship model
2. **Configure Cameras**: Adjust camera angles, heights, and distances in MultiCameraManager
3. **Setup Storm**: Add storm cloud prefabs and particle systems to StormController
4. **Add Puzzles**: Create EscapeRoomPuzzle instances in GameManager
5. **Configure Audio**: Add audio sources for music, thunder, and wind
6. **Test PLC**: Ensure UDP port matches your PLC configuration

### Troubleshooting:

**"Component already exists"**  
- The setup will use existing components instead of creating duplicates
- This is safe - existing configurations are preserved

**"Ship not found"**  
- Setup creates a basic ship GameObject
- Replace it with your actual ship model

**"UI Canvas already exists"**  
- Setup detects existing canvas and uses it
- Interaction prompt is added if missing

### Next Steps:

1. ✅ Run One-Click Setup
2. 📦 Import your ship model and replace the basic ship
3. 🎥 Configure camera positions and angles
4. ⚡ Add storm visual effects (clouds, particles)
5. 🧩 Create puzzles in GameManager
6. 🔊 Add audio sources and clips
7. 🧪 Test with PLC or UDP sender tool
8. 🎮 Build and deploy!

---

**Need Help?** Check `SETUP_GUIDE.md` for detailed documentation.



