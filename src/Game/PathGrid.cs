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
            // other characters are not obstacles for planning
            int ec = LayerMask.NameToLayer("EntityCollision");
            if (ec >= 0) m &= ~(1 << ec);
            _mask = m;
            return m;
        }

        public static void OnSceneLoaded() { _mask = -1; }

        private static float _inflate = 1f;

        private static bool Blocked(Vector3 c, int mask)
        {
            // the player's feet are about 70 cm wide and 10 cm deep; inflate by half a cell so walls between cells are found
            Vector3 half = new Vector3(0.35f * _inflate + Cell * 0.5f, 0.35f, 0.05f + Cell * 0.5f * _inflate);
            int n = Physics.OverlapBoxNonAlloc(new Vector3(c.x, 0.5f, c.z), half, _hits, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider h = _hits[i];
                if (h == null) continue;
                if (h.GetComponentInParent<CombatEntity>() != null) continue;
                if (h.GetComponentInParent<PickupObject>() != null) continue;
                return true;
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
                // 0 unknown, 1 free, 2 blocked
                byte[] state = new byte[w * d];
                Func<int, int, bool> free = delegate (int x, int z)
                {
                    if (x < 0 || z < 0 || x >= w || z >= d) return false;
                    int i = z * w + x;
                    if (state[i] == 0) state[i] = Blocked(new Vector3(minX + x * Cell, 0, minZ + z * Cell), mask) ? (byte)2 : (byte)1;
                    return state[i] == 1;
                };
                int sx = Mathf.Clamp(Mathf.RoundToInt((from.x - minX) / Cell), 0, w - 1);
                int sz = Mathf.Clamp(Mathf.RoundToInt((from.z - minZ) / Cell), 0, d - 1);
                int gx = Mathf.Clamp(Mathf.RoundToInt((to.x - minX) / Cell), 0, w - 1);
                int gz = Mathf.Clamp(Mathf.RoundToInt((to.z - minZ) / Cell), 0, d - 1);
                state[sz * w + sx] = 1; // the player stands here

                float[] g = new float[w * d];
                int[] parent = new int[w * d];
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
                            if (!free(nx, nz)) continue;
                            if (dx != 0 && dz != 0 && (!free(cx + dx, cz) || !free(cx, cz + dz))) continue;
                            int ni = nz * w + nx;
                            if (closed[ni]) continue;
                            float ng = g[cur] + ((dx != 0 && dz != 0) ? 1.414f : 1f);
                            if (ng < g[ni]) { g[ni] = ng; parent[ni] = cur; open.Push(ng + Heu(nx, nz, gx, gz), ni); }
                        }
                }
                // walk back, keep only the corners (where the step direction changes)
                List<int> chain = new List<int>();
                int k = best;
                while (k >= 0) { chain.Add(k); k = parent[k]; }
                chain.Reverse();
                List<Vector3> path = new List<Vector3>();
                for (int i = 1; i < chain.Count; i++)
                {
                    if (i < chain.Count - 1)
                    {
                        int ax = chain[i] % w - chain[i - 1] % w, az = chain[i] / w - chain[i - 1] / w;
                        int bx = chain[i + 1] % w - chain[i] % w, bz = chain[i + 1] / w - chain[i] / w;
                        if (ax == bx && az == bz) continue;
                    }
                    int c = chain[i];
                    path.Add(new Vector3(minX + (c % w) * Cell, from.y, minZ + (c / w) * Cell));
                }
                if (path.Count == 0) path.Add(new Vector3(from.x, from.y, from.z));
                return path;
            }
            catch (Exception e) { Log.Error("PathGrid", e); return null; }
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
