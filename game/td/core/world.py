from td.core.path import build_default_path_pixels
from td.core.entities import Enemy, Tower

class World:
    def __init__(self, viewport=(0,0,960,720), shoot_cb=None, death_cb=None):
        self.viewport = viewport
        self.tile_size = 48

        # Game state
        self.enemies = []
        self.towers = []
        self.gold = 150
        self.lives = 20
        self.wave_number = 0
        self.time_to_next_wave = 3.0
        self.paused = False
        self.status_text = ""

        # Path and blocked tiles
        self.path_pixels, self.path_grid = build_default_path_pixels(self.tile_size, viewport)
        self.blocked = set(self.path_grid)  # can't place towers on path

        # Callbacks for SFX
        self.cb_shoot = shoot_cb if shoot_cb else (lambda: None)
        self.cb_death = death_cb if death_cb else (lambda: None)

        # Spawn handling
        self._spawn_timer = 0.0
        self._spawn_interval = 0.6
        self._enemies_to_spawn = 0
        self._wave_cooldown = 2.0

    def reset(self):
        self.__init__(viewport=self.viewport, shoot_cb=self.cb_shoot, death_cb=self.cb_death)

    def place_tower(self, grid_pos, cost=50, rng=140, dmg=15, firerate=1.0):
        gx, gy = grid_pos
        left, bottom, w, h = self.viewport
        max_gx = int((w*0.75) // self.tile_size) - 1
        max_gy = int(h // self.tile_size) - 1
        if gx < 0 or gy < 0 or gx > max_gx or gy > max_gy:
            self.status_text = "Ungültige Position."
            return False
        if (gx, gy) in self.blocked:
            self.status_text = "Pfad/Blockiert."
            return False
        for t in self.towers:
            if t.grid == (gx, gy):
                self.status_text = "Belegt."
                return False
        if self.gold < cost:
            self.status_text = "Zu wenig Gold."
            return False
        x = left + gx * self.tile_size + self.tile_size/2
        y = bottom + gy * self.tile_size + self.tile_size/2
        self.towers.append(Tower(x=x, y=y, grid=(gx, gy), rng=rng, dmg=dmg, firerate=firerate))
        self.gold -= cost
        self.status_text = ""
        return True

    def start_next_wave(self):
        self.wave_number += 1
        base_hp = 50 + 15 * (self.wave_number-1)
        count = 8 + 2 * self.wave_number
        speed = 60 + 4 * self.wave_number
        self._enemies_to_spawn = count
        self._spawn_timer = 0.0
        self._spawn_interval = max(0.28, 0.62 - 0.03 * self.wave_number)
        self._wave_cooldown = 9999.0  # disabled during active spawns
        self._pending_enemy_stats = (base_hp, speed)

    def spawn_enemy(self):
        if self._enemies_to_spawn <= 0:
            return
        start = self.path_pixels[0]
        hp, speed = self._pending_enemy_stats
        e = Enemy(x=start[0], y=start[1], hp=hp, speed=speed, waypoints=self.path_pixels)
        self.enemies.append(e)
        self._enemies_to_spawn -= 1

    def update_spawning(self, dt):
        if self._enemies_to_spawn > 0:
            self._spawn_timer += dt
            if self._spawn_timer >= self._spawn_interval:
                self._spawn_timer -= self._spawn_interval
                self.spawn_enemy()
        else:
            if not self.enemies:
                self._wave_cooldown -= dt
                self.time_to_next_wave = max(0.0, self._wave_cooldown)
                if self._wave_cooldown <= 0.0:
                    self.time_to_next_wave = 0.0
                    self._wave_cooldown = 2.0
                    self.start_next_wave()

    def enemy_reached_end(self, enemy):
        self.lives -= 1
        if self.lives <= 0:
            self.status_text = "Game Over."

    def give_gold(self, amount):
        self.gold += amount
