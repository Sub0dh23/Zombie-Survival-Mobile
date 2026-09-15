# Dead Dawn: Zombie Survival Prototype — Development Progress

**Project**: Zombie-Game (LILA)  
**Engine & Pipeline**: Unity 6 (URP - Universal Render Pipeline)  
**Target Platforms**: Mobile & PC  
**Architecture**: Event-Driven (`EventBus`), Modular Components, Mobile-First Input  

---

## 1. Executive Summary
This prototype is an isometric mobile/PC zombie survival and base-building game. The core gameplay loop combines scavenging resources in an expanding dangerous perimeter, fortifying a central base station via section-wise structural upgrades (Gardenscapes-style fixed plots), crafting essential survival equipment at the station workbench, and preparing for recurring zombie horde attacks.

---

## 2. Completed Milestones & Implemented Features

### 2.1 Player & Mobile Controls
- **Movement (`PlayerController.cs`)**:
  - **Left-Hand Touch Drag**: Floating touch tracker on left half of screen (0%–50% width) allowing smooth virtual joystick movement.
  - **Keyboard / Gamepad Support**: WASD / Arrow keys with 45° isometric rotation alignment and Left-Shift sprint.
  - **Boundary Enforcer**: Clamps player movement inside the currently unlocked base/scavenge zone.
- **Combat & Shooting (`PlayerShooting.cs`)**:
  - **Tap-To-Shoot**: Tapping on right half of screen (or Mouse Left-Click / Space) shoots in player facing direction.
  - **Ammo & Reserve System**: Magazine tracking, real-time HUD event publishing (`PlayerAmmoChangedEvent`).
  - **Accidental Fire Guard**: Release-guarded debounce and `Time.timeScale` checks that prevent any stray bullets from firing when clicking/tapping UI buttons or resuming the game.
- **Health & Inventory (`PlayerHealth.cs`, `PlayerInventory.cs`)**:
  - 100 Max HP with screen shake on damage (`ScreenShakeEvent`), invulnerability window, and game over state.
  - Tracks Wood, Scrap, Gunpowder, and Barricade inventory with centralized event publishing (`InventoryChangedEvent`).

### 2.2 Base Station & Interactive Crafting Bench
- **Base Station (`Station_Base`)**:
  - Located at `(0, 0, 0)` with a 7.5m × 7.5m floor platform, perimeter trim, and warm lantern lighting.
- **Crafting Bench (`CraftingBench.cs`)**:
  - 3D model featuring wooden tabletop, scrap metal legs, lower shelf, tabletop vise and ammo chest, and an isometric-facing signboard.
  - **Proximity-Only Pop-Up**: Only appears when the player is within ~2.8m of the bench: `[ ⚒️ CRAFT [E] - Open Workbench ]`. When outside the base or scavenging, crafting options are hidden.
  - **Time-Freezing**: Interacting sets `Time.timeScale = 0f;`, freezing all game time, physics, upcoming zombie movement, and horde progression.
  - **Crafting Modal UI**:
    - Header with `⏸️ GAME TIME FROZEN` badge.
    - Player inventory bar (Wood, Scrap, Gunpowder, Barricades, Ammo, HP).
    - Essential recipes configured:
      1. **Pistol Ammo (+10)**: 2 Scrap, 1 Gunpowder
      2. **Wood Barricade (+1)**: 5 Wood
      3. **Repair Kit (+30 HP)**: 3 Wood, 2 Scrap
    - Color-coded cost breakdown (green if player has enough, red if insufficient) with automatically enabled/disabled Craft buttons.
    - `[✕ RESUME GAME]` button and `Esc`/`E` shortcuts that restore `Time.timeScale = 1f;`.

### 2.3 Gardenscapes-Style Section-Wise Base Progression
- **Design Philosophy**: Swapped complex freeform manual placement keys for intuitive, mobile-first fixed structural plots.
- **Section Component (`BaseBuildingSection.cs`)**:
  - Supports structural plots with two distinct visual states: **Unbuilt Foundation** (broken stakes/footing) and **Fortified Structure** (heavy wood palisade with metal braces and colliders/NavMesh obstacles).
  - Durability (`IDamageable`): 200 Max HP. Reverts to unbuilt if broken by enemies.
  - Tap-To-Build / Repair: Single proximity tap on screen:
    - Unbuilt: `[🔨 BUILD <Name> - 5 Wood]` (triggers squash-and-bounce scale animation upon construction).
    - Damaged: `[🔧 REPAIR <Name> - 2 Wood]`.
    - Intact: `[🛡️ FORTIFIED - HP: 200]`.
- **Level 1 Base Layout**:
  - **North Wall** (`Wall_North`): 7.5m solid defensive palisade.
  - **South Wall** (`Wall_South`): 7.5m solid defensive palisade.
  - **West Wall** (`Wall_West`): 7.5m solid defensive palisade.
  - **East Gate Wall** (`Wall_East`): Two defensive palisade wings with a 2.5m open archway for player entry/exit.
- **Base Coordinator (`BaseManager.cs`)**:
  - Tracks base progression and status: `🏕️ BASE LVL 1 | Fortifications: X/4`.
  - **Base Level 2 Upgrade**: When all 4 perimeter walls are fortified and the player has 15 Wood & 10 Scrap, unlocks `[⭐ UPGRADE BASE TO LEVEL 2]`. Expands player boundaries and unlocks future structural tiers.

### 2.4 Resource & World Economy
- **Resource Nodes (`ResourceNode.cs`)**:
  - Interactive resource nodes with proximity collection and color-coded materials: Wood (`MAT_Wood`), Scrap (`MAT_Scrap`), Gunpowder (`MAT_Gunpowder`).
- **Resource Spawner (`ResourceSpawner.cs`)**:
  - Procedurally spawns 18 harvest nodes in the scavenging ring outside the base perimeter (between radius 5m and 18m from base center).

---

## 3. Project Directory Map

```
Assets/
├── Scenes/
│   └── SampleScene.unity           # Primary playable prototype scene
├── _Game/
│   ├── Materials/                  # Stylized URP Lit Materials
│   │   ├── MAT_Wood.mat
│   │   ├── MAT_Scrap.mat
│   │   ├── MAT_Gunpowder.mat
│   │   ├── MAT_Player.mat
│   │   ├── MAT_Zombie.mat
│   │   └── MAT_Ground.mat
│   ├── Prefabs/
│   │   ├── Combat/
│   │   │   ├── Bullet.prefab
│   │   │   └── WoodBarricade.prefab
│   │   ├── Player/
│   │   │   └── Player.prefab
│   │   └── Resources/
│   │       ├── Node_Wood.prefab
│   │       ├── Node_Scrap.prefab
│   │       └── Node_Gunpowder.prefab
│   └── Scripts/
│       ├── Camera/
│       │   └── IsometricCamera.cs
│       ├── Combat/
│       │   ├── Barricade.cs
│       │   └── Projectile.cs
│       ├── Core/
│       │   ├── BaseBuildingSection.cs
│       │   ├── BaseManager.cs
│       │   ├── EventBus.cs
│       │   ├── GameState.cs
│       │   └── ObjectPool.cs
│       ├── Crafting/
│       │   ├── CraftingBench.cs
│       │   └── CraftingSystem.cs
│       ├── Data/
│       │   ├── Recipe_PistolAmmo.asset
│       │   ├── Recipe_RepairKit.asset
│       │   ├── Recipe_WoodBarricade.asset
│       │   ├── SO_CraftingRecipe.cs
│       │   ├── SO_Resource.cs
│       │   └── WeaponData.cs
│       ├── Player/
│       │   ├── PlayerController.cs
│       │   ├── PlayerHealth.cs
│       │   ├── PlayerInventory.cs
│       │   └── PlayerShooting.cs
│       ├── Resources/
### 2.5 Zombie Horde System & Day/Night Survival Cycle
- **Single Zombie Type (`ZombieController.cs`)**:
  - Balanced 50 HP (takes 2 pistol shots), 3.2f speed (slower than player's 6.5f speed, creating tactical evasion choices).
  - Implementation of `IDamageable` with death collapse/sink coroutine and `ZombieKilledEvent` broadcasting.
  - **NavMesh & Obstacle Pathing**: Uses Unity `NavMeshAgent` to path around barricades, base walls, and obstacles, with fail-safe kinematic movement.
  - **Strategic Base Targeting (Requirement 6)**:
    - Zombies roam/chase outside and do **not** blindly attack base fortifications.
    - If and only if a zombie witnesses the player enter inside the base perimeter (`sawPlayerEnterBase`), it advances on the shelter and attacks blocking perimeter walls/barricades to breach the base.
- **Overhead Health Bars & Floating Damage Indicators (`WorldHealthBar.cs`, `DamagePopup.cs`)**:
  - Zero-dependency procedural billboard health bars positioned above both the Player and Zombies.
  - Dynamic floating damage text (yellow for zombie hits, orange for barrier damage, red for player damage) that punches scale, floats upward, and fades out.
- **Day/Night & Horde Wave Cycle (`WaveManager.cs`)**:
  - **Scavenge Phase (80s)**: Warm sunlight; 9 ambient roaming zombies scattered in the outer ring (12m–34m) for strategic encounters while harvesting.
  - **Warning Phase (10s)**: Amber sunset lighting, warning screen rumble alerting the player of the impending assault.
  - **Horde Wave Phase**: Night moonlit atmosphere (cool blue lighting); spawns balanced wave of zombies from the outer perimeter (36m radius).
  - **Resource Balancing (Requirement 5)**: Wave count is dynamically bounded below total craftable map ammunition (Day 1: 10 horde zombies; Day 2: 14; Day 3: 18; capped safely below player ammo budget).
  - **Dawn Phase (6s)**: Golden sunrise, wave survival celebration, advances day counter and triggers daily harvest node replenishment.
- **Expanded Map & Resource Exploration (`ResourceSpawner.cs`, `PlayerController.cs`)**:
  - Resource spawn radius expanded to 6m–35m with 32 harvest nodes.
  - Player movement boundary expanded to ±38m.
  - Ground plane expanded to 100m × 100m.

---

## 3. Project Directory Map

```
Assets/
├── Scenes/
│   └── MainGame.unity              # Playable scene with Base, WaveManager & NavMesh
├── _Game/
│   ├── Materials/                  # Stylized URP Lit Materials
│   │   ├── MAT_Wood.mat
│   │   ├── MAT_Scrap.mat
│   │   ├── MAT_Gunpowder.mat
│   │   ├── MAT_Player.mat
│   │   ├── MAT_Zombie.mat
│   │   └── MAT_Ground.mat
│   ├── Prefabs/
│   │   ├── Combat/
│   │   │   ├── Bullet.prefab
│   │   │   └── WoodBarricade.prefab
│   │   ├── Enemy/
│   │   │   └── Zombie.prefab       # Prefab with NavMeshAgent, WorldHealthBar, ZombieController
│   │   ├── Player/
│   │   │   └── Player.prefab
│   │   └── Resources/
│   │       ├── Node_Wood.prefab
│   │       ├── Node_Scrap.prefab
│   │       └── Node_Gunpowder.prefab
│   └── Scripts/
│       ├── Camera/
│       │   └── IsometricCamera.cs
│       ├── Combat/
│       │   ├── Barricade.cs
│       │   └── Projectile.cs
│       ├── Core/
│       │   ├── BaseBuildingSection.cs
│       │   ├── BaseManager.cs
│       │   ├── EventBus.cs
│       │   ├── GameState.cs
│       │   ├── ObjectPool.cs
│       │   ├── RuntimeNavMeshBuilder.cs
│       │   └── WaveManager.cs
│       ├── Crafting/
│       │   ├── CraftingBench.cs
│       │   └── CraftingSystem.cs
│       ├── Data/
│       │   ├── Recipe_PistolAmmo.asset
│       │   ├── Recipe_RepairKit.asset
│       │   ├── Recipe_WoodBarricade.asset
│       │   ├── SO_CraftingRecipe.cs
│       │   ├── SO_Resource.cs
│       │   └── WeaponData.cs
│       ├── Enemy/
│       │   ├── DamagePopup.cs
│       │   ├── WorldHealthBar.cs
│       │   └── ZombieController.cs
│       ├── Player/
│       │   ├── PlayerController.cs
│       │   ├── PlayerHealth.cs
│       │   ├── PlayerInventory.cs
│       │   └── PlayerShooting.cs
│       ├── Resources/
│       │   ├── ResourceNode.cs
│       │   └── ResourceSpawner.cs
### 2.5 Zombie Horde & Combat Overhaul
- **Standardized Single Zombie Unit**: Single streamlined zombie archetype (`Zombie.prefab`) balancing speed, detection radius, and damage.
- **Ambient Roamers & Day/Night Horde Waves**:
  - Roaming ambient zombies spawn scattered across the outer exploration radius.
  - Night horde waves trigger periodically, calculated proportionally below available world resources so encounters reward strategic resource management.
  - Zombie AI targets player on sight; only breaches or targets base fortifications if player retreats inside within their chase vision.
- **Hit Detection & Damage Physics**:
  - `Projectile.cs` upgraded with `Physics.SphereCastAll` ($0.35\,\text{m}$ radius) sweep and normalized torso firing height ($y = 1.05\,\text{m}$) to eliminate tunneling.
  - Prefab kinematic rigidbodies ensured robust collision registration across URP physics steps.
- **Combat Feedback & Visuals**:
  - Real-time world-space health bars for both player and zombies (`WorldHealthBar.cs`).
  - Floating damage popups indicating damage dealt (`DamagePopup.cs`).

### 2.6 Death Screen & Run Summary UI (`DeathScreenUI.cs`)
- **Game Over Detection**: Listens for `GameStateChangedEvent` (`GameState.GameOver`) from `PlayerHealth.cs`.
- **Game Slowdown & State Pause**: Smoothly decelerates game time (`Time.timeScale = 0.25f` briefly, then `0f`) and disables player input controls.
- **Run Statistics Modal**:
  - Darkened full-screen vignette overlay to cleanly separate gameplay and UI.
  - Summarizes day reached, total zombies killed, base tier achieved, and resources held upon death.
- **Retry Mechanism**:
  - Interactive `[ 🔄 TRY AGAIN ]` button + keyboard hotkeys (`Space`, `Return`, `R`).
  - Restores `Time.timeScale = 1f` and reloads `MainGame.unity` cleanly.
- **Temporary IMGUI Pipeline**:
  - Zero-dependency, responsive layout ready to be converted to Figma UI canvas assets in the upcoming UI overhaul.

### 2.7 Level 2 Base Expansion & Defenses
- **Fortified Entrance Gate (`BaseGate.cs`)**:
  - Adds an interactive swinging wooden palisade door with scrap metal cross-bracing to the base archway.
  - Interactive player prompt `[🚪 OPEN GATE / 🔒 CLOSE GATE [G]]`.
  - Dynamically activates/deactivates physical `BoxCollider` and `NavMeshObstacle` carving.
  - Implements `IDamageable` (250 HP) so zombies must break it down if locked shut.
- **Automated Sentry Watchtower (`Watchtower.cs`, `BaseBuildingSection`)**:
  - Unlocked when Base upgrades to Level 2.
  - Fixed-plot build model (10 Wood, 5 Scrap) featuring 4 tall corner stilts (3.6m), elevated observation platform with safety railings, and a rotating sniper turret.
  - Automatically scans for zombies within $16\,\text{m}$ perimeter, aims turret smoothly, and fires sniper rounds ($35\,\text{HP}$, $1.3\,\text{s}$ interval) with visual tracers and damage popups.
- **Base Storage Stash (`ResourceStash.cs`)**:
  - Safe storage chest located inside the shelter near the workbench.
  - Interactive prompt `[📦 OPEN STASH [F]]` freezing gameplay time (`Time.timeScale = 0f`).
  - Supports quick `Deposit All` and `Withdraw All` transfers, preserving surplus resources safely across player deaths.
  - Integrated into `DeathScreenUI` to display safe banked reserves alongside carried inventory.
- **Base Progression Refinements (`BaseManager.cs`)**:
  - Evaluates Level 1 perimeter fortifications (`AllPerimeterWallsBuilt`) to unlock Level 2 upgrade prompt.
  - Automatically unhides the Watchtower construction plot and expands player exploration boundaries upon upgrading.

---

## 3. Project Directory Map

```
Assets/
├── Scenes/
│   └── MainGame.unity              # Primary playable prototype scene
├── _Game/
│   ├── Materials/                  # Stylized URP Lit Materials
│   │   ├── MAT_Wood.mat
│   │   ├── MAT_Scrap.mat
│   │   ├── MAT_Gunpowder.mat
│   │   ├── MAT_Player.mat
│   │   ├── MAT_Zombie.mat
│   │   └── MAT_Ground.mat
│   ├── Prefabs/
│   │   ├── Combat/
│   │   │   ├── Bullet.prefab
│   │   │   └── WoodBarricade.prefab
│   │   ├── Enemy/
│   │   │   └── Zombie.prefab
│   │   ├── Player/
│   │   │   └── Player.prefab
│   │   └── Resources/
│   │       ├── Node_Wood.prefab
│   │       ├── Node_Scrap.prefab
│   │       └── Node_Gunpowder.prefab
│   └── Scripts/
│       ├── Camera/
│       │   └── IsometricCamera.cs
│       ├── Combat/
│       │   ├── Barricade.cs
│       │   └── Projectile.cs
│       ├── Core/
│       │   ├── BaseBuildingSection.cs
│       │   ├── BaseGate.cs
│       │   ├── BaseManager.cs
│       │   ├── EventBus.cs
│       │   ├── GameState.cs
│       │   ├── ObjectPool.cs
│       │   ├── ResourceStash.cs
│       │   ├── RuntimeNavMeshBuilder.cs
│       │   ├── Watchtower.cs
│       │   └── WaveManager.cs
│       ├── Crafting/
│       │   ├── CraftingBench.cs
│       │   └── CraftingSystem.cs
│       ├── Data/
│       │   ├── Recipe_PistolAmmo.asset
│       │   ├── Recipe_RepairKit.asset
│       │   ├── Recipe_WoodBarricade.asset
│       │   ├── SO_CraftingRecipe.cs
│       │   ├── SO_Resource.cs
│       │   └── WeaponData.cs
│       ├── Enemy/
│       │   ├── DamagePopup.cs
│       │   ├── WorldHealthBar.cs
│       │   └── ZombieController.cs
│       ├── Player/
│       │   ├── PlayerController.cs
│       │   ├── PlayerHealth.cs
│       │   ├── PlayerInventory.cs
│       │   └── PlayerShooting.cs
│       ├── Resources/
│       │   ├── ResourceNode.cs
│       │   └── ResourceSpawner.cs
│       └── UI/
│           ├── DeathScreenUI.cs
│           ├── HUDManager.cs
│           └── MobileOverlayHUD.cs
```

---

## 4. Current Status & Next Session Agenda

### 4.1 Current Status (September 12, 2026)
- **Core Loop Complete**: Player movement, shooting, scavenging economy, base fortification, crafting bench, day/night zombie horde cycles, and player death/retry loop are fully connected and verified in-engine.
- **Level 2 Base Defenses**: Fortified interactive entrance gate (`BaseGate.cs`), automated sniper watchtower (`Watchtower.cs`), and safe base storage chest (`ResourceStash.cs`) active and validated in live play mode.
- **Combat & Rendering Quality**: Continuous damage incrementing fixed for stationary players; shadow artifacts and fog-of-war mesh scaling distortions eliminated. Clean 0 compiler errors.

### 4.2 Next Session Agenda (Tomorrow)
1. **Figma UI & Asset Overhaul**:
   - Transition temporary IMGUI overlays (HUD, Crafting Workbench, Safe Storage Stash, Section Prompts, and Death Screen) to custom Figma-designed Canvas UI components.
   - Import bespoke icons and UI assets for inventory resources (Wood, Scrap, Gunpowder, Barricades, Ammo, Health).
2. **Visual & 3D Asset Polish**:
   - Replace prototype block-out models with customized stylized survival models and materials.
3. **Audio & Sound Effects**:
   - Add AudioSources and trigger sound effects for gunfire, reloading, zombie growls/bites, sentry sniper shots, base construction, and night siren alarms.
