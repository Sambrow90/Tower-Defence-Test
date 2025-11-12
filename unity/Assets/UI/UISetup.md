# UI Setup Guide

This document describes the recommended scene hierarchy, prefabs, and references required to wire the new UI controllers into the Tower Defense project. All objects live under a single screen-space overlay canvas so the UI scales correctly on mobile resolutions.

## Canvas Root

Create an empty GameObject named **UIRoot** and add:

- `Canvas` (Render Mode: *Screen Space - Overlay*)
- `Canvas Scaler` (UI Scale Mode: *Scale With Screen Size*, Reference Resolution: `1920x1080`, Match: `0.5`)
- `Graphic Raycaster`
- Attach the `UIManager` component

Children of `UIRoot`:

1. **HUDRoot** (`RectTransform`, anchor stretch, full screen)
   - Attach the `HudView` component.
   - Contains gameplay HUD elements (see below).
2. **MenuRoot** (`RectTransform`, anchor stretch, full screen)
   - Contains the main menu, level select, and settings panels as separate child objects.

Assign `UIManager.MainCanvas`, `HudRoot`, `MenuRoot`, and the screen GameObjects in the inspector.

## HUD Layout (`HudView`)

Under **HUDRoot** create the following hierarchy:

- **HUDPanel** (optional `Image` background for readability)
  - **TopBar** (Horizontal Layout Group)
    - **LevelNameLabel** (`TMP_Text`, assign to `HudView.levelNameLabel`)
    - **WaveLabel** (`TMP_Text`, assign to `HudView.waveLabel`)
  - **StatsBar** (Horizontal Layout Group)
    - **LivesLabel** (`TMP_Text`, assign to `HudView.livesLabel`)
    - **CurrencyLabel** (`TMP_Text`, assign to `HudView.currencyLabel`)
  - **SpeedControls**
    - **PauseButton** (`Button` + child `TMP_Text`, hook to `HudView.pauseButton` / `pauseButtonLabel`)
    - **Speed1xButton** (`Button`, label “1x”, hook to `HudView.speedNormalButton`)
    - **Speed2xButton** (`Button`, label “2x”, hook to `HudView.speedFastButton`)
  - **TowerSelectionPanel** (see Tower Selection below)
  - **WaveMessage** (`TMP_Text`, centered overlay, assign to `HudView.waveMessageLabel`)
  - **PauseOverlay** (full-screen `Image` with CanvasGroup, assign to `HudView.pauseOverlay` and hide by default)
  - **ResultPanels**
    - **VictoryPanel** (assign to `HudView.victoryPanel`, disable by default)
    - **DefeatPanel** (assign to `HudView.defeatPanel`, disable by default)

Add the `TowerSelectionView` component to **TowerSelectionPanel**.

## Tower Selection (`TowerSelectionView`)

Inside **TowerSelectionPanel** place one `Button` prefab per tower definition. For each button:

- Set the `towerId` string to match `TowerDefinition.TowerId` in the `TowerManager`.
- Assign the button to `towerButtons[i].button`.
- Optional: child `TMP_Text` for the name (`towerButtons[i].label`).
- Optional: second `TMP_Text` for the cost (`towerButtons[i].costLabel`).

Add a dedicated **CancelButton** and wire it to `TowerSelectionView.cancelButton`. The panel also needs a reference to the active `TowerPlacementController` (drag the scene instance into `placementController`).

## Main Menu (`MainMenuView`)

Under **MenuRoot** create a child GameObject **MainMenuScreen** and attach `MainMenuView`. Add buttons for:

- **PlayButton** (`Button`, assign to `MainMenuView.playButton`)
- **LevelSelectButton** (`Button`, assign to `MainMenuView.levelSelectButton`)
- **SettingsButton** (`Button`, assign to `MainMenuView.settingsButton`)
- **QuitButton** (optional, assign to `MainMenuView.quitButton`)

Point `UIManager.mainMenuScreen` to **MainMenuScreen** and assign the view reference.

## Level Select (`LevelSelectView`)

Create **LevelSelectScreen** (child of **MenuRoot**), attach `LevelSelectView`, and set:

- `contentRoot`: a `VerticalLayoutGroup` inside a `ScrollRect` for listing levels.
- `levelButtonPrefab`: prefab with `LevelButtonWidget` (see below).
- `backButton`: button to return to the main menu.
- Optional `TMP_Text` header label.

### Level Button Prefab (`LevelButtonWidget`)

Create a prefab under `Assets/Prefabs/UI/` containing:

- Root `Button`
  - Child `TMP_Text` for the title (assign to `titleLabel`).
  - Child `TMP_Text` for the subtitle/lock state (assign to `subtitleLabel`).

Drag this prefab into the `levelButtonPrefab` slot on the `LevelSelectView`.

## Settings Screen (`SettingsView`)

Create **SettingsScreen** (child of **MenuRoot**) with sliders and dropdowns:

- `masterVolumeSlider`: Slider (0–1).
- `musicVolumeSlider`: Slider (0–1) to control the stored music volume preference.
- `qualityDropdown`: `TMP_Dropdown` listing quality tiers.
- `backButton`: returns to main menu.

Attach `SettingsView` and assign the references. `UIManager.settingsScreen` should point to this GameObject.

## Manager Wiring

Ensure the persistent **GameManager** prefab has references to:

- `LevelManager`
- `WaveManager`
- `TowerManager`
- `EnemyManager`
- `UIManager` (the component on **UIRoot**)
- `SaveManager`

`TowerManager` must reference the `TowerDefinition` list and the scene’s `GridManager`. Assign the same `TowerManager` to `TowerSelectionView`.

The `TowerPlacementController` should live in the gameplay scene, using the same `TowerManager` and `GridManager`. Assign it to `TowerSelectionView.placementController`.

## Scenes & Prefabs

- **BootScene**: contains persistent managers and optionally the UI canvas so it survives scene loads.
- **MainMenu.unity**: loads the `UIRoot` canvas and `GameManager` prefab.
- **Level_X.unity** scenes: include gameplay grid, `TowerPlacementController`, and reference the shared `UIRoot` prefab (or rely on the persistent instance from BootScene).

Recommended prefabs:

- `Assets/Prefabs/UI/UIRoot.prefab` – contains the entire canvas hierarchy.
- `Assets/Prefabs/UI/LevelButtonWidget.prefab` – reused by the level select list.
- Optional `Assets/Prefabs/UI/TowerButton.prefab` to standardize tower selection entries.

With this setup the scripts automatically subscribe to `LevelManager`, `WaveManager`, and `GameManager` events, keeping the HUD and menus updated without additional glue code.
