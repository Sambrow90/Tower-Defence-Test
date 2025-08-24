class Enemy:
    def __init__(self, x, y, hp, speed, waypoints):
        self.x = x
        self.y = y
        self.hp = float(hp)
        self.max_hp = float(hp)
        self.speed = float(speed)
        self.waypoints = waypoints
        self._wp_idx = 1  # 0 is spawn point, move to 1
        self.alive = True

    def take_damage(self, dmg):
        self.hp -= dmg
        if self.hp <= 0:
            self.alive = False

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
        self.x += dirx * self.speed * dt
        self.y += diry * self.speed * dt
        if ((tx - self.x)**2 + (ty - self.y)**2) < 4.0:
            self._wp_idx += 1
        return "ok"


class Tower:
    def __init__(self, x, y, grid, rng=120.0, dmg=10.0, firerate=1.0):
        self.x = x
        self.y = y
        self.grid = grid
        self.rng = float(rng)
        self.dmg = float(dmg)
        self.firerate = float(firerate)
        self._cooldown = 0.0

    def can_shoot(self):
        return self._cooldown <= 0.0

    def update_cooldown(self, dt):
        if self._cooldown > 0.0:
            self._cooldown -= dt

    def shoot(self):
        self._cooldown = 1.0 / self.firerate
