using System;
using System.Collections.Generic;
using RCG;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Path finder for the player: a 25 cm grid over the floor, blocked where the player's body would touch
    /// solid scenery (walls, columns, desks, crates), A* with diagonal steps. The enemies' NavMesh reaches further than
    /// the player may walk, so it is not used for the player.</summary>
    public static class PathGrid
    {
        private const float Cell = 0.25f;
        private static int _mask = -1;
        private static readonly Collider[] _hits = new Collider[16];

        private static int Mask(RCG.Player p)
        {
            if (_mask != -1) return _mask;
            int layer = LayerMask.NameToLayer("EntityCollision");
            if (layer < 0) layer = p.gameObject.layer;
            int m = 0;
            for (int i = 0; i < 32; i++) if (!Physics.GetIgnoreLayerCollision(layer, i)) m |= 1 << i;
            // characters share the layer EntityCollision with big objects (benches, garbage cans); characters are
            // filtered out in Blocked, the objects stay obstacles
            _mask = m;
            return m;
        }

        public static void OnSceneLoaded() { _mask = -1; _tris.Clear(); }

        private static float _inflate = 1f;

        private static bool _wantMap;
        public static string LastMap;

        /// <summary>Makes the next search write a text map of its grid (for the room protocol when guiding is blocked).</summary>
        public static void RequestMap() { _wantMap = true; LastMap = null; }

        /// <summary>Object the path leads to (never an obstacle for its own path).</summary>
        public static Transform Ignore;

        /// <summary>Height the player stands on while planning (cells are checked relative to it).</summary>
        private static float _base;
        private static readonly RaycastHit[] _rays = new RaycastHit[24];

        private static bool Counts(Collider h)
        {
            if (h == null) return false;
            if (h.GetComponentInParent<CombatEntity>() != null) return false;
            if (Ignore != null && h.transform.IsChildOf(Ignore)) return false;
            PickupObject po = h.GetComponentInParent<PickupObject>();
            if (po != null && !(po is Weapon_TwoHandedOverhead)) return false; // small pickups do not block
            return true;
        }

        private static bool BodyBlocked(Vector3 c, float y, int mask)
        {
            // the player's feet are about 70 cm wide and 10 cm deep; inflate by half a cell so walls between cells are found
            Vector3 half = new Vector3(0.35f * _inflate + Cell * 0.5f, 0.35f, 0.05f + Cell * 0.5f * _inflate);
            int n = Physics.OverlapBoxNonAlloc(new Vector3(c.x, y, c.z), half, _hits, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (Counts(_hits[i])) return true;
            return false;
        }

        private static readonly Collider[] _blk = new Collider[24];

        /// <summary>True if the player's body would not fit at this spot (walls, furniture).</summary>
        public static bool Blocked(Vector3 p)
        {
            float keep = _inflate;
            _inflate = 1f;
            bool b = BodyBlocked(p, p.y + 0.5f, _mask != -1 ? _mask : ~0);
            _inflate = keep;
            return b;
        }

        /// <summary>What stops the player walking in direction h/v: 0 nothing found (invisible border = wall),
        /// 1 wall or room boundary, 2 obstacle (furniture, crate, car ...), 3 an enemy or other character.</summary>
        public static int Blocker(Vector3 pos, int h, int v, out Collider what)
        {
            what = null;
            int mask = _mask != -1 ? _mask : ~0;
            Vector3 c = pos + new Vector3(h * 0.45f, 0.9f, v * 0.2f);
            Vector3 half = new Vector3(h != 0 ? 0.2f : 0.35f, 0.75f, v != 0 ? 0.12f : 0.06f);
            int n = Physics.OverlapBoxNonAlloc(c, half, _blk, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
            int result = 0;
            for (int i = 0; i < n; i++)
            {
                Collider col = _blk[i];
                if (col == null) continue;
                CombatEntity ce = col.GetComponentInParent<CombatEntity>();
                if (ce != null)
                {
                    if (ce is RCG.Player) continue;
                    if (result == 0) { result = 3; what = col; }
                    continue;
                }
                if (!Counts(col)) continue;
                int k = IsObstacle(col, pos.y) ? 2 : 1;
                if (result == 0 || result == 3 || (k == 2 && result == 1)) { result = k; what = col; }
            }
            return result;
        }

        private static bool IsObstacle(Collider col, float floorY)
        {
            Bounds b = col.bounds;
            string n = col.name.ToLowerInvariant() + "/" + (col.transform.parent != null ? col.transform.parent.name.ToLowerInvariant() : "");
            if (n.Contains("wall") || n.Contains("border") || n.Contains("bound") || n.Contains("invisible") || n.Contains("camera") || n.Contains("backcollision") || n.Contains("frontcollision")) return false;
            if (b.max.y < floorY + 2.6f) return true;           // low: furniture, counters, crates, cars
            return b.size.x < 6f && b.size.z < 3f;              // small and free standing: pillars, machines
        }

        /// <summary>Highest walkable surface at x/z that a jump from the given height can reach (furniture, steps);
        /// float.NaN if there is none.</summary>
        public static float SurfaceY(float x, float z, float from, int mask)
        {
            int n = Physics.RaycastNonAlloc(new Vector3(x, from + 2.2f, z), Vector3.down, _rays, 4.5f, mask, QueryTriggerInteraction.Ignore);
            float best = float.NaN;
            for (int i = 0; i < n; i++)
            {
                if (!Counts(_rays[i].collider)) continue;
                float y = _rays[i].point.y;
                if (y > from + 2.0f) continue;
                if (float.IsNaN(best) || y > best) best = y;
            }
            return best;
        }

        public static float SurfaceY(float x, float z, float from) { return SurfaceY(x, z, from, _mask != -1 ? _mask : ~0); }

        /// <summary>Floor checks only where the player's own spot has a floor collider (some rooms have none).</summary>
        private static bool _holeCheck;

        /// <summary>True if there is floor at x/z no more than 1.2 m below the height baseY (no abyss, no hole).</summary>
        public static bool HasFloor(Vector3 c, float baseY, int mask)
        {
            int n = Physics.RaycastNonAlloc(new Vector3(c.x, baseY + 0.4f, c.z), Vector3.down, _rays, 1.6f, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (Counts(_rays[i].collider)) return true;
            return false;
        }

        public static bool HasFloor(Vector3 p) { return HasFloor(p, p.y, _mask != -1 ? _mask : ~0); }

        /// <summary>Floor under the whole foot area (with a small margin to edges).</summary>
        private static bool FloorAround(Vector3 c, int mask)
        {
            float ox = 0.2f * _inflate + 0.05f, oz = 0.12f;
            return HasFloor(c, _base, mask) && HasFloor(c + new Vector3(ox, 0, 0), _base, mask) && HasFloor(c - new Vector3(ox, 0, 0), _base, mask)
                && HasFloor(c + new Vector3(0, 0, oz), _base, mask) && HasFloor(c - new Vector3(0, 0, oz), _base, mask);
        }

        /// <summary>0 free, 2 blocked, 4 a platform (furniture, step) the player has to jump onto, 5 no floor (abyss, hole).</summary>
        private static byte CellState(Vector3 c, int mask)
        {
            if (!BodyBlocked(c, _base + 0.5f, mask)) return _holeCheck && !FloorAround(c, mask) ? (byte)5 : (byte)0;
            // something at body height: maybe the top of a piece of furniture one can jump onto
            float top = SurfaceY(c.x, c.z, _base, mask);
            if (float.IsNaN(top) || top < _base + 0.3f) return 2;
            if (BodyBlocked(c, top + 0.55f, mask)) return 2; // no room on top (wall, shelf above)
            return 4;
        }

        /// <summary>True if squeaky floor lies within mx (sideways) and mz (depth) of p.</summary>
        public static bool ToyNear(Vector3 p, float mx, float mz)
        {
            List<Collider> toys = Navigator.Toys;
            for (int i = 0; i < toys.Count; i++)
            {
                Collider t = toys[i];
                if (t == null) continue;
                Bounds b = t.bounds;
                if (p.x < b.min.x - mx || p.x > b.max.x + mx || p.z < b.min.z - mz || p.z > b.max.z + mz) continue;
                List<Vector2> tri = Triangles(t);
                if (tri != null)
                {
                    // the floor patch as triangles seen from above; test the point and the edges of the foot area
                    for (int sx = -1; sx <= 1; sx++)
                        for (int sz = -1; sz <= 1; sz++)
                            if (InTriangles(tri, p.x + sx * mx, p.z + sz * mz)) return true;
                    continue;
                }
                Vector3 q = new Vector3(p.x, Mathf.Clamp(0.02f, b.min.y, b.max.y), p.z);
                Vector3 cp;
                try { cp = t.ClosestPoint(q); } catch { cp = b.ClosestPoint(q); }
                if (Mathf.Abs(cp.x - p.x) <= mx && Mathf.Abs(cp.z - p.z) <= mz) return true;
            }
            return false;
        }

        private static readonly Dictionary<int, List<Vector2>> _tris = new Dictionary<int, List<Vector2>>();

        /// <summary>Triangles (x/z, world space, 3 points each) of a mesh collider; null if not a readable mesh.</summary>
        private static List<Vector2> Triangles(Collider c)
        {
            MeshCollider mc = c as MeshCollider;
            if (mc == null || mc.sharedMesh == null) return null;
            int id = c.GetInstanceID();
            List<Vector2> list;
            if (_tris.TryGetValue(id, out list)) return list;
            list = null;
            try
            {
                Vector3[] v = mc.sharedMesh.vertices;
                int[] t = mc.sharedMesh.triangles;
                if (v != null && t != null && v.Length > 0 && t.Length >= 3)
                {
                    list = new List<Vector2>(t.Length);
                    Transform tr = mc.transform;
                    for (int i = 0; i + 2 < t.Length; i += 3)
                        for (int k = 0; k < 3; k++)
                        {
                            Vector3 w = tr.TransformPoint(v[t[i + k]]);
                            list.Add(new Vector2(w.x, w.z));
                        }
                    Log.Write("Quietschboden " + c.name + ": " + (t.Length / 3) + " Dreiecke, konvex " + mc.convex);
                }
            }
            catch (Exception e) { Log.Write("Quietschboden " + c.name + " nicht lesbar: " + e.Message); list = null; }
            _tris[id] = list;
            return list;
        }

        private static bool InTriangles(List<Vector2> tri, float x, float z)
        {
            for (int i = 0; i + 2 < tri.Count; i += 3)
            {
                Vector2 a = tri[i], b = tri[i + 1], c = tri[i + 2];
                if (Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) < 1e-4f) continue; // side faces seen from above
                float d1 = (x - b.x) * (a.y - b.y) - (a.x - b.x) * (z - b.y);
                float d2 = (x - c.x) * (b.y - c.y) - (b.x - c.x) * (z - c.y);
                float d3 = (x - a.x) * (c.y - a.y) - (c.x - a.x) * (z - a.y);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) return true;
            }
            return false;
        }

        private struct Node { public int X, Z; public Node(int x, int z) { X = x; Z = z; } }

        /// <summary>Waypoints from the player to the walkable cell nearest to the goal; null if no path.
        /// Standing tight against scenery can wall the player in on the grid: then retry with a slimmer body.</summary>
        public static List<Vector3> Find(RCG.Player p, Vector3 from, Vector3 to)
        {
            _inflate = 1f;
            List<Vector3> r = FindOnce(p, from, to);
            if (r == null || (r.Count == 1 && Mathf.Abs(r[0].x - from.x) < 0.01f && Mathf.Abs(r[0].z - from.z) < 0.01f))
            {
                _inflate = 0.4f;
                r = FindOnce(p, from, to);
                _inflate = 1f;
                if (r != null) Log.Verbose("Wegfinder: schmaler Koerper noetig bei " + from.ToString("F2"));
            }
            return r;
        }

        private static List<Vector3> FindOnce(RCG.Player p, Vector3 from, Vector3 to)
        {
            try
            {
                int mask = Mask(p);
                float minX = Mathf.Min(from.x, to.x) - 4f, maxX = Mathf.Max(from.x, to.x) + 4f;
                float minZ = Mathf.Min(from.z, to.z) - 5f, maxZ = Mathf.Max(from.z, to.z) + 2f;
                int w = Mathf.Clamp(Mathf.CeilToInt((maxX - minX) / Cell) + 1, 2, 240);
                int d = Mathf.Clamp(Mathf.CeilToInt((maxZ - minZ) / Cell) + 1, 2, 60);
                // 0 unknown, 1 free, 2 blocked, 3 squeaky floor (passable only by jumping over it: expensive),
                // 4 platform to jump onto (furniture, steps), 5 no floor (abyss)
                _base = from.y;
                _holeCheck = HasFloor(from, from.y, mask);
                byte[] state = new byte[w * d];
                Vector3 halfToy = new Vector3(0.35f * _inflate + 0.05f, 0, 0.05f + 0.05f);
                Func<int, int, bool> free = delegate (int x, int z)
                {
                    if (x < 0 || z < 0 || x >= w || z >= d) return false;
                    int i = z * w + x;
                    if (state[i] == 0)
                    {
                        Vector3 c = new Vector3(minX + x * Cell, 0, minZ + z * Cell);
                        byte cs = CellState(c, mask);
                        state[i] = cs == 2 ? (byte)2 : cs == 4 ? (byte)4 : cs == 5 ? (byte)5 : (_base < 0.4f && ToyNear(c, halfToy.x, halfToy.z)) ? (byte)3 : (byte)1;
                    }
                    return state[i] == 1 || state[i] == 3 || state[i] == 4;
                };
                int sx = Mathf.Clamp(Mathf.RoundToInt((from.x - minX) / Cell), 0, w - 1);
                int sz = Mathf.Clamp(Mathf.RoundToInt((from.z - minZ) / Cell), 0, d - 1);
                int gx = Mathf.Clamp(Mathf.RoundToInt((to.x - minX) / Cell), 0, w - 1);
                int gz = Mathf.Clamp(Mathf.RoundToInt((to.z - minZ) / Cell), 0, d - 1);
                state[sz * w + sx] = 1; // the player stands here

                float[] g = new float[w * d];
                int[] parent = new int[w * d];
                bool[] jumpTo = new bool[w * d];
                for (int i = 0; i < g.Length; i++) { g[i] = float.MaxValue; parent[i] = -1; }
                bool[] closed = new bool[w * d];
                Heap open = new Heap();
                int start = sz * w + sx;
                g[start] = 0;
                open.Push(Heu(sx, sz, gx, gz), start);
                int best = start;
                float bestH = Heu(sx, sz, gx, gz);
                int expanded = 0;
                while (open.Count > 0 && expanded < 8000)
                {
                    int cur = open.Pop();
                    if (closed[cur]) continue;
                    closed[cur] = true;
                    expanded++;
                    int cx = cur % w, cz = cur / w;
                    float h = Heu(cx, cz, gx, gz);
                    if (h < bestH) { bestH = h; best = cur; }
                    if (cx == gx && cz == gz) { best = cur; break; }
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx = cx + dx, nz = cz + dz;
                            if (!free(nx, nz))
                            {
                                // abyss right next to us: floor again a few cells further in the same direction = jump across
                                if (dx != 0 && nx >= 0 && nz >= 0 && nx < w && nz < d && state[nz * w + nx] == 5) TryGap(cx, cz, dx, dz, w, d, cur, gx, gz, free, state, g, parent, closed, jumpTo, open);
                                continue;
                            }
                            if (dx != 0 && dz != 0 && (!free(cx + dx, cz) || !free(cx, cz + dz))) continue;
                            int ni = nz * w + nx;
                            if (closed[ni]) continue;
                            float ng = g[cur] + ((dx != 0 && dz != 0) ? 1.414f : 1f) + (state[ni] == 3 ? 12f : state[ni] == 4 ? 6f : 0f);
                            if (ng < g[ni]) { g[ni] = ng; parent[ni] = cur; jumpTo[ni] = false; open.Push(ng + Heu(nx, nz, gx, gz), ni); }
                        }
                }
                // walk back, keep only the corners (where the step direction changes)
                List<int> chain = new List<int>();
                int k = best;
                while (k >= 0) { chain.Add(k); k = parent[k]; }
                chain.Reverse();
                List<Vector3> path = new List<Vector3>();
                LastJumps.Clear();
                for (int i = 1; i < chain.Count; i++)
                {
                    // take-off and landing of a jump across an abyss always stay waypoints
                    bool keep = jumpTo[chain[i]] || (i < chain.Count - 1 && jumpTo[chain[i + 1]]);
                    if (i < chain.Count - 1 && !keep)
                    {
                        int ax = chain[i] % w - chain[i - 1] % w, az = chain[i] / w - chain[i - 1] / w;
                        int bx = chain[i + 1] % w - chain[i] % w, bz = chain[i + 1] / w - chain[i] / w;
                        if (ax == bx && az == bz) continue;
                    }
                    int c = chain[i];
                    if (jumpTo[c]) LastJumps.Add(path.Count);
                    path.Add(new Vector3(minX + (c % w) * Cell, from.y, minZ + (c / w) * Cell));
                }
                if (LastJumps.Count > 0) Log.Write("Wegfinder: Sprung ueber Abgrund geplant, " + LastJumps.Count + "x, erster bei " + path[LastJumps[0]].ToString("F2"));
                if (path.Count == 0) path.Add(new Vector3(from.x, from.y, from.z));
                if (_wantMap)
                {
                    _wantMap = false;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    HashSet<int> onPath = new HashSet<int>(chain);
                    sb.Append("Rasterkarte x ").Append(minX.ToString("F2")).Append(" bis ").Append((minX + (w - 1) * Cell).ToString("F2"))
                      .Append(", z ").Append(minZ.ToString("F2")).Append(" (unten) bis ").Append((minZ + (d - 1) * Cell).ToString("F2")).Append(" (oben); # Wand, ~ Quietschboden, ^ Moebel/Stufe (Sprung), O Abgrund, * Weg, S Start, Z Ziel, ? nicht geprueft\r\n");
                    for (int z = d - 1; z >= 0; z--)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int i = z * w + x;
                            char ch = state[i] == 2 ? '#' : state[i] == 3 ? '~' : state[i] == 4 ? '^' : state[i] == 5 ? 'O' : state[i] == 1 ? '.' : '?';
                            if (onPath.Contains(i)) ch = '*';
                            if (i == start) ch = 'S';
                            if (x == gx && z == gz) ch = 'Z';
                            sb.Append(ch);
                        }
                        sb.Append("\r\n");
                    }
                    LastMap = sb.ToString();
                }
                return path;
            }
            catch (Exception e) { Log.Error("PathGrid", e); return null; }
        }

        /// <summary>Path indices (of the last Find) that are reached by jumping across an abyss.</summary>
        public static readonly List<int> LastJumps = new List<int>();

        /// <summary>Longest jump across an abyss the path finder plans (meters, sideways; in depth at most 1 m).</summary>
        private const float MaxGap = 4.5f;

        private static void TryGap(int cx, int cz, int dx, int dz, int w, int d, int cur, int gx, int gz, Func<int, int, bool> free,
            byte[] state, float[] g, int[] parent, bool[] closed, bool[] jumpTo, Heap open)
        {
            int maxK = Mathf.FloorToInt(MaxGap / Cell);
            for (int k = 2; k <= maxK; k++)
            {
                int jx = cx + dx * k, jz = cz + dz * k;
                if (jx < 0 || jz < 0 || jx >= w || jz >= d) return;
                if (Mathf.Abs(dz * k) * Cell > 1.0f) return;
                bool f = free(jx, jz);
                int ji = jz * w + jx;
                if (state[ji] == 2) return;              // a wall in the way
                if (!f) continue;                         // still abyss
                if (!free(jx + dx, jz + dz)) return;      // landing too narrow
                if (closed[ji]) return;
                float ng = g[cur] + k * (dz != 0 ? 1.414f : 1f) + 10f;
                if (ng < g[ji]) { g[ji] = ng; parent[ji] = cur; jumpTo[ji] = true; open.Push(ng + Heu(jx, jz, gx, gz), ji); }
                return;
            }
        }

        /// <summary>Minimal binary heap (priority, index).</summary>
        private class Heap
        {
            private readonly List<float> _p = new List<float>();
            private readonly List<int> _v = new List<int>();
            public int Count { get { return _v.Count; } }
            public void Push(float p, int v)
            {
                _p.Add(p); _v.Add(v);
                int i = _v.Count - 1;
                while (i > 0)
                {
                    int up = (i - 1) / 2;
                    if (_p[up] <= _p[i]) break;
                    Swap(i, up); i = up;
                }
            }
            public int Pop()
            {
                int r = _v[0];
                int last = _v.Count - 1;
                _p[0] = _p[last]; _v[0] = _v[last];
                _p.RemoveAt(last); _v.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, rr = l + 1, m = i;
                    if (l < _v.Count && _p[l] < _p[m]) m = l;
                    if (rr < _v.Count && _p[rr] < _p[m]) m = rr;
                    if (m == i) break;
                    Swap(i, m); i = m;
                }
                return r;
            }
            private void Swap(int a, int b)
            {
                float tp = _p[a]; _p[a] = _p[b]; _p[b] = tp;
                int tv = _v[a]; _v[a] = _v[b]; _v[b] = tv;
            }
        }

        private static float Heu(int x, int z, int gx, int gz)
        {
            int dx = Mathf.Abs(x - gx), dz = Mathf.Abs(z - gz);
            return Mathf.Max(dx, dz) + 0.414f * Mathf.Min(dx, dz);
        }

        /// <summary>"0,5 Meter nach unten, dann links 4 Meter" for the first legs of a path.</summary>
        public static string Describe(Vector3 from, List<Vector3> path)
        {
            if (path == null || path.Count == 0) return "";
            List<string> legs = new List<string>();
            Vector3 cur = from;
            foreach (Vector3 w in path)
            {
                string s = GameInfo.Direction(cur, w);
                if (s != Loc.T("dir_here")) legs.Add(s);
                cur = w;
                if (legs.Count >= 3) break;
            }
            return string.Join(", " + Loc.T("then") + " ", legs.ToArray());
        }
    }
}
