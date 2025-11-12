import random
from collections import deque


def _neighbors(cell, cols, rows):
    x, y = cell
    if x > 0:
        yield (x - 1, y)
    if x < cols - 1:
        yield (x + 1, y)
    if y > 0:
        yield (x, y - 1)
    if y < rows - 1:
        yield (x, y + 1)


def _generate_maze(cols, rows, rng):
    adjacency = {(x, y): [] for x in range(cols) for y in range(rows)}
    start_row = rng.randrange(rows)
    far_rows = [r for r in range(rows) if abs(r - start_row) >= max(2, rows // 3)]
    if far_rows:
        goal_row = rng.choice(far_rows)
    else:
        goal_row = (start_row + rows // 2) % rows
    start = (0, start_row)
    goal = (cols - 1, goal_row)

    stack = [start]
    visited = {start}

    while stack:
        cell = stack[-1]
        unvisited = [n for n in _neighbors(cell, cols, rows) if n not in visited]
        if unvisited:
            nxt = rng.choice(unvisited)
            adjacency[cell].append(nxt)
            adjacency[nxt].append(cell)
            visited.add(nxt)
            stack.append(nxt)
        else:
            stack.pop()

    return adjacency, start, goal


def _shortest_path(adjacency, start, goal):
    queue = deque([start])
    came_from = {start: None}
    while queue:
        cell = queue.popleft()
        if cell == goal:
            break
        for nb in adjacency[cell]:
            if nb not in came_from:
                came_from[nb] = cell
                queue.append(nb)

    if goal not in came_from:
        return [start]

    path = []
    cur = goal
    while cur is not None:
        path.append(cur)
        cur = came_from[cur]
    path.reverse()
    return path


def build_default_path_pixels(tile, viewport):
    """Generate a fresh maze-style path for every run.

    Returns a list of pixel waypoints and the grid coordinates that make up the
    valid path. Enemies will follow the shortest route through the maze,
    ensuring their path always lines up with the rendered maze corridor.
    """

    left, bottom, w, h = viewport
    usable_w = int(w * 0.75)
    cols = max(12, max(1, usable_w // tile))
    rows = max(14, max(1, int(h // tile)))

    rng = random.Random()
    adjacency, start, goal = _generate_maze(cols, rows, rng)
    path_cells = _shortest_path(adjacency, start, goal)

    path_pixels = []
    for gx, gy in path_cells:
        x = left + gx * tile + tile / 2
        y = bottom + gy * tile + tile / 2
        path_pixels.append((x, y))

    return path_pixels, path_cells
