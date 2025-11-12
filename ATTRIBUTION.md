# Asset Attribution

Binary assets are intentionally excluded from version control.  They are downloaded on demand by
running `PYTHONPATH=game python -m td.tools.assets` (automatically executed on application start-up)
and originate from the following freely licensed sources:

- [Proper Pixel Art](https://github.com/KennethJAllen/proper-pixel-art) by Kenneth J. Allen (MIT):
  - `assets/mountain/mountain.png` → `assets/textures/background.png`
  - `assets/mountain/mesh.png` → `assets/textures/path.png`
  - `assets/anchor/anchor.png` → `assets/textures/tower_cannon.png`
  - `assets/ash/ash.png` → `assets/textures/tower_slow.png`
  - `assets/pumpkin/pumpkin.png` → `assets/textures/tower_elite.png`
- [Universal LPC Spritesheet](https://github.com/jrconway3/Universal-LPC-spritesheet) (CC-BY-SA 3.0/GPLv3 dual licence):
  - `_build/_base/female/all.png` → `assets/animations/enemy_walk.png`
- [pygame example data](https://github.com/pygame/pygame/tree/main/examples/data) (LGPL):
  - `shot.gif` → `assets/animations/projectile.gif`
  - `explosion1.gif` → `assets/animations/explosion.gif`
  - `blue.gif` → `assets/ui/button_normal.gif` and `assets/ui/button_down.gif`
  - `background.gif` → `assets/ui/panel.gif`
  - `punch.wav` → `assets/sfx/shoot.wav`
  - `boom.wav` → `assets/sfx/death.wav`
  - `whiff.wav` → `assets/sfx/ui_click.wav`
  - `house_lo.mp3` → `assets/music/loop.mp3`
- [MedievalSharp](https://fonts.google.com/specimen/MedievalSharp) by Astigmatic (SIL Open Font
  License): `assets/fonts/MedievalSharp.ttf`

All files retain their original licences; see the linked projects for the exact terms.
