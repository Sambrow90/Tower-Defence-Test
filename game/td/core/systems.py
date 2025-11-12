from td.util.geometry import vec2_dist

def update_world(world, dt):
    if world.paused or world.lives <= 0:
        return

    # Spawning and waves
    world.update_spawning(dt)

    # Towers acquire targets & deal damage
    for t in world.towers:
        t.update_cooldown(dt)
        if not t.can_shoot():
            continue
        target = None
        best = 1e9
        for e in world.enemies:
            if not e.alive:
                continue
            d = vec2_dist((t.x, t.y), (e.x, e.y))
            if d <= t.rng and d < best:
                best = d
                target = e
        if target is not None:
            target.take_damage(t.dmg)
            if t.tower_type == "slow":
                target.apply_slow(0.5, 1.0)
            t.shoot()
            world.cb_shoot(t, target)

    # Update enemies & handle removal/events
    new_enemies = []
    for e in world.enemies:
        status = e.update(dt)
        if status == "dead":
            world.cb_death(e)
            world.give_gold(10)
            continue
        elif status == "end":
            world.enemy_reached_end(e)
            continue
        else:
            new_enemies.append(e)
    world.enemies = new_enemies
