def build_default_path_pixels(tile, viewport):
    # Build a light S-curve path across the left 75% of the screen.
    # Returns (path_pixels, path_grid).
    left, bottom, w, h = viewport
    usable_w = int(w * 0.75)
    cols = max(10, usable_w // tile)
    rows = max(8, int(h // tile))

    # Define grid waypoints (col, row)
    waypoints_grid = [
        (0, rows//2),
        (cols//4, rows//2),
        (cols//4, rows//4),
        (cols//2, rows//4),
        (cols//2, 3*rows//4),
        (3*cols//4, 3*rows//4),
        (3*cols//4, rows//3),
        (cols-1, rows//3),
    ]

    path_pixels = []
    path_grid = []

    for (gx, gy) in waypoints_grid:
        x = left + gx * tile + tile/2
        y = bottom + gy * tile + tile/2
        path_pixels.append((x, y))
        path_grid.append((gx, gy))

    # Interpolate grid cells between waypoints (4-connected)
    dense_grid = set()
    for i in range(len(waypoints_grid)-1):
        x0, y0 = waypoints_grid[i]
        x1, y1 = waypoints_grid[i+1]
        steps = max(abs(x1 - x0), abs(y1 - y0))
        for s in range(steps+1):
            gx = x0 + (x1 - x0) * s // max(1, steps)
            gy = y0 + (y1 - y0) * s // max(1, steps)
            dense_grid.add((int(gx), int(gy)))

    return path_pixels, list(dense_grid)
