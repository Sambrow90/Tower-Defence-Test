import os
import math

from kivy.uix.screenmanager import Screen
from kivy.uix.widget import Widget
from kivy.properties import NumericProperty, StringProperty, ObjectProperty
from kivy.clock import Clock
from kivy.graphics import Color, Rectangle, Ellipse
from kivy.core.image import Image as CoreImage
from kivy.core.audio import SoundLoader
from kivy.logger import Logger

from td.core.world import World
from td.core.systems import update_world
from td.util.resources import resource_path

class GameWidget(Widget):
    world = ObjectProperty(None)

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        # Load external artwork if available
        def load_tex(*parts, linear=True, wrap=True):
            path = resource_path(*parts)
            if not os.path.exists(path):
                Logger.warning("game: missing asset '%s'", path)
                return None
            try:
                img = CoreImage(path)
            except Exception as exc:
                Logger.warning("game: failed to load '%s': %s", path, exc)
                return None
            tex = img.texture
            if wrap:
                tex.wrap = 'repeat'
            if linear:
                tex.mag_filter = 'linear'
                tex.min_filter = 'linear'
            return tex

        self.bg_tex = load_tex("assets", "textures", "background.png")
        self.path_tex = load_tex("assets", "textures", "path.png")
        self.enemy_tex = load_tex("assets", "textures", "enemy.png", wrap=False)
        self.tower_tex = load_tex("assets", "textures", "tower.png", wrap=False)
        self.projectile_tex = load_tex("assets", "animations", "projectile.gif", wrap=False)
        self.explosion_tex = load_tex("assets", "animations", "explosion.gif", wrap=False)

        self.tower_colors = {"cannon": (1, 1, 1), "slow": (0.4, 0.6, 1)}
        self.enemy_colors = {"normal": (1, 1, 1), "fast": (1, 0.5, 0.5)}
        self.selected_tower = None
        self.shot_effects = []
        self.explosions = []

        self._music = SoundLoader.load(resource_path("assets", "music", "loop.mp3"))
        if self._music:
            self._music.loop = True
            self._music.volume = 0.2
            self._music.play()

        self.sfx_shoot = SoundLoader.load(resource_path("assets", "sfx", "shoot.wav"))
        self.sfx_death = SoundLoader.load(resource_path("assets", "sfx", "death.wav"))

    def on_size(self, *args):
        if self.world:
            self.world.viewport = (self.x, self.y, self.width, self.height)

    def on_pos(self, *args):
        if self.world:
            self.world.viewport = (self.x, self.y, self.width, self.height)

    def on_touch_down(self, touch):
        if not self.collide_point(*touch.pos):
            return False
        if touch.button == "right":
            self.world.cycle_tower_type()
            return True
        if touch.x > self.parent.width * 0.75:
            return False
        gx = int((touch.x - self.x) // self.world.tile_size)
        gy = int((touch.y - self.y) // self.world.tile_size)
        tower = self.world.get_tower_at((gx, gy))
        if tower:
            if self.selected_tower and self.selected_tower is not tower:
                self.world.try_fuse(self.selected_tower, tower)
                self.selected_tower = None
            else:
                self.selected_tower = tower
            return True
        placed = self.world.place_tower((gx, gy))
        if placed:
            self.selected_tower = None
        return placed

    def draw(self):
        self.canvas.clear()
        with self.canvas:
            # Background artwork
            if self.bg_tex:
                Color(1, 1, 1, 1)
                Rectangle(texture=self.bg_tex, pos=self.pos, size=self.size)
            else:
                Color(0.08, 0.1, 0.16, 1)
                Rectangle(pos=self.pos, size=self.size)

            # Path drawn as tiled pixel-art road
            left, bottom, _, _ = self.world.viewport
            for (gx, gy) in self.world.path_grid:
                px = left + gx * self.world.tile_size
                py = bottom + gy * self.world.tile_size
                if self.path_tex:
                    Rectangle(texture=self.path_tex, pos=(px, py),
                              size=(self.world.tile_size, self.world.tile_size))
                else:
                    Color(0.35, 0.26, 0.18, 1)
                    Rectangle(pos=(px, py), size=(self.world.tile_size, self.world.tile_size))
                    Color(1, 1, 1, 1)

            # Towers
            for t in self.world.towers:
                col = self.tower_colors.get(t.tower_type, (1, 1, 1))
                scale = 1.0 + 0.05 * (t.level - 1) + 0.08 * math.sin(t.anim * 3)
                base = self.world.tile_size * 0.95
                size = base * scale
                if self.tower_tex:
                    Color(1, 1, 1, 1)
                    Rectangle(texture=self.tower_tex, pos=(t.x - size/2, t.y - size/2), size=(size, size))
                else:
                    Color(*col, 1)
                    Rectangle(pos=(t.x - size/2, t.y - size/2), size=(size, size))
                if self.selected_tower is t:
                    Color(1, 1, 0, 0.6)
                    Rectangle(pos=(t.x - size/2 - 2, t.y - size/2 - 2), size=(size + 4, size + 4))
                Color(1, 1, 1, 1)

            # Enemies with small health bar
            for e in self.world.enemies:
                col = self.enemy_colors.get(e.enemy_type, (1, 1, 1))
                offset = 2 * math.sin(e.anim * 6)
                size = self.world.tile_size * 0.85
                if self.enemy_tex:
                    Color(1, 1, 1, 1)
                    Rectangle(texture=self.enemy_tex, pos=(e.x - size/2, e.y - size/2 + offset), size=(size, size))
                else:
                    Color(*col, 1)
                    Ellipse(pos=(e.x - size/2, e.y - size/2 + offset), size=(size, size))
                hp_frac = max(0.0, e.hp / e.max_hp)
                Color(0.2, 0.0, 0.0, 1)
                Rectangle(pos=(e.x - size/2, e.y + size/2 + 4 + offset), size=(size, 4))
                Color(0.8, 0.0, 0.0, 1)
                Rectangle(pos=(e.x - size/2, e.y + size/2 + 4 + offset), size=(size * hp_frac, 4))
                Color(1, 1, 1, 1)

            # Projectile trails
            for effect in self.shot_effects:
                alpha = max(0.0, 1.0 - effect["progress"])
                Color(1, 1, 1, alpha)
                pos = effect["pos"]
                size = effect["size"]
                if self.projectile_tex:
                    Rectangle(texture=self.projectile_tex,
                              pos=(pos[0] - size/2, pos[1] - size/2),
                              size=(size, size))
                else:
                    Ellipse(pos=(pos[0] - size/4, pos[1] - size/4), size=(size/2, size/2))

            # Explosions
            for blast in self.explosions:
                alpha = max(0.0, 1.0 - blast["progress"])
                Color(1, 1, 1, alpha)
                size = blast["size"] * (0.6 + 0.4 * blast["progress"])
                if self.explosion_tex:
                    Rectangle(texture=self.explosion_tex,
                              pos=(blast["pos"][0] - size/2, blast["pos"][1] - size/2),
                              size=(size, size))
                else:
                    Ellipse(pos=(blast["pos"][0] - size/2, blast["pos"][1] - size/2),
                            size=(size, size))

    def update_effects(self, dt):
        for effect in list(self.shot_effects):
            effect["time"] += dt
            effect["progress"] = effect["time"] / effect["duration"]
            if effect["progress"] >= 1.0:
                self.shot_effects.remove(effect)
                continue
            sx, sy = effect["start"]
            tx, ty = effect["end"]
            frac = effect["progress"]
            effect["pos"] = (sx + (tx - sx) * frac, sy + (ty - sy) * frac)

        for blast in list(self.explosions):
            blast["time"] += dt
            blast["progress"] = blast["time"] / blast["duration"]
            if blast["progress"] >= 1.0:
                self.explosions.remove(blast)

    def play_shoot(self, tower, enemy):
        if self.sfx_shoot:
            self.sfx_shoot.stop()
            self.sfx_shoot.play()
        start = (tower.x, tower.y)
        target_pos = (enemy.x, enemy.y)
        self.shot_effects.append({
            "start": start,
            "end": target_pos,
            "pos": start,
            "time": 0.0,
            "duration": 0.25,
            "progress": 0.0,
            "size": self.world.tile_size * 0.45,
        })

    def play_death(self, enemy):
        if self.sfx_death:
            self.sfx_death.stop()
            self.sfx_death.play()
        self.explosions.append({
            "pos": (enemy.x, enemy.y),
            "time": 0.0,
            "duration": 0.35,
            "progress": 0.0,
            "size": self.world.tile_size * 1.6,
        })


class GameScreen(Screen):
    gold = NumericProperty(100)
    lives = NumericProperty(20)
    wave = NumericProperty(0)
    status = StringProperty("")
    next_wave_in = NumericProperty(0.0)
    game_widget = ObjectProperty(None)

    def on_enter(self, *args):
        if not hasattr(self, "world") or self.world is None:
            self.world = World(viewport=(0, 0, self.width * 0.75, self.height), shoot_cb=self.on_tower_shoot, death_cb=self.on_enemy_death)
        self.ids.game.world = self.world
        self._clock = Clock.schedule_interval(self._update, 1/60.0)

    def on_pre_leave(self, *args):
        if hasattr(self, "_clock") and self._clock:
            self._clock.cancel()

    def _update(self, dt):
        update_world(self.world, dt)
        if not self.world.paused and self.world.lives > 0:
            self.ids.game.update_effects(dt)
        self.gold = self.world.gold
        self.lives = self.world.lives
        self.wave = self.world.wave_number
        self.status = self.world.status_text
        self.next_wave_in = max(0.0, self.world.time_to_next_wave)
        self.ids.game.draw()

    def on_tower_shoot(self, tower, enemy):
        self.ids.game.play_shoot(tower, enemy)

    def on_enemy_death(self, enemy):
        self.ids.game.play_death(enemy)

    def pause(self):
        self.world.paused = True
        self.status = "Pausiert"

    def resume(self):
        self.world.paused = False
        self.status = ""

    def restart(self):
        self.world.reset()
