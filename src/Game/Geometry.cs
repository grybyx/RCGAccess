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

        public static void OnSceneLoaded() { _doorCols.Clear(); }

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
                // stand well inside the area: centre for narrow areas, otherwise half a metre from its sides
                float mx = b.size.x < 2f ? b.size.x * 0.5f : 0.5f;
                float qx = Mathf.Clamp(from.x, b.min.x + mx, b.max.x - mx);
                Vector3 q = new Vector3(qx, from.y, Mathf.Clamp(from.z, b.min.z, b.max.z));
                float d = (q.x - from.x) * (q.x - from.x) + (q.z - from.z) * (q.z - from.z);
                if (d < best) { best = d; point = q; found = true; }
            }
            return found;
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
                if (p != null)
                {
                    sb.Append("Kollider im Umkreis von 12 m:\r\n");
                    int n = 0;
                    foreach (Collider c in Physics.OverlapSphere(pp, 12f, ~0, QueryTriggerInteraction.Collide))
                    {
                        if (c.GetComponentInParent<CombatEntity>() != null) continue;
                        sb.Append("  ").Append(Col(c)).Append("\r\n");
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
