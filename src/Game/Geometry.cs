using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RCG;
using UnityEngine;
using UnityEngine.AI;

namespace RCGAccess
{
    /// <summary>Door interaction areas and room geometry protocols (for finding where exits really are).</summary>
    public static class Geometry
    {
        private static readonly Dictionary<int, List<Collider>> _doorCols = new Dictionary<int, List<Collider>>();
        private static readonly HashSet<string> _written = new HashSet<string>();

        public static void OnSceneLoaded() { _doorCols.Clear(); _inside.Clear(); _insideLogged.Clear(); }

        private class InsideCache { public float At; public Vector3 From; public bool Found; public Vector3 Point; }
        private static readonly Dictionary<int, InsideCache> _inside = new Dictionary<int, InsideCache>();
        private static readonly HashSet<int> _insideLogged = new HashSet<int>();
        private static readonly Collider[] _ov = new Collider[32];

        /// <summary>Large walk-in door triggers (a mesh the player has to walk into, e.g. DoorToFight): their bounds say little
        /// about their shape. Samples the floor inside the bounds at body height and returns the reachable point inside
        /// the trigger nearest to the player.</summary>
        /// <summary>Reachable point inside a walk-in trigger (quest areas, big door triggers).</summary>
        public static bool InsidePoint(Collider c, Vector3 from, out Vector3 point) { return InsideTrigger(c, from, out point); }

        private static bool InsideTrigger(Collider c, Vector3 from, out Vector3 point)
        {
            point = Vector3.zero;
            int id = c.GetInstanceID();
            float now = Time.realtimeSinceStartup;
            InsideCache ic;
            if (_inside.TryGetValue(id, out ic) && now - ic.At < 2f && (ic.From - from).sqrMagnitude < 1f)
            {
                point = ic.Point;
                return ic.Found;
            }
            ic = new InsideCache { At = now, From = from };
            _inside[id] = ic;
            Bounds b = c.bounds;
            int mask = 1 << c.gameObject.layer;
            float bestD = float.MaxValue;
            int tested = 0, floorPts = 0;
            float x0 = Mathf.Max(b.min.x, from.x - 20f), x1 = Mathf.Min(b.max.x, from.x + 20f);
            for (float x = x0 + 0.25f; x <= x1; x += 0.5f)
                for (float z = b.min.z + 0.15f; z <= b.max.z; z += 0.3f)
                {
                    Vector3 f = new Vector3(x, from.y, z);
                    if (!PathGrid.HasFloor(f) || PathGrid.Blocked(f)) continue;
                    floorPts++;
                    bool inside = false;
                    for (int k = 0; k < 3 && !inside; k++)
                    {
                        tested++;
                        int n = Physics.OverlapSphereNonAlloc(new Vector3(x, from.y + 0.3f + k * 0.7f, z), 0.25f, _ov, mask, QueryTriggerInteraction.Collide);
                        for (int i = 0; i < n; i++) if (_ov[i] == c) { inside = true; break; }
                    }
                    if (!inside) continue;
                    float d = (x - from.x) * (x - from.x) + (z - from.z) * (z - from.z);
                    if (d < bestD) { bestD = d; point = f; }
                }
            ic.Found = bestD < float.MaxValue;
            ic.Point = point;
            if (_insideLogged.Add(id))
                Log.Write("Tuer-Trigger " + GameInfo.Path(c.transform) + (c is MeshCollider ? (((MeshCollider)c).convex ? " (konvex)" : " (nicht konvex)") : "")
                    + ": Bodenpunkte " + floorPts + ", Pruefungen " + tested + (ic.Found ? ", begehbarer Punkt " + point.ToString("F2") : ", kein begehbarer Punkt in Koerperhoehe"));
            return ic.Found;
        }

        /// <summary>Colliders that belong to this interactable (the player has to overlap one of them).</summary>
        public static List<Collider> Colliders(InteractEntity e)
        {
            int id = e.GetInstanceID();
            List<Collider> list;
            if (_doorCols.TryGetValue(id, out list)) return list;
            list = new List<Collider>();
            foreach (Collider c in e.GetComponentsInChildren<Collider>(true))
            {
                InteractEntity owner = c.GetComponentInParent<InteractEntity>();
                if (owner != null && owner != e) continue;
                list.Add(c);
            }
            _doorCols[id] = list;
            return list;
        }

        /// <summary>Point of the interaction area closest to the player (x/z), or false if the door has no usable collider.</summary>
        public static bool ClosestPoint(InteractEntity e, Vector3 from, out Vector3 point)
        {
            point = Vector3.zero;
            float best = float.MaxValue;
            bool found = false;
            // the interaction area is a trigger on the layer "Interactables" (TriggerVolume); the "PlayerSpawn" triggers
            // (layer DetectEntity) only mark where players appear and must never be the goal
            int inter = LayerMask.NameToLayer("Interactables");
            int level = 0; // 2 = Interactables trigger, 1 = other trigger (no spawn), 0 = anything
            foreach (Collider c in Colliders(e))
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (c.isTrigger && c.gameObject.layer == inter) { level = 2; break; }
                if (c.isTrigger && c.name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) < 0) level = 1;
            }
            foreach (Collider c in Colliders(e))
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (level == 2 && !(c.isTrigger && c.gameObject.layer == inter)) continue;
                if (level == 1 && !(c.isTrigger && c.name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) < 0)) continue;
                Bounds b = c.bounds;
                // big walk-in trigger: find a reachable point that is really inside its shape
                if (level == 1 && c is MeshCollider && (b.size.x > 4f || b.size.z > 4f))
                {
                    Vector3 ip;
                    if (InsideTrigger(c, from, out ip))
                    {
                        float di = (ip.x - from.x) * (ip.x - from.x) + (ip.z - from.z) * (ip.z - from.z);
                        if (di < best) { best = di; point = ip; found = true; }
                        continue;
                    }
                }
                // stand well inside the area: centre for narrow areas, otherwise half a metre from its sides
                float mx = b.size.x < 2f ? b.size.x * 0.5f : 0.5f;
                float qx = Mathf.Clamp(from.x, b.min.x + mx, b.max.x - mx);
                float mz = b.size.z < 0.6f ? b.size.z * 0.5f : 0.3f; // a little inside the area, not on its edge
                Vector3 q = new Vector3(qx, from.y, Mathf.Clamp(from.z, b.min.z + mz, b.max.z - mz));
                float d = (q.x - from.x) * (q.x - from.x) + (q.z - from.z) * (q.z - from.z);
                if (d < best) { best = d; point = q; found = true; }
            }
            return found;
        }

        private static string Comps(GameObject go)
        {
            StringBuilder sb = new StringBuilder();
            foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                if (mb != null) sb.Append(sb.Length == 0 ? " Skripte: " : ", ").Append(mb.GetType().Name);
            return sb.ToString();
        }

        private static string B(Bounds b)
        {
            return "min " + b.min.ToString("F2") + " max " + b.max.ToString("F2");
        }

        private static string Col(Collider c)
        {
            return GameInfo.Path(c.transform) + " [" + c.GetType().Name + (c.isTrigger ? ", Trigger" : "") + (c.enabled && c.gameObject.activeInHierarchy ? "" : ", AUS")
                + ", Ebene " + LayerMask.LayerToName(c.gameObject.layer) + "] " + B(c.bounds);
        }

        /// <summary>Writes doors, their colliders, walls and colliders around the player.</summary>
        public static void Protocol(string reason, bool once)
        {
            string scene = GameInfo.SceneName;
            if (once && _written.Contains(scene)) return;
            _written.Add(scene);
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Raum ").Append(scene).Append(" (").Append(reason).Append(") ").Append(DateTime.Now.ToString("HH:mm:ss")).Append("\r\n");
                RCG.Player p = GameInfo.Player;
                Vector3 pp = p != null ? p.transform.position : Vector3.zero;
                sb.Append("Spieler ").Append(pp.ToString("F2")).Append(" GameState ").Append(GameState.CurrentState).Append("\r\n");
                if (p != null) foreach (Collider c in p.GetComponentsInChildren<Collider>()) sb.Append("  Spieler-Kollider ").Append(Col(c)).Append("\r\n");
                foreach (Door d in UnityEngine.Object.FindObjectsOfType<Door>())
                {
                    sb.Append("Tuer ").Append(GameInfo.Path(d.transform)).Append(" pos ").Append(d.transform.position.ToString("F2"))
                      .Append(" nach ").Append(d.DoorData.ToScene).Append("/").Append(d.DoorData.ToDoor).Append(" Richtung ").Append(d.DoorData.DoorEnterDirection)
                      .Append(" aktiv ").Append(d.isActiveAndEnabled).Append(" kannRein ").Append(d.CanEnter).Append(" Interaktion ").Append(d.CanInteract)
                      .Append(" Spawn1 ").Append(d.DoorData.SpawnPoint1 != null ? d.DoorData.SpawnPoint1.position.ToString("F2") : "-").Append("\r\n");
                    foreach (Collider c in Colliders(d)) sb.Append("  ").Append(Col(c)).Append("\r\n");
                }
                foreach (DoorTrigger_Normal t in UnityEngine.Object.FindObjectsOfType<DoorTrigger_Normal>())
                {
                    Collider c = t.GetComponent<Collider>();
                    sb.Append("Randausgang ").Append(GameInfo.Path(t.transform)).Append(c != null ? " " + Col(c) : "").Append("\r\n");
                }
                foreach (InteractEntity e in UnityEngine.Object.FindObjectsOfType<InteractEntity>())
                {
                    if (e is Door) continue;
                    sb.Append("Interaktion ").Append(e.GetType().Name).Append(" ").Append(GameInfo.Path(e.transform)).Append(" pos ").Append(e.transform.position.ToString("F2")).Append("\r\n");
                }
                try
                {
                    ScreenLock sl = ScreenLock.Instance;
                    if (sl != null)
                    {
                        sb.Append("Kampfbereich ").Append(sl.GetState).Append("\r\n");
                        foreach (string w in new string[] { "_LeftWall", "_RightWall" })
                        {
                            Collider c = UiReader.Traverse(sl, w) as Collider;
                            if (c != null) sb.Append("  ").Append(w).Append(" ").Append(Col(c)).Append("\r\n");
                        }
                    }
                }
                catch { }
                foreach (CameraBorder cb in UnityEngine.Object.FindObjectsOfType<CameraBorder>())
                {
                    Collider2D c2 = cb.GetComponent<Collider2D>();
                    sb.Append("Kameragrenze ").Append(GameInfo.Path(cb.transform)).Append(c2 != null ? " min " + c2.bounds.min.ToString("F2") + " max " + c2.bounds.max.ToString("F2") : "").Append("\r\n");
                }
                try
                {
                    NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                    if (tri.vertices != null && tri.vertices.Length > 0)
                    {
                        Bounds nb = new Bounds(tri.vertices[0], Vector3.zero);
                        foreach (Vector3 v in tri.vertices) nb.Encapsulate(v);
                        sb.Append("NavMesh ").Append(B(nb)).Append(", ").Append(tri.vertices.Length).Append(" Punkte\r\n");
                    }
                }
                catch { }
                try
                {
                    foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                    {
                        string tn = mb.GetType().Name;
                        if (tn == "PlayableDirector" || tn.IndexOf("Trigger", StringComparison.Ordinal) >= 0 || tn.IndexOf("Timeline", StringComparison.Ordinal) >= 0
                            || tn.IndexOf("Cinematic", StringComparison.Ordinal) >= 0 || tn.IndexOf("Portrait", StringComparison.Ordinal) >= 0)
                            sb.Append("Skript ").Append(tn).Append(" an ").Append(GameInfo.Path(mb.transform)).Append(mb.isActiveAndEnabled ? "" : " (AUS)").Append(" pos ").Append(mb.transform.position.ToString("F2")).Append("\r\n");
                    }
                    foreach (Pit pit in UnityEngine.Object.FindObjectsOfType<Pit>())
                        foreach (Collider c in pit.GetComponentsInChildren<Collider>())
                            sb.Append("Grube ").Append(Col(c)).Append("\r\n");
                    foreach (UnityEngine.Playables.PlayableDirector pd in UnityEngine.Object.FindObjectsOfType<UnityEngine.Playables.PlayableDirector>())
                        sb.Append("Timeline ").Append(GameInfo.Path(pd.transform)).Append(" Zustand ").Append(pd.state).Append(" Zeit ").Append(pd.time.ToString("F1")).Append("/").Append(pd.duration.ToString("F1"))
                          .Append(" automatisch ").Append(pd.playOnAwake).Append("\r\n");
                }
                catch (Exception e) { sb.Append("Skriptliste Fehler ").Append(e.Message).Append("\r\n"); }
                if (p != null)
                {
                    sb.Append("Kollider im Umkreis von 12 m:\r\n");
                    int n = 0;
                    foreach (Collider c in Physics.OverlapSphere(pp, 12f, ~0, QueryTriggerInteraction.Collide))
                    {
                        if (c.GetComponentInParent<CombatEntity>() != null) continue;
                        sb.Append("  ").Append(Col(c)).Append(c.isTrigger ? Comps(c.gameObject) : "").Append("\r\n");
                        if (++n >= 150) break;
                    }
                }
                string f = Path.Combine(Mod.ProtoDir, "raum_" + scene + "_" + DateTime.Now.ToString("HH-mm-ss") + ".txt");
                File.WriteAllText(f, sb.ToString(), new UTF8Encoding(false));
                Log.Write("Raumprotokoll geschrieben: " + Path.GetFileName(f));
            }
            catch (Exception e) { Log.Error("Geometry.Protocol", e); }
        }
    }
}
