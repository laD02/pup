"""map_roadcheck.py img...: is the red-dot line ONE road? PASS = one chain, 2 ends, no fork (small gaps at bridges allowed)."""
import sys, math
import numpy as np
from PIL import Image

def dots(path):
    a = np.asarray(Image.open(path).convert('RGB').resize((1672, 941))).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    m = (r > 190) & (g < 80) & (b < 80) & (abs(g - b) < 40)
    ys, xs = np.nonzero(m)
    todo = set(zip(xs.tolist(), ys.tolist()))
    cand = []
    while todo:
        st = [todo.pop()]; px = []
        while st:
            x, y = st.pop(); px.append((x, y))
            for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if q in todo: todo.remove(q); st.append(q)
        X = [q[0] for q in px]; Y = [q[1] for q in px]
        w = max(X) - min(X) + 1; h = max(Y) - min(Y) + 1; ar = len(px)
        if 12 <= ar <= 400 and 0.6 < w / h < 1.67 and ar > 0.55 * w * h:   # small, round, solid
            cand.append((sum(X) / ar, sum(Y) / ar, ar))
    med = np.median([c[2] for c in cand])
    return [(x, y) for x, y, ar in cand if 0.45 * med < ar < 2.2 * med]

def check(path):
    pts = dots(path)
    for _ in range(3):   # drop loners (red props that pass the shape test)
        nn = [min(math.dist(p, q) for q in pts if q is not p) for p in pts]
        step = float(np.median(nn))
        pts = [p for p, d in zip(pts, nn) if d < 2.2 * step]
    n = len(pts); R = 1.9 * step
    adj = [[j for j in range(n) if j != i and math.dist(pts[i], pts[j]) < R] for i in range(n)]
    def arms(i):
        angs = sorted(math.atan2(pts[j][1] - pts[i][1], pts[j][0] - pts[i][0]) for j in adj[i])
        if not angs: return 0
        k = 1 + sum(1 for a, b in zip(angs, angs[1:]) if b - a > math.radians(75))
        if len(angs) > 1 and angs[0] + 2 * math.pi - angs[-1] <= math.radians(75): k -= 1
        return max(k, 1)
    A = [arms(i) for i in range(n)]
    comp = [-1] * n; c = 0
    for i in range(n):
        if comp[i] < 0:
            st = [i]; comp[i] = c
            while st:
                u = st.pop()
                for v in adj[u]:
                    if comp[v] < 0: comp[v] = c; st.append(v)
            c += 1
    # drop tiny components (stray marks)
    size = [comp.count(k) for k in range(c)]
    keep = [k for k in range(c) if size[k] >= 4]
    ends = [i for i in range(n) if A[i] <= 1 and comp[i] in keep]
    forks = [i for i in range(n) if A[i] >= 3 and comp[i] in keep]
    # join end pairs of different components across short gaps (bridges)
    parent = {k: k for k in keep}
    def find(k):
        while parent[k] != k: k = parent[k]
        return k
    free = set(ends)
    pairs = sorted((math.dist(pts[i], pts[j]), i, j) for i in ends for j in ends if i < j and comp[i] != comp[j])
    for d, i, j in pairs:
        if d > 190: break   # a bridge painted without dots
        if i in free and j in free and find(comp[i]) != find(comp[j]):
            parent[find(comp[i])] = find(comp[j]); free -= {i, j}
    chains = len({find(k) for k in keep})
    ok = chains == 1 and len(free) == 2 and not forks
    f = lambda L: [(round(pts[i][0]), round(pts[i][1])) for i in L]
    return ok, sum(size[k] for k in keep), chains, f(sorted(free)), f(forks)

failed = 0
for path in sys.argv[1:]:
    ok, n, chains, ends, forks = check(path)
    failed += not ok
    print(('PASS' if ok else 'FAIL'), path.split('/')[-1], 'dots', n, 'chains', chains, 'ends', ends, 'forks', forks[:5])
sys.exit(1 if failed else 0)   # usable in a shell loop
