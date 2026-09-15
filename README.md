# Dead Dawn: Zombie Survival Mobile

An isometric mobile and PC zombie survival and base-building game built with **Unity 6** and the **Universal Render Pipeline (URP)**.

---

## 🎮 Game Overview

**Dead Dawn** blends fast-paced zombie horde combat with strategic perimeter scavenging, workbench crafting, and fixed-plot base fortification.

### Core Gameplay Loop
1. **Scavenge**: Venture outside the fortified perimeter to gather Wood, Scrap, and Gunpowder.
2. **Craft**: Return to the central base crafting bench to forge ammunition, wooden barricades, and repair kits.
3. **Fortify**: Rebuild and repair defensive palisade walls and entry gates around your base.
4. **Survive**: Defend against escalating zombie waves attacking both you and your structures.

---

## ✨ Key Features

- **Mobile-First Dual Controls**:
  - **Left Screen Drag**: Floating virtual joystick for smooth 360° isometric movement.
  - **Right Screen Tap**: Tap-to-shoot in player facing direction with accidental-fire prevention.
  - **PC / Keyboard Support**: WASD / Arrow keys with 45° isometric alignment, Left-Shift sprint, and Mouse click to shoot.
- **Section-Wise Base Fortification**:
  - Intuitive fixed structural plots (North, South, West, and East Gate palisade sections).
  - Multi-state structures: Unbuilt Foundation ➔ Fortified Structure.
  - Interactive proximity prompt for instant building and repairing with dynamic bounce animations.
- **Interactive Crafting Workbench**:
  - Time-freezing crafting modal with real-time resource check and recipe costs.
  - Recipes: Pistol Ammo (+10), Wood Barricade (+1), Repair Kit (+30 HP).
- **Combat & Arsenal**:
  - Responsive shooting mechanics, magazine reloading, and HUD ammo counters.
  - Screen shake feedback on taking damage and dynamic health indicator.
- **Event-Driven Architecture**:
  - Decoupled systems utilizing a centralized `EventBus` for high performance and maintainability.

---

## 🛠️ Tech Stack & Engine Requirements

- **Engine**: Unity 6 (6000.x)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: Unity New Input System
- **GUI**: Unity UI (uGUI) / TextMeshPro (with planned Figma integration for final production assets)
- **Target Platforms**: Android, iOS, PC / WebGL

---

## 🚀 Getting Started

### Prerequisites
- Install **Unity Hub** and **Unity 6** (6000.x or later) with Android/iOS Build Support modules if targeting mobile.

### Opening the Project
1. Clone this repository:
   ```bash
   git clone https://github.com/Sub0dh23/Zombie-Survival-Mobile.git
   ```
2. Open **Unity Hub** and click **Add** > **Add project from disk**.
3. Select the cloned folder and open it with Unity 6.
4. Open the main gameplay scene:
   - Navigate to `Assets/_Game/Scenes/` (or `Assets/Scenes/MainScene.unity`).
5. Press the **Play** button in the Unity Editor to test.

---

## ⌨️ Controls

| Action | Mobile | PC (Keyboard & Mouse) |
| :--- | :--- | :--- |
| **Move** | Drag left side of screen | `W`, `A`, `S`, `D` or Arrow Keys |
| **Sprint** | Drag outward | `Left Shift` |
| **Shoot** | Tap right side of screen | `Mouse Left-Click` or `Space` |
| **Interact / Craft** | Tap contextual UI button | `E` or Click button |
| **Pause / Resume** | Tap Pause button | `Escape` |

---

## 📁 Project Structure

```
Assets/
├── _Game/
│   ├── Scripts/          # Core gameplay, Player, Enemies, Crafting, Events
│   ├── Prefabs/          # Player, Zombie, Walls, Crafting Bench prefabs
│   ├── Materials/        # Custom URP materials and shaders
│   ├── Scenes/           # Main game and prototype levels
│   └── UI/               # HUD, Crafting modal, Icons & Sprites
├── Settings/             # URP render pipeline settings and quality profiles
└── TextMesh Pro/         # Fonts and TextMeshPro resources
```

---

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.
