class Enemy:
    def __init__(self, x, y, hp, speed, waypoints, enemy_type="normal"):
        self.x = x
        self.y = y
        self.hp = float(hp)
        self.max_hp = float(hp)
        self.speed = float(speed)
        self.waypoints = waypoints
        self._wp_idx = 1  # 0 is spawn point, move to 1
        self.alive = True
        self.enemy_type = enemy_type
        self.anim = 0.0
        self.slow_factor = 1.0
        self.slow_timer = 0.0

    def take_damage(self, dmg):
        self.hp -= dmg
        if self.hp <= 0:
            self.alive = False

    def apply_slow(self, factor, duration):
        if factor < self.slow_factor:
            self.slow_factor = factor
        if duration > self.slow_timer:
            self.slow_timer = duration

    def update(self, dt):
        if not self.alive:
            return "dead"
        if self._wp_idx >= len(self.waypoints):
            return "end"
        tx, ty = self.waypoints[self._wp_idx]
        dx = tx - self.x
        dy = ty - self.y
        dist = (dx*dx + dy*dy) ** 0.5
        if dist < 1e-3:
            self._wp_idx += 1
            return "ok"
        dirx = dx / dist
        diry = dy / dist
        speed = self.speed * self.slow_factor
        self.x += dirx * speed * dt
        self.y += diry * speed * dt
        if self.slow_timer > 0.0:
            self.slow_timer -= dt
            if self.slow_timer <= 0.0:
                self.slow_factor = 1.0
        self.anim += dt
        if ((tx - self.x)**2 + (ty - self.y)**2) < 4.0:
            self._wp_idx += 1
        return "ok"


class Tower:
    def __init__(self, x, y, grid, tower_type="cannon", level=1,
                 rng=120.0, dmg=10.0, firerate=1.0):
        self.x = x
        self.y = y
        self.grid = grid
        self.tower_type = tower_type
        self.level = level
        self.rng = float(rng)
        self.dmg = float(dmg)
        self.firerate = float(firerate)
        self._cooldown = 0.0
        self.anim = 0.0

    def can_shoot(self):
        return self._cooldown <= 0.0

    def update_cooldown(self, dt):
        if self._cooldown > 0.0:
            self._cooldown -= dt
        self.anim += dt

    def shoot(self):
        self._cooldown = 1.0 / self.firerate

    def mergeable(self, other):
        return (
            self.tower_type == other.tower_type
            and self.level == other.level
            and self.level < 20
        )
