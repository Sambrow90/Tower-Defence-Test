import os
import math

from kivy.uix.screenmanager import Screen
from kivy.uix.widget import Widget
from kivy.properties import NumericProperty, StringProperty, ObjectProperty
from kivy.clock import Clock
from kivy.graphics import Color, Rectangle, Ellipse, Line
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
            else:
                tex.mag_filter = 'nearest'
                tex.min_filter = 'nearest'
            return tex

        self.bg_tex = load_tex("assets", "textures", "background.png")
        mesh_tex = load_tex("assets", "textures", "path.png")
        if mesh_tex:
            region_size = min(mesh_tex.width, mesh_tex.height) // 3
            x = int((mesh_tex.width - region_size) / 2)
            y = int((mesh_tex.height - region_size) / 2)
            self.path_tex = mesh_tex.get_region(x, y, region_size, region_size)
            self.path_tex.wrap = 'repeat'
            self.path_tex.mag_filter = 'linear'
            self.path_tex.min_filter = 'linear'
        else:
            self.path_tex = None

        self.tower_textures = {
            "cannon": load_tex("assets", "textures", "tower_cannon.png", linear=False, wrap=False),
            "slow": load_tex("assets", "textures", "tower_slow.png", linear=False, wrap=False),
        }
        self.tower_elite_tex = load_tex("assets", "textures", "tower_elite.png", linear=False, wrap=False)

        enemy_sheet = load_tex("assets", "animations", "enemy_walk.png", linear=False, wrap=False)
        self.enemy_frames = None
        if enemy_sheet:
            self.enemy_frames = {}
            cols = 8
            rows = 4
            frame_w = enemy_sheet.width // cols
            frame_h = enemy_sheet.height // rows
            directions = ["south", "west", "east", "north"]
            for row, direction in enumerate(directions):
                y = enemy_sheet.height - (row + 1) * frame_h
                frames = []
                for col in range(cols):
                    region = enemy_sheet.get_region(col * frame_w, y, frame_w, frame_h)
                    region.mag_filter = 'nearest'
                    region.min_filter = 'nearest'
                    frames.append(region)
                self.enemy_frames[direction] = frames

        self.projectile_tex = load_tex("assets", "animations", "projectile.gif", wrap=False)
        self.explosion_tex = load_tex("assets", "animations", "explosion.gif", wrap=False)

        self.tower_colors = {"cannon": (1, 1, 1), "slow": (0.4, 0.6, 1)}
        self.enemy_colors = {"normal": (1, 1, 1), "fast": (1, 0.5, 0.5)}
        self.selected_tower = None
        self.shot_effects = []
        self.explosions = []
        self.enemy_states = {}

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
            tile = self.world.tile_size
            inner_margin = tile * 0.18
            edge_band = tile * 0.12
            path_tiles = list(self.world.path_grid)
            path_set = set(path_tiles)
            for (gx, gy) in path_tiles:
                px = left + gx * tile
                py = bottom + gy * tile

                Color(0.22, 0.18, 0.14, 1)
                Rectangle(pos=(px, py), size=(tile, tile))

                inner_pos = (px + inner_margin, py + inner_margin)
                inner_size = (tile - 2 * inner_margin, tile - 2 * inner_margin)
                if self.path_tex:
                    Color(1, 1, 1, 0.94)
                    Rectangle(texture=self.path_tex, pos=inner_pos, size=inner_size)
                else:
                    Color(0.7, 0.62, 0.54, 1)
                    Rectangle(pos=inner_pos, size=inner_size)

                Color(0.86, 0.8, 0.69, 0.7)
                Ellipse(pos=(px + tile * 0.22, py + tile * 0.16), size=(tile * 0.56, tile * 0.6))

                Color(0.14, 0.12, 0.09, 0.9)
                Line(rectangle=(inner_pos[0], inner_pos[1], inner_size[0], inner_size[1]),
                     width=max(1.2, tile * 0.05))

                Color(0.16, 0.13, 0.1, 1)
                if (gx, gy + 1) not in path_set:
                    Rectangle(pos=(px, py + tile - edge_band), size=(tile, edge_band))
                if (gx, gy - 1) not in path_set:
                    Rectangle(pos=(px, py), size=(tile, edge_band))
                if (gx - 1, gy) not in path_set:
                    Rectangle(pos=(px, py), size=(edge_band, tile))
                if (gx + 1, gy) not in path_set:
                    Rectangle(pos=(px + tile - edge_band, py), size=(edge_band, tile))

                Color(1, 1, 1, 1)

            # Towers
            for t in self.world.towers:
                col = self.tower_colors.get(t.tower_type, (1, 1, 1))
                base = self.world.tile_size * 0.9
                scale = 1.0 + 0.06 * (t.level - 1) + 0.06 * math.sin(t.anim * 3)
                height = base * scale
                texture = self.tower_textures.get(t.tower_type)
                Color(0, 0, 0, 0.22)
                shadow_size = (self.world.tile_size * 0.82, self.world.tile_size * 0.4)
                Ellipse(pos=(t.x - shadow_size[0]/2, t.y - shadow_size[1]/2 - 4), size=shadow_size)
                if texture:
                    aspect = texture.width / texture.height if texture.height else 1.0
                    width = height * aspect
                    Color(1, 1, 1, 1)
                    Rectangle(texture=texture, pos=(t.x - width/2, t.y - height/2), size=(width, height))
                else:
                    width = height
                    Color(*col, 1)
                    Rectangle(pos=(t.x - width/2, t.y - height/2), size=(width, height))

                if t.level >= 4 and self.tower_elite_tex:
                    elite = self.tower_elite_tex
                    elite_width = width * 1.05
                    elite_height = height * 1.05
                    Color(1, 1, 1, 0.65)
                    Rectangle(texture=elite, pos=(t.x - elite_width/2, t.y - elite_height/2),
                              size=(elite_width, elite_height))

                if t.level > 1:
                    Color(1.0, 0.84, 0.2, 0.28)
                    ring = self.world.tile_size * (0.5 + 0.12 * (t.level - 1))
                    Ellipse(pos=(t.x - ring, t.y - ring), size=(ring * 2, ring * 2))
                if self.selected_tower is t:
                    Color(0.55, 0.85, 1.0, 0.85)
                    Rectangle(pos=(t.x - width/2 - 3, t.y - height/2 - 3), size=(width + 6, height + 6))
                Color(1, 1, 1, 1)

            # Enemies with animation and health bar
            for e in self.world.enemies:
                col = self.enemy_colors.get(e.enemy_type, (1, 1, 1))
                state = self.enemy_states.get(id(e))
                frame = None
                if state and self.enemy_frames:
                    frames = self.enemy_frames.get(state["dir"], [])
                    if frames:
                        frame = frames[state["frame"] % len(frames)]
                Color(0, 0, 0, 0.25)
                shadow_size = (self.world.tile_size * 0.75, self.world.tile_size * 0.32)
                Ellipse(pos=(e.x - shadow_size[0]/2, e.y - shadow_size[1]/2 - 3), size=shadow_size)
                if frame:
                    sprite_size = self.world.tile_size * 1.2
                    Color(1, 1, 1, 1)
                    Rectangle(texture=frame, pos=(e.x - sprite_size/2, e.y - sprite_size/2), size=(sprite_size, sprite_size))
                else:
                    offset = 2 * math.sin(e.anim * 6)
                    size = self.world.tile_size * 0.9
                    Color(*col, 1)
                    Ellipse(pos=(e.x - size/2, e.y - size/2 + offset), size=(size, size))
                hp_frac = max(0.0, e.hp / e.max_hp)
                bar_width = self.world.tile_size * 0.9
                Color(0.12, 0.05, 0.02, 1)
                Rectangle(pos=(e.x - bar_width/2, e.y + self.world.tile_size * 0.55), size=(bar_width, 6))
                Color(0.85, 0.1, 0.18, 1)
                Rectangle(pos=(e.x - bar_width/2, e.y + self.world.tile_size * 0.55), size=(bar_width * hp_frac, 6))
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

        if self.enemy_frames:
            active_ids = set()
            for enemy in self.world.enemies:
                key = id(enemy)
                active_ids.add(key)
                state = self.enemy_states.get(key)
                if state is None:
                    state = {
                        "frame": 0,
                        "time": 0.0,
                        "dir": "south",
                        "pos": (enemy.x, enemy.y),
                    }
                    self.enemy_states[key] = state
                else:
                    prev_x, prev_y = state["pos"]
                    dx = enemy.x - prev_x
                    dy = enemy.y - prev_y
                    if abs(dx) + abs(dy) > 0.1:
                        if abs(dx) > abs(dy):
                            state["dir"] = "east" if dx > 0 else "west"
                        else:
                            state["dir"] = "north" if dy > 0 else "south"
                    state["pos"] = (enemy.x, enemy.y)
                    move_mag = (dx * dx + dy * dy) ** 0.5
                    state["time"] += dt + move_mag * 0.015
                    if state["time"] >= 0.12:
                        state["time"] -= 0.12
                        frames = self.enemy_frames.get(state["dir"], [])
                        if frames:
                            state["frame"] = (state["frame"] + 1) % len(frames)
            for key in list(self.enemy_states.keys()):
                if key not in active_ids:
                    self.enemy_states.pop(key, None)

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
