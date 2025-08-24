from kivy.uix.screenmanager import Screen
from kivy.uix.widget import Widget
from kivy.properties import NumericProperty, StringProperty, ObjectProperty
from kivy.clock import Clock
from kivy.graphics import Color, Rectangle
from kivy.graphics.texture import Texture
from kivy.core.audio import SoundLoader

from td.core.world import World
from td.core.systems import update_world
from td.util.resources import resource_path

class GameWidget(Widget):
    world = ObjectProperty(None)

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        # Build small pixel-art textures directly in code to avoid external images
        palette = {
            '0': (0, 0, 0, 0),
            'D': (34, 34, 42, 255),
            'd': (44, 44, 52, 255),
            'P': (120, 70, 20, 255),
            'p': (160, 110, 60, 255),
            'S': (190, 170, 140, 255),
            '3': (60, 60, 70, 255),
            '5': (150, 150, 160, 255),
            '6': (110, 110, 120, 255),
            '7': (80, 80, 90, 255),
            '2': (70, 170, 255, 255),
            '9': (90, 0, 0, 255),
            'b': (200, 40, 40, 255),
            'B': (240, 80, 80, 255),
            '1': (255, 255, 255, 255),
        }

        def pixel_texture(pattern, repeat=False):
            h = len(pattern)
            w = len(pattern[0])
            buf = bytearray()
            for row in pattern:
                for ch in row:
                    buf.extend(palette.get(ch, (0, 0, 0, 0)))
            tex = Texture.create(size=(w, h))
            tex.blit_buffer(bytes(buf), colorfmt='rgba', bufferfmt='ubyte')
            tex.mag_filter = 'nearest'
            tex.min_filter = 'nearest'
            if repeat:
                tex.wrap = 'repeat'
            return tex

        bg_pattern = [
            "DdDdDdDd",
            "dDdDdDdD",
            "DdDdDdDd",
            "dDdDdDdD",
            "DdDdDdDd",
            "dDdDdDdD",
            "DdDdDdDd",
            "dDdDdDdD",
        ]

        path_pattern = [
            "PPPPPPPPPPPPPPPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPpppppSSpppppPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPpppppSSpppppPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPpppppSSpppppPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPppppppppppppPP",
            "PPPPPPPPPPPPPPPP",
        ]

        tower_pattern = [
            "0000003333000000",
            "0000335555330000",
            "0003555555530000",
            "0035556665553000",
            "0035566666553000",
            "0355667776655300",
            "0355677227655300",
            "0355672227655300",
            "0355677227655300",
            "0355667776655300",
            "0035566666553000",
            "0035556665553000",
            "0003555555530000",
            "0000335555330000",
            "0000003333000000",
            "0000000000000000",
        ]

        enemy_pattern = [
            "0000009999000000",
            "000099bbbb990000",
            "0009bbbbbbbb9000",
            "009bbbbbbbbbb900",
            "009bbbBbbbBbb900",
            "09bbbb1111bbbb90",
            "09bbb1BB11bbb900",
            "09bbb1BB11bbb900",
            "09bbbb1111bbbb90",
            "09bbb1BB11bbb900",
            "009bbbBbbbBbb900",
            "009bbbbbbbbbb900",
            "0009bbbbbbbb9000",
            "000099bbbb990000",
            "0000009999000000",
            "0000000000000000",
        ]

        self.bg_tex = pixel_texture(bg_pattern, repeat=True)
        self.path_tex = pixel_texture(path_pattern)
        self.enemy_tex = pixel_texture(enemy_pattern)
        self.tower_tex = pixel_texture(tower_pattern)
        self._music = SoundLoader.load(resource_path("assets", "music", "loop.wav"))
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
        # HUD ist rechts (25%), hier nichts platzieren
        if touch.x > self.parent.width * 0.75:
            return False
        # Convert to grid
        gx = int((touch.x - self.x) // self.world.tile_size)
        gy = int((touch.y - self.y) // self.world.tile_size)
        return self.world.place_tower((gx, gy))

    def draw(self):
        self.canvas.clear()
        with self.canvas:
            # Background with repeating texture
            Color(1, 1, 1, 1)
            rx = self.width / self.bg_tex.width
            ry = self.height / self.bg_tex.height
            Rectangle(texture=self.bg_tex, pos=self.pos, size=self.size,
                      tex_coords=(0, 0, rx, 0, rx, ry, 0, ry))

            # Path drawn as tiled pixel-art road
            left, bottom, _, _ = self.world.viewport
            for (gx, gy) in self.world.path_grid:
                px = left + gx * self.world.tile_size
                py = bottom + gy * self.world.tile_size
                Rectangle(texture=self.path_tex, pos=(px, py),
                          size=(self.world.tile_size, self.world.tile_size))

            # Towers
            for t in self.world.towers:
                Rectangle(texture=self.tower_tex, pos=(t.x - 16, t.y - 16), size=(32, 32))

            # Enemies with small health bar
            for e in self.world.enemies:
                Rectangle(texture=self.enemy_tex, pos=(e.x - 16, e.y - 16), size=(32, 32))
                hp_frac = max(0.0, e.hp / e.max_hp)
                Color(0.2, 0.0, 0.0, 1)
                Rectangle(pos=(e.x - 16, e.y + 18), size=(32, 4))
                Color(0.8, 0.0, 0.0, 1)
                Rectangle(pos=(e.x - 16, e.y + 18), size=(32 * hp_frac, 4))
                Color(1, 1, 1, 1)

    def play_shoot(self):
        if self.sfx_shoot:
            self.sfx_shoot.stop()
            self.sfx_shoot.play()

    def play_death(self):
        if self.sfx_death:
            self.sfx_death.stop()
            self.sfx_death.play()


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
        self.gold = self.world.gold
        self.lives = self.world.lives
        self.wave = self.world.wave_number
        self.status = self.world.status_text
        self.next_wave_in = max(0.0, self.world.time_to_next_wave)
        self.ids.game.draw()

    def on_tower_shoot(self):
        self.ids.game.play_shoot()

    def on_enemy_death(self):
        self.ids.game.play_death()

    def pause(self):
        self.world.paused = True
        self.status = "Pausiert"

    def resume(self):
        self.world.paused = False
        self.status = ""

    def restart(self):
        self.world.reset()
