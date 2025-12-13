# ONE-CLICK SCENE CREATION

## Automatic Scene Setup

I've created an editor script that will create the entire scene for you automatically.

### How to Use:

1. **Open Unity**
2. **Go to the top menu**: `Tools` → `Create Test Scene`
3. **That's it!** The scene will be created and automatically start playing

### What It Creates:

- ✅ Floor (gray plane)
- ✅ Player (blue capsule with WASD controls)
- ✅ 5 interactable objects (colorful cubes)
- ✅ Camera (positioned at perfect angle)
- ✅ Saves the scene as `Assets/Scenes/TestScene.unity`
- ✅ Automatically starts playing

### Controls:

| Key | Action |
|-----|--------|
| **W/A/S/D** | Move around |
| **E** | Grab/Release object |
| **C** | Cut object (splits in 2) |
| **B** | Break object (destroys) |

### What You'll See:

- Blue capsule = Your player
- Colorful cubes = Objects you can interact with
- Yellow highlight = Object you can interact with (when close)

---

## If the Menu Doesn't Appear

If you don't see the "Tools" menu option:

1. Make sure Unity has compiled the scripts (check bottom-right corner)
2. Wait for compilation to finish
3. The menu will appear at the top: `Tools` → `Create Test Scene`

---

## Manual Alternative

If you prefer to do it manually, you can also:

1. Create a new scene
2. Add an empty GameObject
3. Add the `QuickTestSetup` component
4. Click Play

But the menu option is much faster! 🚀
