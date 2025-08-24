import random

from td.core.path import build_default_path_pixels
from td.core.entities import Enemy, Tower

class World:
    def __init__(self, viewport=(0,0,960,720), shoot_cb=None, death_cb=None):
        self.viewport = viewport
        self.tile_size = 48

        # Game state
        self.enemies = []
        self.towers = []
        self.gold = 250
        self.lives = 20
        self.wave_number = 0
        self.time_to_next_wave = 1.0
        self.paused = False
        self.status_text = ""
        self.selected_tower = None

        # Tower definitions
        self.tower_types = {
            "cannon": {"cost": 50, "rng": 140, "dmg": 15, "firerate": 1.0},
            "slow": {"cost": 65, "rng": 120, "dmg": 5, "firerate": 0.8},
        }
        self.build_tower_type = "cannon"

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

    def cycle_tower_type(self):
        keys = list(self.tower_types.keys())
        idx = keys.index(self.build_tower_type)
        self.build_tower_type = keys[(idx + 1) % len(keys)]
        self.status_text = f"Tower: {self.build_tower_type}"

    def get_tower_at(self, grid_pos):
        for t in self.towers:
            if t.grid == grid_pos:
                return t
        return None

    def get_tower_stats(self, t_type, level):
        base = self.tower_types[t_type]
        dmg = base["dmg"] * (1 + 0.15 * (level - 1))
        rng = base["rng"] * (1 + 0.05 * (level - 1))
        firerate = base["firerate"] * (1 + 0.03 * (level - 1))
        return dict(rng=rng, dmg=dmg, firerate=firerate)

    def place_tower(self, grid_pos):
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
        if self.get_tower_at((gx, gy)):
            self.status_text = "Belegt."
            return False
        tdef = self.tower_types[self.build_tower_type]
        cost = tdef["cost"]
        if self.gold < cost:
            self.status_text = "Zu wenig Gold."
            return False
        x = left + gx * self.tile_size + self.tile_size/2
        y = bottom + gy * self.tile_size + self.tile_size/2
        stats = self.get_tower_stats(self.build_tower_type, 1)
        self.towers.append(Tower(x=x, y=y, grid=(gx, gy), tower_type=self.build_tower_type,
                                 level=1, **stats))
        self.gold -= cost
        self.status_text = ""
        return True

    def try_fuse(self, t1, t2):
        if not t1.mergeable(t2):
            self.status_text = "Fusion nicht möglich"
            return False
        gx, gy = t2.grid
        x = self.viewport[0] + gx * self.tile_size + self.tile_size/2
        y = self.viewport[1] + gy * self.tile_size + self.tile_size/2
        new_level = t1.level + 1
        stats = self.get_tower_stats(t1.tower_type, new_level)
        self.towers.remove(t1)
        self.towers.remove(t2)
        self.towers.append(Tower(x=x, y=y, grid=(gx, gy), tower_type=t1.tower_type,
                                 level=new_level, **stats))
        self.status_text = f"{t1.tower_type} L{new_level}"
        return True

    def start_next_wave(self):
        self.wave_number += 1
        base_hp = 50 + 15 * (self.wave_number-1)
        count = 8 + 2 * self.wave_number
        speed = 60 + 4 * self.wave_number
        self._enemies_to_spawn = count
        self._spawn_timer = 0.0
        self._spawn_interval = max(0.28, 0.62 - 0.03 * self.wave_number)
        self._wave_cooldown = 9.0  # disabled during active spawns
        self._pending_enemy_stats = (base_hp, speed)

    def spawn_enemy(self):
        if self._enemies_to_spawn <= 0:
            return
        start = self.path_pixels[0]
        hp, speed = self._pending_enemy_stats
        etype = "fast" if random.random() < 0.3 else "normal"
        if etype == "fast":
            hp *= 0.6
            speed *= 1.4
        e = Enemy(x=start[0], y=start[1], hp=hp, speed=speed,
                  waypoints=self.path_pixels, enemy_type=etype)
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
