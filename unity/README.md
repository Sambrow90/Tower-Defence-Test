# Unity Tower Defense Projektstruktur

## Empfohlene Ordner im Project Browser
```
Assets/
  Audio/
    Music/
    SFX/
  Prefabs/
    Towers/
    Enemies/
    UI/
  Scenes/
  Scripts/
    Gameplay/
      Towers/
      Enemies/
    Managers/
    Systems/
  ScriptableObjects/
    Data/
  UI/
```

### Weitere Vorschläge
- **Art/** (optional) für 2D/3D Assets
- **VFX/** für Partikel- und Shader-Ressourcen
- **Settings/** für `InputAction`-, `AudioMixer`- oder andere Projekt-Settings

## Kernskripte
Die wichtigsten Manager-Klassen befinden sich unter `Assets/Scripts/Managers/`, System-Klassen unter `Assets/Scripts/Systems/` und Gameplay-spezifische Logik unter `Assets/Scripts/Gameplay/`.

Alle Klassen sind als Skeleton implementiert und enthalten TODO-Kommentare für die spätere Ausarbeitung.

## Szenen-Setup
1. **BootScene** (`Assets/Scenes/BootScene.unity`)
   - Enthält einen persistenten `GameManager`-Prefab mit folgenden Komponenten als Kinder oder referenzierte Objekte:
     - `GameManager` (MonoBehaviour, hängt am Root).
     - `LevelManager`, `WaveManager`, `TowerManager`, `EnemyManager`, `UIManager`, `SaveManager` (jeweils Komponenten auf eigenen GameObjects oder als Unterobjekte des GameManagers).
   - Setze `GameManager` auf `DontDestroyOnLoad`, damit er zwischen Szenen erhalten bleibt.
   - Optional: Lade nach Initialisierung automatisch die Hauptmenü-Szene.

2. **MainMenu** (`Assets/Scenes/MainMenu.unity`)
   - UI-Canvas mit Menüpanel (`UIManager` referenziert diese Elemente).
   - Buttons zum Starten eines Spiels, Öffnen der Optionen usw.

3. **Level_X** (`Assets/Scenes/Level_01.unity`, etc.)
   - Enthält die Level-Geometrie, Wegpunkte/Navigation und Platzhalter für Tower-Plätze.
   - `EnemySpawnPoints` und `PathWaypoints` als separate GameObjects für die Gegnerbewegung.
   - `TowerPlacementRoot` (Empty GameObject) als Elternobjekt für platzierte Türme.
   - `EnemyRoot` (Empty GameObject) als Elternobjekt für gespawnte Gegner.

## Prefab-Setup
- **Managers/**: Erstelle Prefabs für den zentralen `GameManager` sowie die einzelnen Manager, um sie bequem in mehreren Szenen zu verwenden.
- **Towers/**: Jeder Turm erhält ein Prefab mit `TowerBehaviour`-abgeleiteter Komponente.
- **Enemies/**: Jeder Gegnertyp erhält ein Prefab mit `EnemyBehaviour`-abgeleiteter Komponente.
- **UI/**: HUD-, Menü- und Popup-Prefabs, die vom `UIManager` angesteuert werden.

## Script-Anbindung
- `GameManager`: Hängt am persistenten GameObject "GameManager" in der BootScene. Referenzen zu allen anderen Managern werden im Inspector gesetzt.
- `LevelManager`: Eigenes GameObject "LevelManager" oder Child des GameManagers. Hat Zugriff auf Level-Definitionen (ScriptableObjects in `Assets/ScriptableObjects/Data`).
- `WaveManager`: Child "WaveManager" mit Referenz auf `EnemyManager` und `LevelManager`.
- `TowerManager`: Child "TowerManager" mit Referenzen auf `TowerPlacementRoot` und Tower-Definitionen.
- `EnemyManager`: Child "EnemyManager" mit Referenz auf `EnemyRoot` und Enemyprefabs.
- `UIManager`: Liegt auf einem UI GameObject im Canvas, referenziert HUD- und Menü-Container.
- `SaveManager`: Child "SaveManager" (oder `ScriptableObject`-basierter Service). Wird vom `GameManager` verwendet.

## Weitere Empfehlungen
- Lagere Balancing-Daten (Tower-, Enemy-, Level-Definitionen) in ScriptableObjects im Ordner `Assets/ScriptableObjects/Data/` aus.
- Verwende `Addressables` oder `AssetBundles`, wenn du später LiveOps/Content-Updates planst.
- Für Mobile-spezifische Optimierungen: Ziel ist es, Manager-Logik vom Rendering zu trennen, damit UI/Gameplay unabhängig skaliert werden können (z. B. durch `Update` vs. `FixedUpdate`, Job-System, oder eigene Ticker).

## Nächste Schritte
1. Implementiere die TODOs in den Skeleton-Klassen.
2. Erstelle konkrete abgeleitete Klassen (`ProjectileTower`, `SlowEnemy`, etc.) in den entsprechenden Gameplay-Ordnern.
3. Füge ScriptableObjects zur Konfiguration hinzu.
4. Erweitere den `SaveManager` um Cloud-Sync oder mobile-spezifische Speicherroutinen.
