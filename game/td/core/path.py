import random
from collections import deque


def _bfs(adjacency, start):
    queue = deque([start])
    parent = {start: None}
    dist = {start: 0}
    while queue:
        cell = queue.popleft()
        for nb in adjacency[cell]:
            if nb not in parent:
                parent[nb] = cell
                dist[nb] = dist[cell] + 1
                queue.append(nb)
    return parent, dist


def _farthest_node(adjacency, start):
    parent, dist = _bfs(adjacency, start)
    farthest = max(dist, key=dist.get)
    return farthest, dist[farthest], parent


def _reconstruct_path(parent, end):
    path = []
    cur = end
    while cur is not None:
        path.append(cur)
        cur = parent[cur]
    path.reverse()
    return path


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

    stack = [(start, None)]
    visited = {start}

    while stack:
        cell, prev_dir = stack[-1]
        options = [n for n in _neighbors(cell, cols, rows) if n not in visited]
        if options:
            rng.shuffle(options)
            straight = []
            if prev_dir:
                straight = [n for n in options if (n[0] - cell[0], n[1] - cell[1]) == prev_dir]
            if straight and rng.random() < 0.65:
                nxt = straight[0]
            else:
                nxt = options[0]
            direction = (nxt[0] - cell[0], nxt[1] - cell[1])
            adjacency[cell].append(nxt)
            adjacency[nxt].append(cell)
            visited.add(nxt)
            stack.append((nxt, direction))
        else:
            stack.pop()

    return adjacency, start, goal


def _longest_path_from_left_to_right(adjacency, cols, rows):
    left_candidates = [(0, r) for r in range(rows)]
    best_path = []
    for start in left_candidates:
        parent, dist = _bfs(adjacency, start)
        best_end = None
        best_dist = -1
        for cell, d in dist.items():
            if cell[0] == cols - 1 and d > best_dist:
                best_dist = d
                best_end = cell
        if best_end is None:
            continue
        candidate = _reconstruct_path(parent, best_end)
        if len(candidate) > len(best_path):
            best_path = candidate
    return best_path


def _maze_diameter(adjacency):
    seed = next(iter(adjacency))
    farthest, _, _ = _farthest_node(adjacency, seed)
    other, _, parent = _farthest_node(adjacency, farthest)
    return _reconstruct_path(parent, other)


def build_default_path_pixels(tile, viewport):
    """Generate a fresh maze-style path for every run.

    Returns a list of pixel waypoints and the grid coordinates that make up the
    valid path. Enemies will follow the shortest route through the maze,
    ensuring their path always lines up with the rendered maze corridor.
    """

    left, bottom, w, h = viewport
    usable_w = int(w * 0.75)
    cols = max(14, max(1, usable_w // tile))
    rows = max(16, max(1, int(h // tile)))

    rng = random.Random()
    total_cells = cols * rows
    min_length = max(int(total_cells * 0.66), cols + rows)
    min_x_cover = 0.9 if cols >= 18 else 0.75
    min_y_cover = 0.6

    best_lr_path = []
    best_lr_score = -1
    best_any_path = []
    best_any_score = -1
    path_cells = None
    fallback_path = None
    for _ in range(160):
        adjacency, _, _ = _generate_maze(cols, rows, rng)
        candidate = _longest_path_from_left_to_right(adjacency, cols, rows)
        if not candidate:
            candidate = _maze_diameter(adjacency)

        unique_cells = len(set(candidate))
        if unique_cells < len(candidate):
            continue

        fallback_path = candidate

        coverage_x = len({x for x, _ in candidate}) / max(1, cols)
        coverage_y = len({y for _, y in candidate}) / max(1, rows)
        score = unique_cells + coverage_x * cols + coverage_y * rows

        if (
            unique_cells >= min_length
            and coverage_x >= min_x_cover
            and coverage_y >= min_y_cover
            and candidate[0][0] == 0
            and candidate[-1][0] == cols - 1
        ):
            path_cells = candidate
            break

        if candidate and candidate[0][0] == 0 and candidate[-1][0] == cols - 1:
            if score > best_lr_score:
                best_lr_score = score
                best_lr_path = candidate

        if score > best_any_score:
            best_any_score = score
            best_any_path = candidate

    if path_cells is None:
        if best_lr_path:
            path_cells = best_lr_path
        elif best_any_path:
            path_cells = best_any_path
        else:
            path_cells = fallback_path if fallback_path else []

    path_pixels = []
    for gx, gy in path_cells:
        x = left + gx * tile + tile / 2
        y = bottom + gy * tile + tile / 2
        path_pixels.append((x, y))

    return path_pixels, path_cells
