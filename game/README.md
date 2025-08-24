# Modular Tower Defense (Kivy)

Ein einfacher, modularer 2D-Tower-Defense-Prototyp mit Kivy 2.3+ (Python 3.11/3.12).
Struktur ist so ausgelegt, dass du später Grafiken, Hintergründe, Musik/SFX und neue Spielmechaniken leicht ergänzen kannst.

## Projektstruktur

```
modular_td_kivy/
├─ main.py
├─ requirements.txt
├─ td/
│  ├─ app.py
│  ├─ core/
│  │  ├─ world.py
│  │  ├─ entities.py
│  │  ├─ systems.py
│  │  └─ path.py
│  ├─ screens/
│  │  ├─ menu.py
│  │  └─ game.py
│  ├─ ui/
│  │  ├─ menu.kv
│  │  └─ game.kv
│  ├─ util/
│  │  ├─ resources.py
│  │  └─ geometry.py
│  └─ assets/
│     ├─ textures/
│     │  ├─ background.png
│     │  ├─ enemy.png
│     │  └─ tower.png
│     ├─ sfx/
│     │  ├─ shoot.wav
│     │  └─ death.wav
│     └─ music/
│        └─ loop.wav
```

## Installation

1. Erzeuge am besten ein virtuelles Environment (optional, empfohlen).
2. Installiere Abhängigkeiten:

```bash
pip install -r requirements.txt
```

> Hinweis: Unter Windows kann Kivy zusätzliche Runtime-Dependencies (angle/glew/sdl2) benötigen. Wenn du bereits Kivy 2.3 nutzt, passt das.

## Starten

```bash
python main.py
```

## Steuerung (Basics)

- **Linksklick** auf freie Fliese (nicht auf dem Pfad), um einen Tower zu platzieren (Kosten: 50 Gold).
- Tower schießen automatisch auf Gegner in Reichweite.
- Wellen starten automatisch. Tötest du Gegner, erhältst du Gold.
- Rechts im HUD siehst du Gold, Leben und aktuelle Welle. Menü mit Pause/Resume.

## Erweiterungsideen

- Mehrere Tower-Typen (Splash, Slow, DoT, Sniper)
- Projektilgrafiken und Trefferanimationen
- Eigene Hintergründe/Tilemaps
- Mehr Musik/SFX, Lautstärkeregelung
- Save/Load, Highscores, Upgrades
- Balancing/Tuning
