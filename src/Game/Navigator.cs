using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RCG;
using UnityEngine;
using UnityEngine.AI;

namespace RCGAccess
{
    /// <summary>Exits and points of interest, beacon tone, guided walking ("Hinführen"), room announcements,
    /// dead-end hints, pit warnings, interaction hints and a learned room map for quest directions.</summary>
    public static class Navigator
    {
        public enum Kind { Exit, Shop, Person, Thing, Climb, Weapon, Item, Beggar }

        public class Target
        {
            public Kind Kind;
            public string Name;
            public Component Obj;
            public Door Door;
            public Collider Trigger;
            public DoorEnterDirection Dir;
            public bool IsEdge;
            public bool Seen = true;
            public bool QuestRoute;
            public string ToScene = "";
            public string RoomGoal;          // marked room this target leads to (navigator "room" target)

            public Vector3 RawPos
            {
                get
                {
                    if (Trigger != null) return Trigger.bounds.center;
                    return Obj != null ? Obj.transform.position : Vector3.zero;
                }
            }

            private Vector3 _goal, _goalFrom;
            private float _goalAt = -10f;

            /// <summary>Walkable point next to the target (doors often sit in the wall, outside the walkable floor).</summary>
            public Vector3 Pos
            {
                get
                {
                    // doors: the spot of the interaction area nearest to the player
                    if (Kind == Kind.Exit && Door != null && !IsEdge)
                    {
                        RCG.Player pl = GameInfo.Player;
                        Vector3 cp;
                        if (pl != null && Geometry.ClosestPoint(Door, pl.transform.position, out cp)) return cp;
                    }
                    Vector3 raw = RawPos;
                    float now = Time.realtimeSinceStartup;
                    if (now - _goalAt > 1f || (raw - _goalFrom).sqrMagnitude > 0.09f)
                    {
                        _goalFrom = raw;
                        _goalAt = now;
                        _goal = Walkable(raw);
                    }
                    return _goal;
                }
            }

            public bool Locked { get { return Kind == Kind.Exit && Door != null && DoorLocked(Door); } }

            public string Label()
            {
                string s = Name;
                if (RoomGoal != null) return s + (Locked ? ", " + Loc.T("locked") : "");
                if (Kind == Kind.Exit)
                {
                    if (Locked) s += ", " + Loc.T("locked");
                    else if (QuestRoute) s += ", " + Loc.T("quest_route");
                    else if (!Seen) s += ", " + Loc.T("unvisited");
                }
                return s;
            }
        }

        private static readonly List<Target> _targets = new List<Target>();
        private static int _sel;
        private static Component _selObj;
        private static string _selRoom;
        private static float _nextBuild;
        private static bool _beacon;
        private static bool _beaconByWalk; // beacon was switched on only for guided walking
        private static float _nextBeep;
        private static bool _arrivedSaid;
        private static Component _arrivedObj;

        // room announcement
        private static string _scene = "";
        private static float _announceAt = -1f;

        // guided walking
        public static bool AutoWalk;
        private static Target _walkTarget;
        private static float _walkStart;
        private static readonly List<Vector3> _path = new List<Vector3>();
        private static int _pathIdx;
        private static float _replanAt;
        private static Vector3 _stuckPos;
        private static float _stuckSince;
        private static int _stuckMode;
        private static float _pushUntil;

        // dead end
        private static float _deTimer;
        private static Vector3 _dePos;
        private static int _deDirH, _deDirV;
        private static float _deToneAt, _deSpeechAt;
        private static int _deVCount;

        // hazards
        private static readonly Dictionary<int, float> _hazardWarned = new Dictionary<int, float>();
        private static float _hazardSpeechAt;
        private static float _nextHazardScan;
        private static List<Collider> _pits = new List<Collider>();

        // interaction hints
        private static readonly Dictionary<int, float> _interactSaid = new Dictionary<int, float>();
        private static readonly HashSet<int> _inRange = new HashSet<int>();

        /// <summary>True while the game shows the interaction symbol of this object for player 1.</summary>
        public static bool InRange(Component c) { return c != null && _inRange.Contains(c.GetInstanceID()); }

        // room map (learned)
        private static readonly Dictionary<string, HashSet<string>> _graph = new Dictionary<string, HashSet<string>>();
        private static bool _graphLoaded;

        private static FieldInfo _dtnDoor = typeof(DoorTrigger_Normal).GetField("myDoor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _storeName = typeof(InteractEntity_Store).GetField("_storeName", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _npcName = typeof(InteractEntity_QuestNPC).GetField("_npcName", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _envType = typeof(InteractEntity_Environment).GetField("_interactType", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void OnSceneLoaded(string scene)
        {
            try
            {
                if (_lastDoorInRange != null && !string.IsNullOrEmpty(_scene)) ForgetLocked(_scene, _lastDoorInRange);
            }
            catch { }
            _lastDoorInRange = null;
            _targets.Clear();
            _selObj = null;
            _sel = 0;
            _nextBuild = 0;
            _hazardWarned.Clear();
            _interactSaid.Clear();
            _inRange.Clear();
            _pits.Clear();
            _nextHazardScan = 0;
            _knownItems.Clear();
            Geometry.OnSceneLoaded();
            _pendingItems.Clear();
            _nextItemScan = Time.realtimeSinceStartup + 1.5f;
            if (AutoWalk) StopWalk(null);
            if (scene != _scene)
            {
                _scene = scene;
                _announceAt = Time.realtimeSinceStartup + 0.8f;
            }
        }

        // ---------------- targets ----------------
        public static void BuildTargets()
        {
            _targets.Clear();
            try
            {
                Dictionary<Door, Collider> triggers = new Dictionary<Door, Collider>();
                foreach (DoorTrigger_Normal t in UnityEngine.Object.FindObjectsOfType<DoorTrigger_Normal>())
                {
                    Door d = _dtnDoor != null ? _dtnDoor.GetValue(t) as Door : null;
                    Collider c = t.GetComponent<Collider>();
                    if (d != null && c != null && !triggers.ContainsKey(d)) triggers[d] = c;
                }
                foreach (Door d in UnityEngine.Object.FindObjectsOfType<Door>())
                {
                    if (!d.isActiveAndEnabled) continue;
                    DoorData dd = d.DoorData;
                    if (string.IsNullOrEmpty(dd.ToScene)) continue;
                    Target t = new Target();
                    t.Kind = Kind.Exit;
                    t.Obj = d;
                    t.Door = d;
                    t.ToScene = dd.ToScene;
                    t.Dir = dd.DoorEnterDirection;
                    Collider c;
                    if (triggers.TryGetValue(d, out c)) { t.Trigger = c; t.IsEdge = true; }
                    else if (d.GetComponent<DoorTrigger_Normal>() != null) { t.Trigger = d.GetComponent<Collider>(); t.IsEdge = true; }
                    try { t.Seen = PlayerGlobalInventory.instance == null || PlayerGlobalInventory.instance.HaveYouSeenScene(dd.ToScene); } catch { }
                    t.Name = Loc.T(t.IsEdge ? "exit_to" : "door_to", GameInfo.RoomName(dd.ToScene), Loc.T("edir_" + dd.DoorEnterDirection.ToString().ToLower()));
                    _targets.Add(t);
                }
                foreach (InteractEntity_Store s in UnityEngine.Object.FindObjectsOfType<InteractEntity_Store>())
                {
                    if (!s.isActiveAndEnabled) continue;
                    string n = _storeName != null ? _storeName.GetValue(s) as string : null;
                    _targets.Add(new Target { Kind = Kind.Shop, Obj = s, Name = Loc.T("shop", NiceName(n, s.name)) });
                }
                foreach (InteractEntity_QuestNPC q in UnityEngine.Object.FindObjectsOfType<InteractEntity_QuestNPC>())
                {
                    if (!q.isActiveAndEnabled) continue;
                    string n = _npcName != null ? _npcName.GetValue(q) as string : null;
                    _targets.Add(new Target { Kind = Kind.Person, Obj = q, Name = Loc.T("person", NiceName(n, q.name)) });
                }
                foreach (InteractEntity_Environment en in UnityEngine.Object.FindObjectsOfType<InteractEntity_Environment>())
                {
                    if (!en.isActiveAndEnabled || !en.CanInteract) continue;
                    _targets.Add(new Target { Kind = Kind.Thing, Obj = en, Name = EnvName(en) });
                }
                AddItemTargets();
                foreach (EnemyEnitity e in Combat.Enemies())
                    if (Combat.IsBegging(e)) _targets.Add(new Target { Kind = Kind.Beggar, Obj = e, Name = Loc.T("beggar_target", Combat.EnemyName(e)) });
                foreach (Climable cl in UnityEngine.Object.FindObjectsOfType<Climable>())
                {
                    if (!cl.isActiveAndEnabled) continue;
                    _targets.Add(new Target { Kind = Kind.Climb, Obj = cl, Name = Loc.T("climb") });
                }
            }
            catch (Exception e) { Log.Error("BuildTargets", e); }

            LearnGraph();
            MarkQuestRoute();
            AddRoomTargets();

            RCG.Player p = GameInfo.Player;
            Vector3 pp = p != null ? p.transform.position : Vector3.zero;
            // order: exits on the quest route, unvisited exits, other exits, then the rest; each by distance
            _targets.Sort(delegate (Target a, Target b)
            {
                int ra = Rank(a), rb = Rank(b);
                if (ra != rb) return ra.CompareTo(rb);
                return Vector3.Distance(pp, a.Pos).CompareTo(Vector3.Distance(pp, b.Pos));
            });
            _sel = 0;
            if (_selObj != null)
                for (int i = 0; i < _targets.Count; i++) if (_targets[i].Obj == _selObj && _targets[i].RoomGoal == _selRoom) { _sel = i; break; }
            if (_targets.Count > 0) { _selObj = _targets[_sel].Obj; _selRoom = _targets[_sel].RoomGoal; }
        }

        private static int Rank(Target t)
        {
            if (t.RoomGoal != null) return -1;
            if (t.Kind == Kind.Beggar) return -2;
            if (t.Kind == Kind.Item) return 0;
            if (t.Kind == Kind.Exit)
            {
                if (t.Locked) return 5;
                return t.QuestRoute ? 1 : (!t.Seen ? 2 : 3);
            }
            return 4;
        }

        // ---------------- locked doors ----------------
        private static FieldInfo _noInteract = typeof(InteractEntity).GetField("_noInteract", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly HashSet<string> _lockedDoors = new HashSet<string>();
        private static bool _lockedLoaded;

        private static string DoorKey(Door d) { return (GameInfo.SceneName + "/" + d.name).ToLower(); }

        private static string LockedFile { get { return Path.Combine(Mod.DataDir, "verschlossen.txt"); } }

        /// <summary>Doors the game keeps shut (no entry, no interaction, or the "locked out" tutorial appeared there).</summary>
        public static bool DoorLocked(Door d)
        {
            if (d == null) return false;
            try
            {
                if (!d.CanEnter) return true;
                if (_noInteract != null && (bool)_noInteract.GetValue(d)) return true;
            }
            catch { }
            if (!_lockedLoaded)
            {
                _lockedLoaded = true;
                try { if (File.Exists(LockedFile)) foreach (string l in File.ReadAllLines(LockedFile)) if (l.Trim().Length > 0) _lockedDoors.Add(l.Trim()); } catch { }
            }
            return _lockedDoors.Contains(DoorKey(d));
        }

        /// <summary>A tutorial while standing at a door that did not open = the door is locked for now ("Ausgeschlossen").</summary>
        private static string _lastDoorInRange;

        public static void OnTutorialShown(string keys, string text)
        {
            string k0 = (keys ?? "").ToUpper(), t0 = (text ?? "").ToLower();
            bool lockTutorial = k0.Contains("LOCK") || t0.Contains("verschlossen") || t0.Contains("locked") || t0.Contains("noch nicht rein");
            Log.Write("Tutorial-Schluessel: " + keys + (lockTutorial ? " (verschlossene Tuer)" : ""));
            if (!lockTutorial) return;
            try
            {
                foreach (Door d in UnityEngine.Object.FindObjectsOfType<Door>())
                {
                    if (!InRange(d)) continue;
                    string k = DoorKey(d);
                    if (_lockedDoors.Add(k))
                    {
                        Log.Write("Tuer als verschlossen gemerkt: " + k);
                        try { File.AppendAllText(LockedFile, k + "\r\n"); } catch { }
                    }
                }
            }
            catch (Exception e) { Log.Error("OnTutorialShown", e); }
        }

        /// <summary>Called when the player really went through a door: it is not locked (any more).</summary>
        private static void ForgetLocked(string scene, string doorName)
        {
            string k = (scene + "/" + doorName).ToLower();
            if (!_lockedDoors.Remove(k)) return;
            try
            {
                List<string> rest = new List<string>(_lockedDoors);
                File.WriteAllText(LockedFile, string.Join("\r\n", rest.ToArray()) + "\r\n");
            }
            catch { }
        }

        private static NavMeshQueryFilter Filter()
        {
            if (_agentType < 0)
            {
                _agentType = 0;
                try
                {
                    NavMeshSurface s = UnityEngine.Object.FindObjectOfType<NavMeshSurface>();
                    if (s != null) _agentType = s.agentTypeID;
                }
                catch { }
            }
            NavMeshQueryFilter f = new NavMeshQueryFilter();
            f.agentTypeID = _agentType;
            f.areaMask = NavMesh.AllAreas;
            return f;
        }

        public static Vector3 Walkable(Vector3 p)
        {
            try
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(p, out hit, 6f, Filter())) return new Vector3(hit.position.x, p.y, hit.position.z);
            }
            catch { }
            return p;
        }

        /// <summary>A begging enemy becomes the navigator target, so a short right stick press walks into it to grab it.</summary>
        public static void FocusBeggar(EnemyEnitity e)
        {
            if (e == null || AutoWalk) return;
            _selObj = e;
            _selRoom = null;
            BuildTargets();
        }

        // ---------------- items lying around ----------------
        private static readonly HashSet<int> _knownItems = new HashSet<int>();
        private static readonly List<InteractEntity> _pendingItems = new List<InteractEntity>();
        private static float _nextItemScan;

        private static void AddItemTargets()
        {
            if (!Settings.ItemNav) return;
            foreach (InteractEntity e in UnityEngine.Object.FindObjectsOfType<InteractEntity>())
            {
                if (!(e is InteractEntity_DropV2) && !(e is InteractEntity_Drop)) continue;
                if (!e.isActiveAndEnabled) continue;
                bool imp;
                string n = Pickups.Describe(e, out imp);
                if (n == null || !imp) continue;
                _targets.Add(new Target { Kind = Kind.Item, Obj = e, Name = Loc.T("item_target", n) });
            }
        }

        /// <summary>Announce important items that appeared (dropped by enemies, from boxes) and point the navigator to them.</summary>
        private static void ScanNewItems(RCG.Player p, float now)
        {
            if (!Settings.ItemNav || now < _nextItemScan) return;
            _nextItemScan = now + 0.7f;
            foreach (InteractEntity e in UnityEngine.Object.FindObjectsOfType<InteractEntity>())
            {
                if (!(e is InteractEntity_DropV2) && !(e is InteractEntity_Drop)) continue;
                if (!e.isActiveAndEnabled) continue;
                int id = e.GetInstanceID();
                if (_knownItems.Contains(id)) continue;
                _knownItems.Add(id);
                bool imp;
                string n = Pickups.Describe(e, out imp);
                if (n == null || !imp) continue;
                _pendingItems.Add(e);
            }
            if (_pendingItems.Count == 0 || Combat.InFight || AutoWalk) return;
            InteractEntity best = null;
            float bd = 9999f;
            foreach (InteractEntity e in _pendingItems)
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                float d = Vector3.Distance(p.transform.position, e.transform.position);
                if (d < bd) { bd = d; best = e; }
            }
            int count = 0;
            foreach (InteractEntity e in _pendingItems) if (e != null && e.isActiveAndEnabled) count++;
            _pendingItems.Clear();
            if (best == null) return;
            bool imp2;
            string name = Pickups.Describe(best, out imp2);
            _selObj = best;
            _selRoom = null;
            BuildTargets();
            Speech.Tone(1046, 80, Combat.StrongPan(best.transform.position.x - p.transform.position.x), 0.6f, 0, 1568, 0);
            string s = Loc.T("item_appeared", name, GameInfo.Direction(p.transform.position, Walkable(best.transform.position)));
            if (count > 1) s = Loc.T("items_appeared", count) + " " + s;
            if (Settings.PadKeys) s += " " + Loc.T("item_guide_help");
            Speech.Say(s, false);
        }

        private static string NiceName(string key, string fallback)
        {
            if (!string.IsNullOrEmpty(key))
            {
                string t = GameInfo.Translate(key);
                if (!string.IsNullOrEmpty(t) && t != "null") return t;
                return GameInfo.Readable(key);
            }
            return GameInfo.Readable(fallback);
        }

        private static string EnvName(InteractEntity_Environment en)
        {
            try
            {
                object v = _envType != null ? _envType.GetValue(en) : null;
                if (v != null) return Loc.T("env_" + v.ToString().ToLower());
            }
            catch { }
            return GameInfo.Readable(en.name);
        }

        public static Target Current
        {
            get
            {
                if (_targets.Count == 0) return null;
                if (_sel < 0 || _sel >= _targets.Count) _sel = 0;
                Target t = _targets[_sel];
                if (t.Obj == null) return null;
                return t;
            }
        }

        public static void Cycle(int dir)
        {
            BuildTargets();
            if (_targets.Count == 0) { Speech.Say(Loc.T("no_targets"), true); return; }
            _sel = (_sel + dir + _targets.Count) % _targets.Count;
            _selObj = _targets[_sel].Obj;
            _selRoom = _targets[_sel].RoomGoal;
            _arrivedSaid = false;
            SayTarget(true);
        }

        public static void SayTarget(bool withIndex)
        {
            Target t = Current;
            RCG.Player p = GameInfo.Player;
            if (t == null || p == null) { Speech.Say(Loc.T("no_targets"), true); return; }
            string s = t.Label() + ", " + GameInfo.Direction(p.transform.position, t.Pos);
            if (withIndex) s += ". " + Loc.T("pos", _sel + 1, _targets.Count);
            Speech.Say(s, true);
        }

        public static void ToggleBeacon()
        {
            _beacon = !_beacon;
            _beaconByWalk = false;
            if (_beacon)
            {
                if (_targets.Count == 0) BuildTargets();
                _arrivedSaid = false;
                Target t = Current;
                Speech.Say(Loc.T("nav_on") + (t != null ? ": " + t.Label() + ", " + GameInfo.Direction(GameInfo.Player != null ? GameInfo.Player.transform.position : t.Pos, t.Pos) : ". " + Loc.T("no_targets")), true);
            }
            else Speech.Say(Loc.T("nav_off"), true);
        }

        public static void DescribeRoom(bool full, bool interrupt = true)
        {
            BuildTargets();
            RCG.Player p = GameInfo.Player;
            List<string> parts = new List<string>();
            parts.Add(GameInfo.RoomName(GameInfo.SceneName));
            int exits = 0, unseen = 0;
            foreach (Target t in _targets) if (t.Kind == Kind.Exit) { exits++; if (!t.Seen) unseen++; }
            if (exits == 0) parts.Add(Loc.T("no_exits"));
            else if (!full)
            {
                parts.Add(unseen > 0 ? Loc.T("exits_n_new", exits, unseen) : Loc.T("exits_n", exits));
                foreach (Target t in _targets) if (t.QuestRoute) { parts.Add(Loc.T("quest_dir", t.Name, p != null ? GameInfo.Direction(p.transform.position, t.Pos) : "")); break; }
                if (QuestHere) parts.Add(Loc.T("mark_here"));
            }
            if (full && p != null)
            {
                foreach (Target t in _targets)
                    parts.Add(t.Label() + ", " + GameInfo.Direction(p.transform.position, t.Pos));
            }
            if (Combat.ScreenLockRunning) parts.Add(Loc.T("st_locked"));
            Speech.Say(string.Join(". ", parts.ToArray()), interrupt);
        }

        public static void PreviewBeacon()
        {
            Beep(new Vector3(0, 0, 0), new Vector3(4, 0, 0), Kind.Exit, false);
            Beep(new Vector3(0, 0, 0), new Vector3(-4, 0, 2.5f), Kind.Exit, true, 350);
        }

        private static void Beep(Vector3 from, Vector3 to, Kind kind, bool quest, float delay = 0)
        {
            float dx = to.x - from.x, dz = to.z - from.z;
            float f = 330f;
            if (dz > 0.5f) f *= Mathf.Lerp(1.25f, 2f, Mathf.Clamp01((dz - 0.5f) / 4f));
            else if (dz < -0.5f) f *= Mathf.Lerp(0.8f, 0.5f, Mathf.Clamp01((-dz - 0.5f) / 4f));
            int wave = quest ? 2 : (kind == Kind.Exit ? 1 : 0);
            float vol = quest ? 0.45f : 0.7f;
            Speech.Tone(f, 70, GameInfo.Pan(dx, 8f), vol, wave, f, delay);
        }

        // ---------------- update ----------------
        public static void Update()
        {
            float now = Time.realtimeSinceStartup;
            RCG.Player p = GameInfo.Player;
            bool quiet = GameInfo.Quiet;

            if (_announceAt > 0 && now >= _announceAt)
            {
                if (p != null && !quiet && !GameInfo.Fading)
                {
                    _announceAt = -1f;
                    BuildTargets();
                    if (Settings.RoomAnnounce) DescribeRoom(false, false);
                    Geometry.Protocol("Raumeintritt", true);
                }
                else if (now - _announceAt > 20f) _announceAt = -1f;
            }
            if (p == null || quiet) return;

            if (now >= _nextBuild) { _nextBuild = now + 3f; BuildTargets(); }

            Target t = Current;
            Vector3 pp = p.transform.position;
            if (_beacon && t != null)
            {
                float dist = Vector3.Distance(new Vector3(pp.x, 0, pp.z), new Vector3(t.Pos.x, 0, t.Pos.z));
                if (dist < 1.0f)
                {
                    if (!_arrivedSaid)
                    {
                        _arrivedSaid = true;
                        Speech.Tone(392, 250, 0, 0.7f, 0, 784, 0);
                        // say it only once per target, never in the middle of a fight
                        if (!AutoWalk && _arrivedObj != t.Obj && !Combat.InFight && !Combat.ScreenLockRunning && now - _lastArrivalAt > 3f)
                        {
                            _arrivedObj = t.Obj;
                            SayArrival(Loc.T("arrived", t.Label()) + ArrivalHint(t));
                        }
                    }
                }
                else
                {
                    if (dist > 2f) _arrivedSaid = false;
                    if (dist > 4f && _arrivedObj == t.Obj) _arrivedObj = null;
                    // the beacon pauses while an enemy is close (the enemy knock has priority), unless guiding to a beggar
                    bool enemyClose = Settings.AutoWalkStopForEnemies && Combat.NearestHostileDist < 4.5f && t.Kind != Kind.Beggar;
                    if (now >= _nextBeep && !enemyClose)
                    {
                        _nextBeep = now + Mathf.Lerp(0.2f, 1.2f, Mathf.Clamp01(dist / 30f));
                        Beep(pp, t.Pos, t.Kind, t.QuestRoute);
                    }
                }
            }
            UpdateHazards(p, now);
            ScanNewItems(p, now);
        }

        private static string ArrivalHint(Target t)
        {
            if (t.Locked) return " " + Loc.T("door_locked_long");
            switch (t.Kind)
            {
                case Kind.Exit: return t.IsEdge ? " " + Loc.T("hint_walk", Loc.T("edir_" + t.Dir.ToString().ToLower())) : " " + Loc.T("hint_hold", GameInfo.ButtonName(2));
                case Kind.Climb: return " " + Loc.T("hint_climb");
                case Kind.Beggar: return " " + Loc.T("hint_beggar");
                case Kind.Item:
                    {
                        InteractEntity ie = t.Obj as InteractEntity;
                        if (Pickups.IsFood(ie) && Pickups.HealthFull()) return " " + Loc.T("hint_food_full");
                        if (ie != null && ie.AutoInteract) return " " + Loc.T("hint_touch");
                        return " " + Loc.T("hint_press", GameInfo.ButtonName(2));
                    }
                default: return " " + Loc.T("hint_press", GameInfo.ButtonName(2));
            }
        }

        // ---------------- hazards ----------------
        private static void UpdateHazards(RCG.Player p, float now)
        {
            if (!Settings.Hazards) return;
            if (now >= _nextHazardScan)
            {
                _nextHazardScan = now + 2f;
                _pits.Clear();
                try
                {
                    foreach (Pit pit in UnityEngine.Object.FindObjectsOfType<Pit>())
                        foreach (Collider c in pit.GetComponentsInChildren<Collider>())
                            if (c.enabled) _pits.Add(c);
                }
                catch { }
            }
            Vector3 pp = p.transform.position;
            foreach (Collider c in _pits)
            {
                if (c == null) continue;
                Vector3 cp = c.bounds.ClosestPoint(pp);
                float d = Vector2.Distance(new Vector2(cp.x, cp.z), new Vector2(pp.x, pp.z));
                if (d > 1.6f) continue;
                int id = c.GetInstanceID();
                float last;
                if (_hazardWarned.TryGetValue(id, out last) && now - last < 3f) continue;
                _hazardWarned[id] = now;
                float pan = GameInfo.Pan(cp.x - pp.x, 3f);
                Speech.Tone(300, 260, pan, 0.8f, 0, 150, 0);
                if (now - _hazardSpeechAt > 12f)
                {
                    _hazardSpeechAt = now;
                    string where = Mathf.Abs(cp.x - pp.x) > Mathf.Abs(cp.z - pp.z) ? GameInfo.SideWord(cp.x - pp.x) : Loc.T(cp.z > pp.z ? "dir_up" : "dir_down");
                    Speech.Say(Loc.T("pit_warn", where), false);
                }
            }
        }

        // ---------------- interaction hints (from patch) ----------------
        public static void OnInteractSymbol(InteractEntity e, int player, bool enabled)
        {
            try
            {
                if (player != 0 || e == null) return;
                int rid = e.GetInstanceID();
                Door door = e as Door;
                bool locked = door != null && (Combat.ScreenLockRunning || !door.CanEnter);
                if (!enabled)
                {
                    // leaving the area of a door or object: short falling tone
                    if (_inRange.Remove(rid) && !locked && Settings.Interact && !e.AutoInteract && !GameInfo.Quiet && !(e is PickupObject))
                        Speech.Tone(880, 120, 0, 0.45f, 1, 440, 0);
                    return;
                }
                _inRange.Add(rid);
                if (door != null) _lastDoorInRange = door.name;
                if (locked) return; // doors cannot be used during a fight: stay silent
                if (door != null && DoorLocked(door))
                {
                    float nowL = Time.realtimeSinceStartup, lastL;
                    if (_interactSaid.TryGetValue(rid, out lastL) && nowL - lastL < 20f) return;
                    _interactSaid[rid] = nowL;
                    if (Settings.Interact && !GameInfo.Quiet) Speech.Say(Describe(e) + ", " + Loc.T("locked"), false);
                    return;
                }
                if (AutoWalk && _walkTarget != null && _walkTarget.Obj == e && !_walkTarget.IsEdge) return; // arrival is announced by the guide
                if (!Settings.Interact) return;
                if (e.AutoInteract) return;
                if (GameInfo.Quiet) return;
                if (e is PickupObject && ((PickupObject)e).HasOwner()) return;
                int id = e.GetInstanceID();
                float now = Time.realtimeSinceStartup;
                float last;
                bool isWeapon = e is PickupObject;
                if (_interactSaid.TryGetValue(id, out last) && now - last < (isWeapon ? 20f : 15f)) return;
                _interactSaid[id] = now;
                string what = Describe(e);
                bool hold = e is Door || e._howLongToHold > 0f;
                string act = hold ? Loc.T("hint_hold", GameInfo.ButtonName(2)) : Loc.T("hint_press", GameInfo.ButtonName(2));
                Speech.Tone(660, 60, 0, 0.5f, 1, 990, 0);
                if (isWeapon) Speech.Say(Loc.T("weapon_short", what, GameInfo.ButtonName(2)), false);
                else Speech.Say(what + ". " + act, !Combat.InFight);
            }
            catch (Exception ex) { Log.Error("OnInteractSymbol", ex); }
        }

        private static string Describe(InteractEntity e)
        {
            Door d = e as Door;
            if (d != null) return Loc.T("door_to", GameInfo.RoomName(d.DoorData.ToScene), Loc.T("edir_" + d.DoorData.DoorEnterDirection.ToString().ToLower()));
            if (e is InteractEntity_Store) return Loc.T("shop", NiceName(_storeName.GetValue(e) as string, e.name));
            if (e is InteractEntity_QuestNPC) return Loc.T("person", NiceName(_npcName.GetValue(e) as string, e.name));
            if (e is InteractEntity_Environment) return EnvName((InteractEntity_Environment)e);
            Weapon w = e as Weapon;
            if (w != null)
            {
                try { Data_WeaponItem wd = w.GetWeaponData(); if (wd != null) return Loc.T("weapon", NiceName(wd.ItemName, w.name)); } catch { }
                return Loc.T("weapon", GameInfo.Readable(w.name));
            }
            if (e is PickupObject) return Loc.T("object", GameInfo.Readable(e.name));
            return GameInfo.Readable(e.name);
        }

        // ---------------- guided walking ----------------
        public static void ToggleWalk()
        {
            if (AutoWalk) { StopWalk(Loc.T("walk_off")); return; }
            if (GameInfo.Quiet) return;
            BuildTargets();
            Target t = Current;
            RCG.Player p = GameInfo.Player;
            if (t == null || p == null) { Speech.Say(Loc.T("no_targets"), true); return; }
            if (Settings.AutoWalkStopForEnemies && Combat.NearestHostileDist < 4.5f && t.Kind != Kind.Beggar) { Speech.Say(Loc.T("walk_enemy_no"), true); return; }
            _walkTarget = t;
            AutoWalk = true;
            _walkStart = Time.realtimeSinceStartup;
            _path.Clear();
            _pathIdx = 0;
            _replanAt = 0;
            _stuckSince = Time.realtimeSinceStartup;
            _stuckPos = p.transform.position;
            _stuckMode = 0;
            _pushUntil = 0;
            if (!_beacon) { _beacon = true; _beaconByWalk = true; }
            Speech.Say(Loc.T("walk_on", t.Label(), GameInfo.Direction(p.transform.position, t.Pos)), true);
        }

        public static void StopWalk(string say)
        {
            AutoWalk = false;
            _walkTarget = null;
            _path.Clear();
            // a beacon that only came on for guiding goes off with it
            if (_beaconByWalk) { _beacon = false; _beaconByWalk = false; }
            if (say != null) Speech.Say(say, true);
        }

        private static int _agentType = -1;
        private static float _lastArrivalAt = -10f;
        private static float _traceAt;
        private static int _failProtocols;

        private static void Plan(RCG.Player p, Vector3 from, Vector3 to)
        {
            _path.Clear();
            _pathIdx = 0;
            List<Vector3> grid = PathGrid.Find(p, from, to);
            if (grid != null && grid.Count > 0)
            {
                _path.AddRange(grid);
                // reachable end close to the goal: finish on the exact goal
                Vector3 last = _path[_path.Count - 1];
                if (Mathf.Abs(last.x - to.x) < 0.3f && Mathf.Abs(last.z - to.z) < 0.3f) _path[_path.Count - 1] = new Vector3(to.x, last.y, to.z);
                return;
            }
            _path.Add(to);
        }

        private static void Arrive(Target t, string text)
        {
            StopWalk(null);
            Speech.Tone(392, 250, 0, 0.7f, 0, 784, 0);
            _arrivedSaid = true;
            _arrivedObj = t.Obj;
            if (text != null) SayArrival(text);
            _lastArrivalAt = Time.realtimeSinceStartup;
        }

        private static string _lastArrivalText;

        /// <summary>One arrival announcement, never the same one twice within 3 seconds.</summary>
        private static void SayArrival(string text)
        {
            float now = Time.realtimeSinceStartup;
            if (text == _lastArrivalText && now - _lastArrivalAt < 3f) { Log.Write("Ankunft doppelt unterdrueckt, Aufruf: " + new System.Diagnostics.StackTrace(1, false).ToString().Replace("\r", "").Replace("\n", " | ")); return; }
            _lastArrivalText = text;
            _lastArrivalAt = now;
            Speech.Say(text, true);
        }

        public static bool WalkRun;
        private static InteractEntity _autoEnterDoor;
        private static float _autoEnterUntil;

        /// <summary>After guiding to an open door: hold the interact button until the game takes the player through.</summary>
        public static bool AutoEnter(int rawH, int rawV)
        {
            if (_autoEnterDoor == null) return false;
            float now = Time.realtimeSinceStartup;
            if (now > _autoEnterUntil || rawH != 0 || rawV != 0 || !_autoEnterDoor.gameObject.activeInHierarchy || !InRange(_autoEnterDoor))
            {
                _autoEnterDoor = null;
                return false;
            }
            return true;
        }

        public static bool WalkInput(RCG.Player p, int rawH, int rawV, out int h, out int v)
        {
            h = 0; v = 0;
            WalkRun = false;
            if (!AutoWalk) return false;
            float now = Time.realtimeSinceStartup;
            Target t = _walkTarget;
            if (t == null || t.Obj == null || !t.Obj.gameObject.activeInHierarchy)
            {
                // an item that vanished was picked up (announced by the pickup hook)
                bool picked = t != null && t.Kind == Kind.Item;
                StopWalk(picked ? null : Loc.T("walk_lost"));
                return false;
            }
            if ((rawH != 0 || rawV != 0) && now - _walkStart > 0.6f) { StopWalk(Loc.T("walk_manual")); return false; }
            bool beggar = t.Kind == Kind.Beggar;
            if (beggar)
            {
                EnemyEnitity be = t.Obj as EnemyEnitity;
                if (p.Puppet != null && p.Puppet.IsMaster) { Arrive(t, null); return false; } // grabbed: the grab message follows
                if (be == null || !Combat.IsBegging(be)) { StopWalk(Loc.T("beg_over")); return false; }
            }
            else if (Settings.AutoWalkStopForEnemies && Combat.NearestHostileDist < 4.5f) { StopWalk(Loc.T("walk_enemy")); return false; }
            Vector3 pos = p.transform.position;
            Vector3 goal = t.Pos;

            // edge exits: keep walking through the exit
            if (now < _pushUntil)
            {
                DirOf(t.Dir, out h, out v);
                return true;
            }
            float gdx = goal.x - pos.x, gdz = goal.z - pos.z;
            // doors: stop in the middle of the door's area, as soon as the game lets us use the door
            if (t.Kind == Kind.Exit && !t.IsEdge && t.Door != null && InRange(t.Door)
                && (Mathf.Abs(gdx) < 0.35f || now - _stuckSince > 0.6f))
            {
                bool locked = Combat.ScreenLockRunning || !_walkTarget_DoorCanEnter(t) || t.Locked;
                _interactSaid[t.Door.GetInstanceID()] = now;
                if (!locked && Settings.AutoDoor)
                {
                    InteractEntity door = t.Door;
                    Arrive(t, Loc.T("door_auto", t.Name));
                    _autoEnterDoor = door;
                    _autoEnterUntil = now + 1.8f;
                    Log.Write("Tuer automatisch: " + door.name);
                    return false;
                }
                Arrive(t, Loc.T("arrived", t.Label()) + (t.Locked ? ArrivalHint(t) : locked ? " " + Loc.T("door_locked") : ArrivalHint(t) + " " + Loc.T("door_stay")));
                return false;
            }
            // items: arrived when the game offers the pickup, or when standing right on it
            if (t.Kind == Kind.Item)
            {
                InteractEntity ie = t.Obj as InteractEntity;
                if ((ie != null && InRange(ie) && !ie.AutoInteract) || (Mathf.Abs(gdx) < 0.25f && Mathf.Abs(gdz) < 0.15f))
                {
                    Arrive(t, Loc.T("arrived", t.Label()) + ArrivalHint(t));
                    return false;
                }
            }
            else if (!beggar && Mathf.Abs(gdx) < 0.5f && Mathf.Abs(gdz) < 0.45f)
            {
                if (t.Kind == Kind.Exit && t.IsEdge)
                {
                    _pushUntil = now + 2.5f;
                    DirOf(t.Dir, out h, out v);
                    return true;
                }
                Arrive(t, Loc.T("arrived", t.Label()) + ArrivalHint(t));
                return false;
            }
            if (now >= _replanAt || _path.Count == 0)
            {
                _replanAt = now + (beggar ? 0.5f : 1.5f);
                Plan(p, pos, goal);
            }
            if (now >= _traceAt)
            {
                _traceAt = now + 0.5f;
                Log.Write("Hinfuehren: Spieler " + pos.ToString("F2") + " Ziel " + goal.ToString("F2") + " Wegpunkte " + _path.Count + " (" + (_pathIdx + 1) + ")");
            }
            Vector3 w = _path[Mathf.Min(_pathIdx, _path.Count - 1)];
            float dx = w.x - pos.x, dz = w.z - pos.z;
            while (Mathf.Abs(dx) < 0.18f && Mathf.Abs(dz) < 0.13f && _pathIdx < _path.Count - 1)
            {
                _pathIdx++;
                w = _path[_pathIdx];
                dx = w.x - pos.x; dz = w.z - pos.z;
            }
            if (beggar && _pathIdx >= _path.Count - 1) { dx = gdx; dz = gdz; } // walk right into the begging enemy to grab it
            // doors: the path grid keeps a safety margin to walls; for the last bit walk straight into the door area
            bool doorFinal = t.Kind == Kind.Exit && !t.IsEdge && t.Door != null && _pathIdx >= _path.Count - 1 && Mathf.Abs(gdx) < 1.5f && Mathf.Abs(gdz) < 2f;
            if (doorFinal) { dx = gdx; dz = gdz; }
            h = Mathf.Abs(dx) > 0.12f ? (dx > 0 ? 1 : -1) : 0;
            v = Mathf.Abs(dz) > 0.08f ? (dz > 0 ? 1 : -1) : 0;
            // run on long horizontal stretches (the game only runs sideways)
            float gdist = Mathf.Sqrt(gdx * gdx + gdz * gdz);
            WalkRun = Settings.WalkRun && h != 0 && Mathf.Abs(dx) > 1.6f && gdist > 3f && !beggar && t.Kind != Kind.Item;

            // stuck handling
            if (Vector3.Distance(pos, _stuckPos) > 0.25f) { _stuckPos = pos; _stuckSince = now; }
            else
            {
                float st = now - _stuckSince;
                float gd = Mathf.Sqrt(gdx * gdx + gdz * gdz);
                if (st > 1.5f && gd < 2.5f && !beggar)
                {
                    if (t.Kind == Kind.Exit && t.IsEdge) { _pushUntil = now + 2.5f; DirOf(t.Dir, out h, out v); return true; }
                    Arrive(t, Loc.T("arrived_near", t.Label(), GameInfo.Direction(pos, t.RawPos)) + ArrivalHint(t));
                    return false;
                }
                if (st > 4f)
                {
                    Log.Write("Hinfuehren blockiert bei " + pos.ToString("F2") + ", Ziel " + goal.ToString("F2") + ", roh " + t.RawPos.ToString("F2"));
                    if (_failProtocols++ < 3) Geometry.Protocol("Hinfuehren blockiert", false);
                    StopWalk(Loc.T("walk_blocked", GameInfo.Direction(pos, goal)));
                    return false;
                }
                if (st > 1.0f && now >= _replanAt - 1.0f) _replanAt = now; // replan now and then while stuck
            }
            return true;
        }

        private static bool _walkTarget_DoorCanEnter(Target t)
        {
            try { return t.Door == null || t.Door.CanEnter; } catch { return true; }
        }

        private static void DirOf(DoorEnterDirection d, out int h, out int v)
        {
            h = 0; v = 0;
            switch (d)
            {
                case DoorEnterDirection.Left: h = -1; break;
                case DoorEnterDirection.Right: h = 1; break;
                case DoorEnterDirection.Up: v = 1; break;
                case DoorEnterDirection.Down: v = -1; break;
            }
        }

        // ---------------- dead end ----------------
        /// <summary>Called after PlayerInput.Update with the player's own stick input.</summary>
        public static void CheckDeadEnd(RCG.Player p, int h, int v)
        {
            if (!Settings.DeadEnd || AutoWalk || GameInfo.Quiet) { _deTimer = 0; return; }
            float now = Time.realtimeSinceStartup;
            Vector3 pos = p.transform.position;
            if ((h == 0 && v == 0) || h != _deDirH || v != _deDirV)
            {
                _deDirH = h; _deDirV = v; _deTimer = now; _dePos = pos;
                return;
            }
            bool climbing = false;
            try { climbing = p.Climb != null && (p.Climb.IsClimbing || p.Climb.TouchingClimbSurface); } catch { }
            if (p.IsGettingHit || p.InKnockdown || p.IsLying || p.IsBlocking || p.IsDodging || climbing || Mathf.Abs(pos.y - _dePos.y) > 0.15f)
            { _deTimer = now; _dePos = pos; return; }
            float span = now - _deTimer;
            if (span < 0.7f) return;
            bool blockedH = h != 0 && Mathf.Abs(pos.x - _dePos.x) < 0.1f;
            bool blockedV = v != 0 && Mathf.Abs(pos.z - _dePos.z) < 0.06f;
            _deTimer = now; _dePos = pos;
            if (!blockedH && !blockedV) return;
            if (now - _deToneAt > 1.5f)
            {
                _deToneAt = now;
                float pan = blockedH ? h : 0;
                float f = blockedV && !blockedH ? (v > 0 ? 140 : 70) : 90;
                Speech.Tone(f, 160, pan, 0.8f, 3, f, 0);
            }
            if (blockedV && !blockedH) _deVCount++; else _deVCount = 0;
            if (Combat.InFight) return;
            // speech: always for sideways, for up/down only when pushed for about two seconds
            if (now - _deSpeechAt < 8f) return;
            if (!blockedH && _deVCount < 3) return;
            _deSpeechAt = now;
            string dirWord = blockedH ? Loc.T(h > 0 ? "blk_right" : "blk_left") : Loc.T(v > 0 ? "blk_up" : "blk_down");
            if (Combat.ScreenLockRunning) { Speech.Say(Loc.T("blocked_lock"), true); return; }
            BuildTargets();
            Target best = null;
            float bestScore = 9999f;
            foreach (Target t in _targets)
            {
                if (t.Kind != Kind.Exit) continue;
                float score = Vector3.Distance(pos, t.Pos);
                if (t.QuestRoute) score -= 1000f;
                else if (!t.Seen) score -= 50f;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            Log.Write("Sackgasse " + dirWord + " bei " + pos.ToString("F2") + (best != null ? ", Ausgang " + best.Pos.ToString("F2") + " roh " + best.RawPos.ToString("F2") : ""));
            string s = Loc.T("blocked", dirWord);
            if (best != null)
            {
                s += " " + Loc.T("blocked_exit", best.Label(), GameInfo.Direction(pos, best.Pos));
                List<Vector3> way = PathGrid.Find(p, pos, best.Pos);
                if (way != null && way.Count > 1) s += " " + Loc.T("way", PathGrid.Describe(pos, way));
            }
            if (Settings.PadKeys) s += " " + Loc.T("blocked_help");
            Speech.Say(s, true);
        }

        // ---------------- learned room map + quest direction ----------------
        private static string GraphFile { get { return Path.Combine(Mod.DataDir, "raumkarte.txt"); } }

        private static void LoadGraph()
        {
            _graphLoaded = true;
            try
            {
                if (!File.Exists(GraphFile)) return;
                foreach (string line in File.ReadAllLines(GraphFile, Encoding.UTF8))
                {
                    int i = line.IndexOf('>');
                    if (i <= 0) continue;
                    AddEdge(line.Substring(0, i).Trim(), line.Substring(i + 1).Trim());
                }
            }
            catch (Exception e) { Log.Error("LoadGraph", e); }
        }

        private static bool AddEdge(string a, string b)
        {
            a = a.ToLower(); b = b.ToLower();
            if (a.Length == 0 || b.Length == 0) return false;
            HashSet<string> set;
            if (!_graph.TryGetValue(a, out set)) { set = new HashSet<string>(); _graph[a] = set; }
            return set.Add(b);
        }

        private static void LearnGraph()
        {
            if (!_graphLoaded) LoadGraph();
            string cur = GameInfo.SceneName;
            if (string.IsNullOrEmpty(cur)) return;
            bool changed = false;
            foreach (Target t in _targets) if (t.Kind == Kind.Exit) changed |= AddEdge(cur, t.ToScene);
            if (!changed) return;
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, HashSet<string>> kv in _graph)
                    foreach (string b in kv.Value) sb.Append(kv.Key).Append('>').Append(b).Append("\r\n");
                File.WriteAllText(GraphFile, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { Log.Error("SaveGraph", e); }
        }

        private static string _questLogged = "";

        public static List<Data_QuestItem> ActiveQuests()
        {
            List<Data_QuestItem> list = new List<Data_QuestItem>();
            try
            {
                if (!Singleton<QuestManager>.HasInstance()) return list;
                QuestManager qm = Singleton<QuestManager>.instance;
                if (qm == null || qm.QuestInventory == null || qm.QuestInventory.Items == null) return list;
                foreach (Data_Item it in qm.QuestInventory.Items)
                {
                    Data_QuestItem q = it as Data_QuestItem;
                    if (q == null) continue;
                    if (q.QuestState == QuestStates.New || q.QuestState == QuestStates.InProgress) list.Add(q);
                }
                // main quests first
                list.Sort((a, b) => ((int)a.QuestType).CompareTo((int)b.QuestType));
            }
            catch (Exception e) { Log.Error("ActiveQuests", e); }
            return list;
        }

        private static bool SceneMatches(string scene, string locKey)
        {
            if (string.IsNullOrEmpty(locKey) || string.IsNullOrEmpty(scene)) return false;
            if (scene.ToUpper() == locKey.ToUpper()) return true;
            string a = GameInfo.Translate(scene.ToUpper());
            string b = GameInfo.Translate(locKey);
            return !string.IsNullOrEmpty(a) && a != "null" && a == b;
        }

        /// <summary>Rooms with a marker on the phone map (exclamation = mission, question = optional).</summary>
        public static List<string> MarkedScenes(out bool mission)
        {
            List<string> ex = new List<string>(), qu = new List<string>();
            mission = false;
            PhoneHelp.RefreshMapData();
            try
            {
                MapDataManager mdm = MapDataManager.Instance;
                if (mdm == null || mdm.MapData == null) return ex;
                foreach (MapData md in mdm.MapData)
                {
                    if (md == null || md.Icons == null || string.IsNullOrEmpty(md.SceneName)) continue;
                    if (md.SceneName.Contains("Cutscene")) continue;
                    foreach (MapData.IconData ic in md.Icons)
                    {
                        if (ic == null || ic.Icon == null || !ic.ShowingIcon || !ic.ShowRequirementsMet) continue;
                        string n = ic.Icon.name.ToLower();
                        if (n.Contains("exclamation")) { if (!ex.Contains(md.SceneName.ToLower())) ex.Add(md.SceneName.ToLower()); }
                        else if (n.Contains("question")) { if (!qu.Contains(md.SceneName.ToLower())) qu.Add(md.SceneName.ToLower()); }
                    }
                }
            }
            catch (Exception e) { Log.Error("MarkedScenes", e); }
            if (ex.Count > 0) { mission = true; return ex; }
            return qu;
        }

        /// <summary>Next room on the shortest known way from the current room to the goal, or null.</summary>
        private static string NextHop(string cur, string goal)
        {
            Dictionary<string, string> prev = new Dictionary<string, string>();
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(cur);
            prev[cur] = null;
            while (queue.Count > 0)
            {
                string n = queue.Dequeue();
                HashSet<string> next;
                if (!_graph.TryGetValue(n, out next)) continue;
                foreach (string m in next)
                {
                    if (prev.ContainsKey(m)) continue;
                    prev[m] = n;
                    if (m == goal)
                    {
                        string hop = m;
                        while (prev[hop] != null && prev[hop] != cur) hop = prev[hop];
                        return hop;
                    }
                    queue.Enqueue(m);
                }
            }
            return null;
        }

        /// <summary>Rooms marked on the phone map become navigator targets: the target is the exit on the way there.</summary>
        private static void AddRoomTargets()
        {
            bool mission;
            List<string> marks = MarkedScenes(out mission);
            if (marks.Count == 0) return;
            string cur = GameInfo.SceneName.ToLower();
            RCG.Player p = GameInfo.Player;
            Vector3 pp = p != null ? p.transform.position : Vector3.zero;
            List<Target> add = new List<Target>();
            foreach (string m in marks)
            {
                if (m == cur) continue;
                string room = GameInfo.RoomName(m);
                string hop = NextHop(cur, m);
                Target via = null;
                if (hop != null)
                    foreach (Target t in _targets) if (t.Kind == Kind.Exit && t.RoomGoal == null && t.ToScene.ToLower() == hop && !t.Locked) { via = t; break; }
                string name;
                if (via != null) name = hop == m ? Loc.T("room_goal_direct", room, via.Name) : Loc.T("room_goal_via", room, via.Name);
                else
                {
                    // way unknown yet: suggest the nearest exit to a room not visited yet
                    float best = float.MaxValue;
                    foreach (Target t in _targets)
                    {
                        if (t.Kind != Kind.Exit || t.RoomGoal != null || t.Seen || t.Locked) continue;
                        float d = Vector3.Distance(pp, t.Pos);
                        if (d < best) { best = d; via = t; }
                    }
                    if (via == null) continue;
                    name = Loc.T("room_goal_unknown", room, via.Name);
                }
                add.Add(new Target
                {
                    Kind = via.Kind, Obj = via.Obj, Door = via.Door, Trigger = via.Trigger, Dir = via.Dir, IsEdge = via.IsEdge,
                    Seen = via.Seen, QuestRoute = mission, ToScene = via.ToScene, RoomGoal = m, Name = name
                });
            }
            _targets.AddRange(add);
        }

        private static string _markLogged = "";
        public static bool QuestHere;

        private static void MarkQuestRoute()
        {
            QuestHere = false;
            string cur = GameInfo.SceneName.ToLower();
            List<Data_QuestItem> quests = ActiveQuests();
            StringBuilder ql = new StringBuilder();
            foreach (Data_QuestItem q in quests) ql.Append(q.ItemName).Append('@').Append(q.Locattion).Append("; ");
            if (ql.ToString() != _questLogged) { _questLogged = ql.ToString(); Log.Write("Aktive Quests: " + _questLogged); }
            bool mission;
            List<string> marks = MarkedScenes(out mission);
            string ml = string.Join(", ", marks.ToArray());
            if (ml != _markLogged) { _markLogged = ml; Log.Write("Kartenmarkierungen" + (mission ? " (Mission)" : "") + ": " + ml); }

            // targets: marked rooms first, otherwise quest locations that are room names
            Predicate<string> isGoal;
            if (marks.Count > 0) isGoal = delegate (string sc) { return marks.Contains(sc); };
            else
            {
                if (quests.Count == 0) return;
                isGoal = delegate (string sc)
                {
                    foreach (Data_QuestItem q in quests) if (SceneMatches(sc, q.Locattion)) return true;
                    return false;
                };
            }
            if (isGoal(cur)) { QuestHere = true; return; }
            // BFS over the learned room map
            Dictionary<string, string> prev = new Dictionary<string, string>();
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(cur);
            prev[cur] = null;
            string found = null;
            while (queue.Count > 0 && found == null)
            {
                string n = queue.Dequeue();
                HashSet<string> next;
                if (!_graph.TryGetValue(n, out next)) continue;
                foreach (string m in next)
                {
                    if (prev.ContainsKey(m)) continue;
                    prev[m] = n;
                    if (isGoal(m)) { found = m; break; }
                    queue.Enqueue(m);
                }
            }
            if (found == null) return;
            string hop = found;
            while (prev[hop] != null && prev[hop] != cur) hop = prev[hop];
            foreach (Target t in _targets)
                if (t.Kind == Kind.Exit && t.ToScene.ToLower() == hop && !t.Locked) t.QuestRoute = true;
        }

        public static string QuestText()
        {
            List<Data_QuestItem> qs = ActiveQuests();
            bool mission;
            List<string> marks = MarkedScenes(out mission);
            if (qs.Count == 0 && marks.Count == 0) return Loc.T("no_quest");
            List<string> parts = new List<string>();
            if (marks.Count > 0)
            {
                List<string> names = new List<string>();
                foreach (string m in marks) { names.Add(GameInfo.RoomName(m)); if (names.Count >= 4) break; }
                parts.Add(Loc.T("map_marks", string.Join(", ", names.ToArray())));
                if (QuestHere) parts.Add(Loc.T("mark_here"));
            }
            foreach (Data_QuestItem q in qs)
            {
                string name = GameInfo.TranslateOr(q.ItemName, GameInfo.Readable(q.ItemName));
                string loc = string.IsNullOrEmpty(q.Locattion) ? "" : GameInfo.TranslateOr(q.Locattion, GameInfo.Readable(q.Locattion));
                parts.Add(loc.Length > 0 ? Loc.T("quest_at", name, loc) : name);
                if (parts.Count >= 3) break;
            }
            return Loc.T("quests") + ": " + string.Join(". ", parts.ToArray());
        }
    }
}
