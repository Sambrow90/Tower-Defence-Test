from kivy.uix.screenmanager import Screen
from kivy.uix.widget import Widget
from kivy.properties import NumericProperty, StringProperty, ListProperty, ObjectProperty
from kivy.clock import Clock
from kivy.graphics import Color, Rectangle, Ellipse
from kivy.core.image import Image as CoreImage
from kivy.core.audio import SoundLoader

from td.core.world import World
from td.core.systems import update_world
from td.util.resources import resource_path
from td.util.geometry import vec2_dist

class GameWidget(Widget):
    world = ObjectProperty(None)

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.bg_tex = CoreImage(resource_path("td", "assets", "textures", "background.png")).texture
        self.enemy_tex = CoreImage(resource_path("td", "assets", "textures", "enemy.png")).texture
        self.tower_tex = CoreImage(resource_path("td", "assets", "textures", "tower.png")).texture
        self._music = SoundLoader.load(resource_path("td", "assets", "music", "loop.wav"))
        if self._music:
            self._music.loop = True
            self._music.volume = 0.2
            self._music.play()

        self.sfx_shoot = SoundLoader.load(resource_path("td", "assets", "sfx", "shoot.wav"))
        self.sfx_death = SoundLoader.load(resource_path("td", "assets", "sfx", "death.wav"))

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
            # Background
            Rectangle(texture=self.bg_tex, pos=self.pos, size=self.size)
            # Path points
            Color(0.2, 0.8, 0.2, 0.35)
            for (px, py) in self.world.path_pixels:
                Ellipse(pos=(px - 6, py - 6), size=(12, 12))
            # Towers
            for t in self.world.towers:
                Rectangle(texture=self.tower_tex, pos=(t.x - 16, t.y - 16), size=(32, 32))
            # Enemies
            for e in self.world.enemies:
                Rectangle(texture=self.enemy_tex, pos=(e.x - 14, e.y - 14), size=(28, 28))

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
