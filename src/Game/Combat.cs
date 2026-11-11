using System;
using System.Collections.Generic;
using RCG;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Health tones, money, enemy radar (knocking), enemy announcements, attack warnings, bosses, fight areas.</summary>
    public static class Combat
    {
        // player
        private static int _lastHp = -1;
        private static float _nextPulse;
        private static float _lastMoney = -1;
        private static float _moneyAccum;
        private static float _moneySayAt;
        private static bool _wasDead;

        // radar
        private static float _nextKnock;
        private static readonly HashSet<int> _known = new HashSet<int>();
        private static readonly Dictionary<int, EnemyEnitity> _tracked = new Dictionary<int, EnemyEnitity>();
        private static readonly Dictionary<int, Vector3> _lastPos = new Dictionary<int, Vector3>();
        private static readonly Dictionary<int, string> _lastState = new Dictionary<int, string>();
        private static readonly Dictionary<int, float> _attackWarned = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> _bossStep = new Dictionary<int, int>();
        private static int _visibleCount;
        private static float _lastEnemySpeech;
        private static float _lastAttack;
        private static ScreenLock.State _lockState = ScreenLock.State.Waiting;
        public static int VisibleEnemies { get { return _visibleCount; } }
        public static float NearestEnemyDist = 999f;
        public static float NearestHostileDist = 999f; // like NearestEnemyDist, but without begging enemies
        public static EnemyEnitity Nearest;

        public static void OnSceneLoaded(string scene)
        {
            _known.Clear();
            _tracked.Clear();
            _lastPos.Clear();
            _lastState.Clear();
            _attackWarned.Clear();
            _beggingSaid.Clear();
            _weaponSaid.Clear();
            _visibleCount = 0;
            _lockState = ScreenLock.State.Waiting;
            Nearest = null;
            NearestEnemyDist = 999f;
            NearestHostileDist = 999f;
        }

        public static void Update()
        {
            RCG.Player p = GameInfo.Player;
            if (p == null) { _lastHp = -1; return; }
            UpdateHealth(p);
            UpdateMoney(p);
            if (GameInfo.Quiet) return;
            UpdateEnemies(p);
            UpdateScreenLock();
            UpdateGrab(p);
        }

        // ---------------- health ----------------
        private static void UpdateHealth(RCG.Player p)
        {
            int hp = p.Stamina;
            int max = Mathf.Max(1, p.StartingStamina);
            bool dead = hp <= 0 || p.IsDead;
            if (_lastHp >= 0 && !GameInfo.Quiet && Settings.HealthTones)
            {
                if (hp < _lastHp && Time.realtimeSinceStartup - _hitToneAt > 0.3f)
                {
                    // damage: low squeaky square
                    Speech.Tone(180, 140, 0, 0.8f, 2, 120, 0);
                }
                else if (hp > _lastHp && _lastHp > 0)
                {
                    Speech.Tone(500, 220, 0, 0.7f, 0, 900, 0);
                }
            }
            if (dead && !_wasDead && _lastHp > 0)
            {
                Speech.Tone(400, 600, 0, 0.9f, 1, 100, 0);
                Speech.Say(Loc.T("player_ko"), true);
            }
            _wasDead = dead;
            _lastHp = hp;
            float pct = (float)hp / max;
            if (Settings.HealthTones && !dead && pct < 0.3f && !GameInfo.Quiet && Time.realtimeSinceStartup >= _nextPulse)
            {
                _nextPulse = Time.realtimeSinceStartup + 2.5f;
                Speech.Tone(110, 160, 0, 0.8f, 0, 100, 0);
                Speech.Tone(110, 160, 0, 0.6f, 0, 100, 260);
            }
        }

        public static float Money(RCG.Player p)
        {
            try
            {
                PlayerGlobalInventory inv = PlayerGlobalInventory.instance;
                if (inv == null || inv.PlayerInventories == null) return -1;
                int idx = (int)p.ClassNameToPlayerCharacter;
                if (idx < 0 || idx >= inv.PlayerInventories.Length) return -1;
                return inv.PlayerInventories[idx].Money;
            }
            catch { return -1; }
        }

        private static void UpdateMoney(RCG.Player p)
        {
            float m = Money(p);
            if (m < 0) return;
            if (_lastMoney >= 0 && m > _lastMoney + 0.001f && !GameInfo.Quiet)
            {
                if (Settings.MoneyTones)
                {
                    Speech.Tone(1318, 50, 0, 0.45f, 0, 1318, 0);
                    Speech.Tone(1760, 70, 0, 0.45f, 0, 1760, 55);
                }
                _moneyAccum += m - _lastMoney;
                _moneySayAt = Time.realtimeSinceStartup + 1.2f;
            }
            if (_moneyAccum > 0 && Time.realtimeSinceStartup >= _moneySayAt)
            {
                if (Settings.MoneySpeak) Speech.Queue(Loc.T("money_gained", FormatMoney(_moneyAccum)));
                _moneyAccum = 0;
            }
            _lastMoney = m;
        }

        public static string FormatMoney(float m)
        {
            string s = m.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            if (s.EndsWith(".00")) s = s.Substring(0, s.Length - 3);
            if (Loc.IsGerman) s = s.Replace('.', ',');
            return s;
        }

        // ---------------- enemies ----------------
        public static List<EnemyEnitity> Enemies()
        {
            List<EnemyEnitity> list = new List<EnemyEnitity>();
            try
            {
                CombatEnitityMainManager mgr = CombatEnitityMainManager.GetInstance(false);
                if (mgr == null) return list;
                List<CombatEntity> all = mgr.GetListOfCombatEntity(TeamType.Enemy);
                if (all == null) return list;
                foreach (CombatEntity c in all)
                {
                    EnemyEnitity e = c as EnemyEnitity;
                    if (e == null || !e.isActiveAndEnabled) continue;
                    if (e is NPCEntity) continue;
                    if (e.Stamina <= 0 || e.IsDead) continue;
                    if (e.IsRecruitAssistEntity()) continue;
                    list.Add(e);
                }
            }
            catch (Exception ex) { Log.Error("Enemies", ex); }
            return list;
        }

        public static bool OnScreen(Vector3 pos)
        {
            Camera cam = Camera.main;
            if (cam == null) return true;
            Vector3 v = cam.WorldToViewportPoint(pos);
            return v.z > 0 && v.x >= -0.02f && v.x <= 1.02f && v.y >= -0.1f && v.y <= 1.1f;
        }

        public static float HpPercent(CombatEntity e)
        {
            return Mathf.Clamp01((float)e.Stamina / Mathf.Max(1, e.StartingStamina));
        }

        public static string EnemyName(EnemyEnitity e)
        {
            BossBaseEntity b = e as BossBaseEntity;
            if (b != null && !string.IsNullOrEmpty(b._BossNameOverride)) return b._BossNameOverride;
            string cls = e.ClassName;
            if (string.IsNullOrEmpty(cls)) cls = e.name;
            string key = "enemy_" + cls.ToLower().Replace(" ", "");
            string t = Loc.TryT(key);
            if (t != null) return t;
            if (b != null || e is SubBossBaseEntity) return GameInfo.Readable(cls);
            return Loc.T("enemy");
        }

        private static float Dist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = (a.z - b.z) * 1.3f;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static void UpdateEnemies(RCG.Player p)
        {
            Vector3 pp = p.transform.position;
            float now = Time.realtimeSinceStartup;
            List<EnemyEnitity> list = Enemies();
            EnemyEnitity nearest = null;
            float best = 9999f, bestHostile = 9999f;
            int visible = 0;
            HashSet<int> alive = new HashSet<int>();
            List<EnemyEnitity> newOnes = new List<EnemyEnitity>();
            foreach (EnemyEnitity e in list)
            {
                Vector3 ep = e.transform.position;
                float d = Dist(pp, ep);
                bool vis = OnScreen(ep) || d < 5f;
                int id = e.GetInstanceID();
                alive.Add(id);
                _tracked[id] = e;
                _lastPos[id] = ep;
                if (!vis) continue;
                visible++;
                if (!_known.Contains(id)) { _known.Add(id); newOnes.Add(e); }
                if (d < best) { best = d; nearest = e; }
                if (d < bestHostile && !IsBegging(e)) bestHostile = d;
                CheckAttack(e, pp, ep, d, now);
                CheckBeggingAndWeapon(e, pp, ep, now);
                CheckBoss(e);
            }
            // defeated enemies: tracked but no longer alive
            List<int> gone = new List<int>();
            foreach (KeyValuePair<int, EnemyEnitity> kv in _tracked)
            {
                if (alive.Contains(kv.Key)) continue;
                gone.Add(kv.Key);
                EnemyEnitity e = kv.Value;
                bool defeated = e == null || e.Stamina <= 0 || e.IsDead;
                if (defeated && _known.Contains(kv.Key) && Settings.Radar)
                {
                    Vector3 lp;
                    float pan = _lastPos.TryGetValue(kv.Key, out lp) ? StrongPan(lp.x - pp.x) : 0f;
                    DefeatTriad(pan);
                    if (e != null && (e is BossBaseEntity) && Settings.EnemyAnnounce) Speech.Say(Loc.T("boss_defeated", EnemyName(e)), true);
                }
            }
            foreach (int g in gone) { _tracked.Remove(g); _lastPos.Remove(g); _lastState.Remove(g); }

            Nearest = nearest;
            NearestEnemyDist = best;
            NearestHostileDist = bestHostile;
            // the room sends a new wave about 20 seconds after the last enemy (EnemySpawnController)
            if (list.Count == 0 && _hadEnemies && !ScreenLockRunning && Settings.EnemyAnnounce && now - _waveSaidAt > 30f)
            {
                bool spawner = false;
                try { foreach (EnemySpawnController sc in UnityEngine.Object.FindObjectsOfType<EnemySpawnController>()) if (sc.isActiveAndEnabled && sc._MasterEnableDisable) { spawner = true; break; } } catch { }
                if (spawner) { _waveSaidAt = now; Speech.Say(Loc.T("wave_done"), false); }
            }
            _hadEnemies = list.Count > 0;
            int prevVisible = _visibleCount;
            _visibleCount = visible;

            // speech for new enemies
            if (Settings.EnemyAnnounce && newOnes.Count > 0 && nearest != null)
            {
                EnemyEnitity first = newOnes[0];
                foreach (EnemyEnitity e in newOnes) if (e is BossBaseEntity || e is SubBossBaseEntity) first = e;
                bool boss = first is BossBaseEntity || first is SubBossBaseEntity;
                if (boss || prevVisible == 0 || now - _lastEnemySpeech > 6f)
                {
                    _lastEnemySpeech = now;
                    string where = GameInfo.SideWord(first.transform.position.x - pp.x) + ", " + GameInfo.Meters(Dist(pp, first.transform.position));
                    string s;
                    if (boss) s = Loc.T("boss_appears", EnemyName(first), where);
                    else if (visible > 1 && prevVisible == 0) s = Loc.T("enemies_n", visible, EnemyName(first), where);
                    else s = Loc.T("enemy_new", EnemyName(first), where);
                    Speech.Say(s, false);
                }
            }

            // knocking for the nearest enemy
            if (Settings.Radar && nearest != null && best <= Settings.RadarRange && now >= _nextKnock)
            {
                Vector3 ep = nearest.transform.position;
                float range = Mathf.Max(2f, Settings.RadarRange);
                float t = Mathf.Clamp01((best - 0.6f) / (range - 0.6f));
                float interval = Mathf.Lerp(0.11f, 1.1f, t);
                _nextKnock = now + interval;
                Knock(ep.x - pp.x, ep.z - pp.z, HpPercent(nearest), nearest is BossBaseEntity, WeaponOf(nearest) != null);
            }
        }

        /// <summary>Soft "tock": side = stereo, higher = further up (back), lower = further down (front), quieter = weaker enemy.</summary>
        private static void Knock(float dx, float dz, float hp, bool boss) { Knock(dx, dz, hp, boss, false); }

        private static void Knock(float dx, float dz, float hp, bool boss, bool armed)
        {
            float pitch = 240f;
            if (dz > 0.4f) pitch = 240f * Mathf.Lerp(1.15f, 1.6f, Mathf.Clamp01((dz - 0.4f) / 3f));
            else if (dz < -0.4f) pitch = 240f * Mathf.Lerp(0.85f, 0.6f, Mathf.Clamp01((-dz - 0.4f) / 3f));
            if (boss) pitch *= 0.75f;
            float vol = (0.2f + 0.8f * Mathf.Clamp01(hp)) * Settings.KnockVolume / 100f;
            Speech.RawTone(pitch, 55, StrongPan(dx), vol, armed ? 1 : 0, pitch * 0.7f, 0);
        }

        /// <summary>Clear stereo side: anything more than 30 cm to the side is at least half way to that ear.</summary>
        public static float StrongPan(float dx)
        {
            float a = Mathf.Abs(dx);
            if (a < 0.3f) return dx / 0.3f * 0.25f;
            float v = 0.5f + 0.5f * Mathf.Clamp01((a - 0.3f) / 3.5f);
            return dx > 0 ? v : -v;
        }

        /// <summary>Falling major triad from the enemy's side: enemy defeated.</summary>
        public static void DefeatTriad(float pan)
        {
            Speech.Tone(784, 90, pan, 0.8f, 1, 784, 0);
            Speech.Tone(659, 90, pan, 0.8f, 1, 659, 95);
            Speech.Tone(523, 180, pan, 0.8f, 1, 523, 190);
        }

        private static System.Reflection.PropertyInfo _reqKills = typeof(ScreenLock).GetProperty("RequiredKills", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static System.Reflection.FieldInfo _defeated = typeof(ScreenLock).GetField("m_iDefeatedEnemiesCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        /// <summary>Enemies still to defeat in the locked fight area, -1 if unknown.</summary>
        public static int LockRemaining()
        {
            try
            {
                ScreenLock sl = ScreenLock.Instance;
                if (sl == null || sl.GetState != ScreenLock.State.Running) return -1;
                int req = (int)_reqKills.GetValue(sl, null);
                int done = (int)_defeated.GetValue(sl);
                if (req <= 0) return -1;
                return Mathf.Max(0, req - done);
            }
            catch { return -1; }
        }

        public static void OnEnemyDeathReported(EnemyEnitity e)
        {
            try
            {
                int left = LockRemaining();
                Log.Write("Gegner besiegt im Kampfbereich, noch " + left);
                if (Settings.EnemyAnnounce && left > 0 && left <= 3) Speech.Say(Loc.T("left_n", left), false);
            }
            catch (Exception ex) { Log.Error("OnEnemyDeathReported", ex); }
        }

        /// <summary>Answer to "how many enemies are left?"</summary>
        public static string EnemyCountText()
        {
            RCG.Player p = GameInfo.Player;
            List<EnemyEnitity> en = Enemies();
            int vis = 0;
            foreach (EnemyEnitity e in en) if (OnScreen(e.transform.position)) vis++;
            List<string> parts = new List<string>();
            int lockLeft = LockRemaining();
            if (lockLeft >= 0) parts.Add(Loc.T("count_lock", lockLeft, vis));
            else if (en.Count == 0) parts.Add(Loc.T("count_none"));
            else parts.Add(Loc.T("count_room", en.Count, vis));
            if (p != null && Nearest != null && en.Count > 0)
                parts.Add(Loc.T("count_nearest", EnemyName(Nearest), GameInfo.Direction(p.transform.position, Nearest.transform.position)));
            return string.Join(". ", parts.ToArray());
        }

        private static readonly HashSet<int> _beggingSaid = new HashSet<int>();
        private static readonly Dictionary<int, string> _weaponSaid = new Dictionary<int, string>();
        private static float _weaponSpeechAt;

        public static bool IsBegging(EnemyEnitity e)
        {
            try { return e.Fsm != null && e.Fsm.GetCurrentState().IndexOf("Begging", StringComparison.Ordinal) >= 0; } catch { return false; }
        }

        public static string WeaponOf(CombatEntity e)
        {
            try
            {
                PickupObject po = e.PickupObject;
                if (po == null) return null;
                Weapon w = po as Weapon;
                if (w != null && w.GetWeaponData() != null && !string.IsNullOrEmpty(w.GetWeaponData().ItemName))
                    return GameInfo.TranslateOr(w.GetWeaponData().ItemName, GameInfo.Readable(w.GetWeaponData().ItemName));
                return Pickups.CleanObjName(po.name);
            }
            catch { return null; }
        }

        /// <summary>Begging enemies can be recruited; armed enemies hit harder.</summary>
        private static void CheckBeggingAndWeapon(EnemyEnitity e, Vector3 pp, Vector3 ep, float now)
        {
            int id = e.GetInstanceID();
            if (IsBegging(e))
            {
                if (_beggingSaid.Add(id) && Settings.EnemyAnnounce)
                {
                    float pan = StrongPan(ep.x - pp.x);
                    Speech.Tone(600, 120, pan, 0.6f, 0, 900, 0);
                    Speech.Tone(600, 120, pan, 0.6f, 0, 900, 180);
                    Speech.Say(Loc.T("begging", EnemyName(e), GameInfo.Direction(pp, ep)) + (Settings.PadKeys ? " " + Loc.T("beg_guide") : ""), false);
                    Navigator.FocusBeggar(e);
                }
            }
            else _beggingSaid.Remove(id);

            string w = WeaponOf(e);
            string prev;
            _weaponSaid.TryGetValue(id, out prev);
            if (w == prev) return;
            _weaponSaid[id] = w;
            if (w == null || !Settings.EnemyAnnounce) return;
            if (now - _weaponSpeechAt < 4f) return;
            _weaponSpeechAt = now;
            Speech.Say(Loc.T("armed", EnemyName(e), w, GameInfo.SideWord(ep.x - pp.x)), false);
        }

        private static float _hitToneAt = -10f;
        private static bool _hadEnemies;
        private static float _waveSaidAt = -100f;
        private static CombatEntity _grabbed;
        private static float _recruitHintAt = -10f;

        /// <summary>Grabbing: short tone; a begging enemy in the hands: "now LT to recruit".</summary>
        private static void UpdateGrab(RCG.Player p)
        {
            CombatEntity slave = null;
            try { if (p.Puppet != null && p.Puppet.IsMaster) slave = p.Puppet.Slave; } catch { }
            if (slave == _grabbed) return;
            _grabbed = slave;
            if (slave == null) return;
            Speech.Tone(330, 60, 0, 0.6f, 2, 500, 0);
            EnemyEnitity e = slave as EnemyEnitity;
            if (e != null && e._CanBeRecruted && !(e is BossBaseEntity))
            {
                _recruitHintAt = Time.realtimeSinceStartup;
                Speech.Say(Loc.T("grab_recruit", EnemyName(e)), true);
            }
            else if (e != null && Settings.EnemyAnnounce) Speech.Say(Loc.T("grab", EnemyName(e)), false);
        }

        /// <summary>Result of pressing the recruit button while holding an enemy.</summary>
        public static void OnRecruit(bool ok)
        {
            if (ok)
            {
                Speech.Tone(523, 90, 0, 0.7f, 0, 523, 0);
                Speech.Tone(659, 90, 0, 0.7f, 0, 659, 95);
                Speech.Tone(784, 90, 0, 0.7f, 0, 784, 190);
                Speech.Tone(1046, 200, 0, 0.7f, 0, 1046, 285);
                Speech.Say(Loc.T("recruited"), true);
            }
        }

        public static void OnRecruitButton(RCG.Player p)
        {
            // LT pressed while holding an enemy that cannot be recruited (any more)
            try
            {
                if (p == null || p.PlayerID != 0 || !p.Puppet.IsMaster) return;
                EnemyEnitity e = p.Puppet.Slave as EnemyEnitity;
                if (e == null) return;
                if (!e._CanBeRecruted) Speech.Say(Loc.T("recruit_not_begging"), true);
                else if (p._playerRecruit != null && p._playerRecruit._EnemySummon != null) Speech.Say(Loc.T("recruit_busy"), true);
            }
            catch { }
        }

        /// <summary>Called for every hit on a player, also when the damage help blocks it, so the hit stays audible.</summary>
        public static void OnPlayerHit(RCG.Player p, int damage)
        {
            if (p == null || p.PlayerID != 0 || !Settings.HealthTones) return;
            float now = Time.realtimeSinceStartup;
            if (now - _hitToneAt < 0.15f) return;
            _hitToneAt = now;
            if (Settings.DamageHelp == 2) Speech.Tone(180, 90, 0, 0.6f, 1, 150, 0);   // blocked hit: softer
            else Speech.Tone(180, 140, 0, 0.8f, 2, 120, 0);
        }

        public static void PreviewKnock()
        {
            Knock(-3f, 0, 1f, false);
            float vol = 0.2f + 0.8f * 0.3f;
            Speech.RawTone(240, 55, 0.6f, vol * Settings.KnockVolume / 100f, 0, 170, 400);
        }

        private static void CheckAttack(EnemyEnitity e, Vector3 pp, Vector3 ep, float d, float now)
        {
            if (!Settings.AttackWarn || d > 4.5f) return;
            string st = "";
            try { if (e.Fsm != null) st = e.Fsm.GetCurrentState(); } catch { }
            int id = e.GetInstanceID();
            string prev;
            _lastState.TryGetValue(id, out prev);
            _lastState[id] = st;
            if (st == prev || string.IsNullOrEmpty(st)) return;
            string l = st.ToLower();
            bool attack = (l.Contains("attack") || l.Contains("throw") || l.Contains("swing") || l.Contains("tackle") || l.Contains("charge")) && !l.Contains("getup") && !l.Contains("end");
            if (!attack) return;
            float last;
            if (_attackWarned.TryGetValue(id, out last) && now - last < 0.6f) return;
            _attackWarned[id] = now;
            float pan = StrongPan(ep.x - pp.x);
            Speech.Tone(900, 35, pan, 0.7f, 2, 1500, 0);
            Speech.Tone(900, 35, pan, 0.7f, 2, 1500, 60);
            Log.Verbose("Angriff: " + EnemyName(e) + " " + st);
        }

        private static void CheckBoss(EnemyEnitity e)
        {
            if (!(e is BossBaseEntity) || !Settings.BossHealth) return;
            int id = e.GetInstanceID();
            float pct = HpPercent(e);
            int step = pct > 0.75f ? 4 : pct > 0.5f ? 3 : pct > 0.25f ? 2 : pct > 0.1f ? 1 : 0;
            int prev;
            if (!_bossStep.TryGetValue(id, out prev)) { _bossStep[id] = step; return; }
            if (step < prev)
            {
                _bossStep[id] = step;
                Speech.Say(Loc.T("boss_hp", EnemyName(e), Mathf.RoundToInt(pct * 100)), false);
            }
        }

        private static void UpdateScreenLock()
        {
            try
            {
                ScreenLock sl = ScreenLock.Instance;
                ScreenLock.State s = sl != null ? sl.GetState : ScreenLock.State.Waiting;
                if (s == _lockState) return;
                ScreenLock.State old = _lockState;
                _lockState = s;
                Log.Write("Kampfbereich: " + old + " -> " + s);
                if (s == ScreenLock.State.Running && Settings.EnemyAnnounce)
                {
                    Speech.Tone(300, 120, 0, 0.7f, 2, 300, 0);
                    Speech.Tone(300, 120, 0, 0.7f, 2, 300, 160);
                    Speech.Say(Loc.T("lock_start"), false);
                }
                else if ((s == ScreenLock.State.Cleanup || s == ScreenLock.State.Finished) && old == ScreenLock.State.Running)
                {
                    Speech.Tone(523, 120, 0, 0.7f, 0, 523, 0);
                    Speech.Tone(784, 200, 0, 0.7f, 0, 1046, 130);
                    Speech.Say(Loc.T("lock_end"), false);
                }
            }
            catch (Exception e) { Log.Error("ScreenLock", e); }
        }

        public static bool InFight
        {
            get { return NearestHostileDist < 7f || Time.realtimeSinceStartup - _lastAttack < 3f; }
        }

        public static bool ScreenLockRunning
        {
            get { try { return ScreenLock.Instance != null && ScreenLock.Instance.GetState == ScreenLock.State.Running; } catch { return false; } }
        }

        public static string StatusText()
        {
            RCG.Player p = GameInfo.Player;
            if (p == null) return Loc.T("no_player");
            List<string> parts = new List<string>();
            int hp = p.Stamina, max = Mathf.Max(1, p.StartingStamina);
            parts.Add(Loc.T("st_hp", Mathf.RoundToInt(100f * hp / max), hp, max));
            try
            {
                int lvl = PlayerAttributes.Instance.Players[(int)p.ClassNameToPlayerCharacter].Level;
                parts.Add(Loc.T("st_level", lvl));
            }
            catch { }
            float m = Money(p);
            if (m >= 0) parts.Add(Loc.T("st_money", FormatMoney(m)));
            List<EnemyEnitity> en = Enemies();
            int vis = 0;
            foreach (EnemyEnitity e in en) if (OnScreen(e.transform.position)) vis++;
            if (vis == 0) parts.Add(Loc.T("st_noenemies"));
            else if (Nearest != null) parts.Add(Loc.T("st_enemies", vis, EnemyName(Nearest), GameInfo.Direction(p.transform.position, Nearest.transform.position), Mathf.RoundToInt(HpPercent(Nearest) * 100)));
            if (ScreenLockRunning) parts.Add(Loc.T("st_locked"));
            return string.Join(". ", parts.ToArray());
        }

        public static string EnemyList()
        {
            RCG.Player p = GameInfo.Player;
            if (p == null) return Loc.T("no_player");
            List<EnemyEnitity> en = Enemies();
            Vector3 pp = p.transform.position;
            en.Sort((a, b) => Dist(pp, a.transform.position).CompareTo(Dist(pp, b.transform.position)));
            List<string> parts = new List<string>();
            foreach (EnemyEnitity e in en)
            {
                if (!OnScreen(e.transform.position)) continue;
                parts.Add(EnemyName(e) + " " + GameInfo.Direction(pp, e.transform.position) + ", " + Loc.T("energy", Mathf.RoundToInt(HpPercent(e) * 100)));
                if (parts.Count >= 6) break;
            }
            if (parts.Count == 0) return EnemyCountText();
            return EnemyCountText() + ". " + string.Join(". ", parts.ToArray());
        }

        public static void NotePlayerAttack() { _lastAttack = Time.realtimeSinceStartup; }
    }
}
