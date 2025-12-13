# Quick Start Guide

Get your multiplayer game running in 5 minutes!

## Prerequisites

- Unity 2021.3 or later
- FastAPI backend running at `http://127.0.0.1:8000`

## Step 1: Install Required Packages

1. Open Package Manager (`Window` → `Package Manager`)
2. Install:
   - **Input System** (com.unity.inputsystem)
   - **TextMeshPro** (com.unity.textmeshpro)

## Step 2: Configure Input System

1. Create Input Actions asset:
   - `Assets` → `Create` → `Input Actions`
   - Name it `InputSystem_Actions`
   - Follow the configuration in `InputActions.md`

2. Or use the existing `InputSystem_Actions.inputactions` file

## Step 3: Create Test Scene

### Option A: Automatic Setup (Recommended)

1. Create a new scene: `File` → `New Scene`
2. Save it as `GameScene`
3. Create empty GameObject, name it "SceneSetup"
4. Add `ExampleSetup` component
5. Click Play - scene will auto-populate!

### Option B: Manual Setup

1. Create a new scene
2. Add these GameObjects:
   - **NetworkManager** (empty GameObject)
     - Add `NetworkManager` component
     - Set server URL: `http://127.0.0.1:8000`
   - **GameStateManager** (empty GameObject)
     - Add `GameStateManager` component
   - **Main Camera**
     - Position: (0, 10, -10)
     - Rotation: (45, 0, 0)

## Step 4: Create Player Prefab

1. Create a Capsule: `GameObject` → `3D Object` → `Capsule`
2. Name it "Player"
3. Add components:
   - `Rigidbody` (freeze rotation X, Y, Z)
   - `PlayerController`
   - `PlayerInput` (assign InputSystem_Actions, set to "Player" map)
4. Save as prefab in `Assets/Resources/`
5. Delete from scene

## Step 5: Create Spectator Camera Prefab

1. Create empty GameObject, name it "SpectatorCamera"
2. Add Camera as child
3. Add components to parent:
   - `SpectatorCamera`
   - `PlayerInput` (assign InputSystem_Actions, set to "Spectator" map)
4. Save as prefab in `Assets/Resources/`
5. Delete from scene

## Step 6: Create Interactable Object

1. Create a Cube: `GameObject` → `3D Object` → `Cube`
2. Name it "InteractableBox"
3. Add components:
   - `Rigidbody`
   - `InteractableObject`
4. Create materials:
   - Normal material (any color)
   - Highlight material (bright yellow/green)
5. Assign materials to InteractableObject
6. Save as prefab in `Assets/Resources/`

## Step 7: Assign References

In GameStateManager:
- Assign Player prefab
- Assign Spectator Camera prefab
- Create two empty GameObjects as spawn points
  - Player1SpawnPoint at (-5, 0, 0)
  - Player2SpawnPoint at (5, 0, 0)

## Step 8: Create Main Menu (Optional)

1. Create new scene: `MainMenu`
2. Create Canvas with:
   - Room browser panel
   - Create room panel
   - Lobby panel
3. Add `RoomUI` component to Canvas
4. Wire up UI elements

Or skip this and test directly in GameScene!

## Step 9: Test Locally

### Single Player Test:
1. Start the backend: `uvicorn app.main:app --reload`
2. Click Play in Unity
3. Open Console to see connection logs

### Multiplayer Test:
1. Build the game: `File` → `Build Settings` → `Build`
2. Run the build
3. Click Play in Unity Editor
4. Both should connect to the same room!

## Quick Test Checklist

- [ ] Backend is running
- [ ] NetworkManager in scene with correct URL
- [ ] GameStateManager has prefab references
- [ ] Player prefab has PlayerController + PlayerInput
- [ ] Spectator prefab has SpectatorCamera + PlayerInput
- [ ] InteractableObject in scene with Rigidbody
- [ ] Input System package installed

## Common Issues

### "Input System not found"
→ Install Input System package and restart Unity

### "WebSocket connection failed"
→ Check backend is running and URL is correct

### "Player not spawning"
→ Check GameStateManager has player prefab assigned

### "Can't interact with objects"
→ Check InteractableObject has Rigidbody and correct layer

## Next Steps

1. Customize player appearance
2. Add more interactable objects
3. Create custom actions
4. Add visual effects
5. Build UI for room management

## Testing Actions

Once in-game:
- **WASD** - Move
- **E** - Grab/Release object
- **C** - Cut object (creates 2 pieces)
- **B** - Break object (destroys it)

As spectator:
- **WASD** - Fly around
- **Mouse** - Look
- **Shift** - Fast move
- **F** - Toggle follow player

Enjoy your multiplayer game! 🎮
