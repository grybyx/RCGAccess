using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RCG;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Boss fights: an own locator sound for the boss (separate from the enemy radar), announcements of the
    /// boss's moves and, for Hibari, of the phases that decide the fight (needle storm, homing needle, stunned on
    /// the floor = attack now).</summary>
    public static class BossHelp
    {
        private static float _nextBeep;
        private static EnemyEnitity _boss;

        /// <summary>The boss currently in the room (null if none). Set by Combat every frame.</summary>
        public static EnemyEnitity Boss { get { return _boss; } }

        public static void SetBoss(EnemyEnitity b) { _boss = b; }

        public static void OnSceneLoaded()
        {
            _boss = null; _sc = null; _scFindAt = 0; _lastStates.Clear();
            _stunned = _bulletHell = _reflector = _ground = false;
            _oobOk = false; _arenaLogged = false; _hibariCol = null; _needleFired = false; _bounceSeenAt = -1f; _flyStart = -1f;
            _needleBeepAt = 0;
        }

        /// <summary>True if the boss is clearly above the player (flying, on a platform): not reachable now.</summary>
        public static bool BossInAir(EnemyEnitity b, Vector3 pp)
        {
            if (b == null) return false;
            if (_sc != null && b is SeamstressEnitity) return !_sc.bStunned && !_ground;
            return b.transform.position.y > pp.y + 1.2f;
        }

        public static bool BossVulnerable(EnemyEnitity b)
        {
            return b != null && _sc != null && b is SeamstressEnitity && _sc.bStunned;
        }

        /// <summary>Called every frame (not in the mod menu).</summary>
        public static void Update()
        {
            try
            {
                if (GameInfo.Quiet) return;
                RCG.Player p = GameInfo.Player;
                if (p == null) return;
                float now = Time.realtimeSinceStartup;
                Vector3 pp = p.transform.position;
                if (_boss != null && (_boss.Stamina <= 0 || _boss.IsDead || !_boss.gameObject.activeInHierarchy)) _boss = null;
                UpdateHibari(pp, now);
                UpdateMoves(now);
                // the boss locator: "dum-dum" from the boss's side, faster when closer
                if (_boss != null && Settings.BossBeacon && now >= _nextBeep)
                {
                    Vector3 bp = _boss.transform.position;
                    float dx = bp.x - pp.x, dz = bp.z - pp.z;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float t = Mathf.Clamp01((d - 0.6f) / 14f);
                    _nextBeep = now + Mathf.Lerp(0.25f, 1.3f, t);
                    Beep(dx, dz, BossInAir(_boss, pp), BossVulnerable(_boss));
                }
            }
            catch (Exception e) { Log.Error("BossHelp", e); }
        }

        /// <summary>Boss locator: low double pulse = not on your line, higher double pulse = on your line,
        /// rising chirp = boss in the air (not reachable), bright fast triple = boss can be hit now.</summary>
        public static void Beep(float dx, float dz, bool air, bool vulnerable) { Beep(dx, dz, air, vulnerable, 0f); }

        public static void Beep(float dx, float dz, bool air, bool vulnerable, float d)
        {
            float pan = Mathf.Clamp(dx / 4f, -1f, 1f);
            float v = Settings.BossVolume / 100f * 0.9f;
            if (vulnerable)
            {
                Speech.RawTone(660, 40, pan, v, 1, 660, d);
                Speech.RawTone(880, 40, pan, v, 1, 880, d + 60);
                return;
            }
            if (air)
            {
                Speech.RawTone(420, 110, pan, v * 0.8f, 2, 760, d);
                return;
            }
            bool line = Mathf.Abs(dz) <= Combat.LineDepth;
            float f = line ? 330f : 150f;
            Speech.RawTone(f, 55, pan, v, 2, f * 0.9f, d);
            Speech.RawTone(f, 55, pan, v, 2, f * 0.9f, d + 90);
        }

        // ---------------- Hibari (SeamstressController) ----------------
        private static SeamstressController _sc;
        private static float _scFindAt;
        private static bool _stunned, _bulletHell, _reflector, _ground;
        private static float _needleBeepAt, _stunLeftSaid;
        private static readonly FieldInfo _fStun = typeof(SeamstressController).GetField("m_ToUse_GroundStundAfterFalling", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _fBhc = typeof(SeamstressController).GetField("m_BulletHellController", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _fBrc = typeof(BulletHellController).GetField("m_BulletReflectiveController", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _fLoc = typeof(SeamstressController).GetField("nCurrentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly HashSet<string> _hintSaid = new HashSet<string>();

        /// <summary>The long text with the explanation only the first time in a session, then the short one.</summary>
        private static string Hint(string key, params object[] args)
        {
            if (_hintSaid.Add(key)) return Loc.T(key, args);
            return Loc.T(key + "_short", args);
        }

        /// <summary>Lower corner opposite to Hibari (the homing needle bounces from there into her).</summary>
        private static string CornerHint(Vector3 pp)
        {
            try
            {
                int loc = _fLoc != null ? (int)_fLoc.GetValue(_sc) : 1;
                Transform bl = _sc.m_transfromFlyTowardsLocation_BottomLeft, br = _sc.m_transfromFlyTowardsLocation_BottomRight;
                Vector3 hp = _sc.transform.position;
                Transform c = loc == 0 ? br : loc == 2 ? bl : (pp.x < hp.x ? bl : br);
                if (c == null) return "";
                Log.Write("Hibari: Ort " + loc + ", Ecke " + c.position.ToString("F2") + ", Spieler " + pp.ToString("F2"));
                return Loc.T(c == br ? "hibari_corner_right" : "hibari_corner_left", GameInfo.Direction(pp, c.position));
            }
            catch { return ""; }
        }

        private static readonly FieldInfo _fShooting = typeof(BulletReflectiveController).GetField("m_IsShooting", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void UpdateHibari(Vector3 pp, float now)
        {
            if (now >= _scFindAt)
            {
                _scFindAt = now + 2f;
                _sc = UnityEngine.Object.FindObjectOfType<SeamstressController>();
            }
            if (_sc == null || !_sc.isActiveAndEnabled) return;
            string name = "Hibari";
            bool say = Settings.BossMoves;

            bool bh = _sc._IsDoingAttack_BulletHell;
            if (bh && !_bulletHell) { Log.Write("Hibari: Nadelhagel"); if (say) Speech.Say(Hint("hibari_bullethell", name), true); }
            _bulletHell = bh;

            bool rf = _sc.m_AI_FireReflector;
            if (rf && !_reflector) { Log.Write("Hibari: Zielnadel"); { _aimStart = now; _needleFired = false; _spotSaidAt = -10f; _spotAnnounced = false; NeedleSpot(pp, now, true); } }
            _reflector = rf;

            bool gr = _sc._InDamageGroundPhase;
            if (gr && !_ground) { Log.Write("Hibari: von der Nadel getroffen, stuerzt"); if (say) Speech.Say(Loc.T("hibari_falls", name), true); }
            _ground = gr;

            bool st = _sc.bStunned;
            if (st && !_stunned)
            {
                Log.Write("Hibari: benommen am Boden");
                _stunLeftSaid = 99f;
                Speech.Tone(Snd.Boss, 523, 70, 0, 0.8f, 1, 523, 0);
                Speech.Tone(Snd.Boss, 659, 70, 0, 0.8f, 1, 659, 80);
                Speech.Tone(Snd.Boss, 784, 120, 0, 0.8f, 1, 784, 160);
                if (Settings.BossAssist && _fStun != null)
                {
                    try { float left0 = (float)_fStun.GetValue(_sc); _fStun.SetValue(_sc, left0 + 6f); Log.Write("Hibari: Erleichterung, benommen " + left0.ToString("F1") + " + 6 s"); } catch { }
                }
                if (say) Speech.Say(Hint("hibari_stunned", name) + " " + GameInfo.Direction(pp, _sc.transform.position) + "." + (Settings.LsWalk ? " " + Loc.T("boss_l3") : ""), true);
            }
            if (st)
            {
                float left = 0f;
                try { left = (float)_fStun.GetValue(_sc); } catch { }
                if (left > 0f && left <= 3f && _stunLeftSaid > 3f) { _stunLeftSaid = left; if (say) Speech.Say(Loc.T("hibari_stun_left", 3), true); }
            }
            if (!st && _stunned) { Log.Write("Hibari: wieder auf"); if (say) Speech.Say(Loc.T("hibari_up", name), true); }
            _stunned = st;

            // aiming phase: guide to a spot from which the needle bounces into Hibari
            if (_reflector && !_needleFired) NeedleSpot(pp, now, false);
            // the homing needle: high ticks from its position, faster when it is close
            try
            {
                BulletHellController bhc = _fBhc != null ? _fBhc.GetValue(_sc) as BulletHellController : null;
                BulletReflectiveController brc = bhc != null && _fBrc != null ? _fBrc.GetValue(bhc) as BulletReflectiveController : null;
                if (brc != null && brc.gameObject.activeInHierarchy)
                {
                    bool shooting = _fShooting != null && (bool)_fShooting.GetValue(brc);
                    if (shooting && !_needleFired && _reflector)
                    {
                        _needleFired = true;
                        if (Settings.BossAssist)
                        {
                            Log.Write("Hibari: Nadel abgeschossen, Spieler " + pp.ToString("F2") + ", Erleichterung an");
                            if (Settings.BossMoves) Speech.Say(Loc.T("needle_go_good"), true);
                        }
                        else
                        {
                            bool hit = SimNeedle(brc.transform.position, pp + new Vector3(0f, 1f, -0.1f), now, true);
                            Log.Write("Hibari: Nadel abgeschossen, Spieler " + pp.ToString("F2") + ", Hibari " + _sc.transform.position.ToString("F2") + ", Vorhersage " + (hit ? "Treffer" : "kein Treffer"));
                            if (Settings.BossMoves) Speech.Say(Loc.T(hit ? "needle_go_good" : "needle_go"), true);
                        }
                    }
                    if (Settings.BossAssist) AssistNeedle(brc, shooting, now);
                    if (brc.gameObject.activeInHierarchy && now >= _needleBeepAt && Settings.BossBeacon)
                    {
                        Vector3 np = brc.transform.position;
                        float dx = np.x - pp.x, dy = np.y - (pp.y + 1f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        _needleBeepAt = now + Mathf.Lerp(0.08f, 0.4f, Mathf.Clamp01(d / 10f));
                        // bounced once = flying back toward Hibari: lower tick
                        float f = brc.HadBounceOnce() ? 950f : 1600f;
                        if (!shooting) f = 1200f; // aiming, not yet flying
                        f *= dy > 0.5f ? 1.15f : dy < -0.5f ? 0.87f : 1f;
                        Speech.RawTone(f, 22, Mathf.Clamp(dx / 4f, -1f, 1f), 0.55f * Settings.BossVolume / 100f, 1, f, 0);
                    }
                }
                else _bounceSeenAt = -1f;
            }
            catch (Exception e) { Log.Error("Hibari-Nadel", e); }
        }

        // ---------------- boss assist: the homing needle always hits ----------------
        private static float _bounceSeenAt = -1f, _flyStart = -1f;
        private static readonly FieldInfo _fArrow = typeof(BulletReflectiveController).GetField("m_Collider_Arrow", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Boss assist: shortly after its first bounce the needle hits Hibari, as if it had flown into her
        /// (same steps as the game's own hit: needle off, hit sound, Hibari falls).</summary>
        private static void AssistNeedle(BulletReflectiveController brc, bool shooting, float now)
        {
            if (!shooting) { _bounceSeenAt = -1f; _flyStart = -1f; return; }
            if (_flyStart < 0f) _flyStart = now;
            // the needle hits the player first if it flies into them (the game then pulls it back without a bounce):
            // hit Hibari already when it comes close to the player, after a bounce, or after 1.2 s of flight
            RCG.Player pl = GameInfo.Player;
            float toPlayer = pl != null ? Vector3.Distance(brc.transform.position, pl.transform.position + Vector3.up) : 99f;
            bool bounced = brc.HadBounceOnce();
            if (bounced && _bounceSeenAt < 0f) _bounceSeenAt = now;
            if (!(toPlayer < 1.6f || (bounced && now - _bounceSeenAt >= 0.3f) || now - _flyStart > 1.2f)) return;
            Log.Write("Hibari: Erleichterung ausgeloest (" + (toPlayer < 1.6f ? "Nadel nah am Spieler" : bounced ? "nach Abprall" : "Flugzeit") + ")");
            _bounceSeenAt = -1f; _flyStart = -1f;
            try { if (!string.IsNullOrEmpty(brc.sfx_Needle_Hit_Boss)) RCGAudio.instance.PlayOneShot(brc.sfx_Needle_Hit_Boss, brc.transform.position); } catch { }
            if (_fShooting != null) _fShooting.SetValue(brc, false);
            Collider arrow = _fArrow != null ? _fArrow.GetValue(brc) as Collider : null;
            if (arrow != null) arrow.enabled = false;
            brc.gameObject.SetActive(false);
            _sc.HitByBulletReflect_OnCall();
            Log.Write("Hibari: Erleichterung, Nadel trifft Hibari");
        }

        // ---------------- homing needle: where to stand ----------------
        private static float _aimStart, _spotSaidAt, _spotToneAt, _spotCalcAt;
        private static bool _needleFired, _spotAnnounced;
        private static float _spotX = float.NaN, _spotZ;
        // the needle's walls: BulletReflectiveController.Update_WallCollisionTest bounces it at x -10/10 and y 0/8.6;
        // the "...-Bullet_OutOfBound" trigger colliders (if the room has them) may be closer
        private static float _oobL = -10f, _oobR = 10f, _oobF = 0f, _oobC = 8.6f;
        private static bool _oobOk, _arenaLogged;
        private static Collider _hibariCol;
        private static readonly FieldInfo _fBlocker = typeof(SeamstressController).GetField("m_gameObject_ProjectileReflectionCollider_Blocker", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _fShield = typeof(SeamstressController).GetField("mShieldTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _fWait = typeof(SeamstressController).GetField("mHowLongToWaitBeforeFiringNeedleHunter", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>World bounds of a collider, also when it is switched off (Unity then reports size 0).</summary>
        private static bool ColBounds(Collider c, out Bounds b)
        {
            b = new Bounds();
            if (c == null) return false;
            if (c.enabled && c.gameObject.activeInHierarchy && c.bounds.size.sqrMagnitude > 0.0001f) { b = c.bounds; return true; }
            Transform t = c.transform;
            BoxCollider bc = c as BoxCollider;
            if (bc != null) { b = new Bounds(t.TransformPoint(bc.center), Abs(Vector3.Scale(bc.size, t.lossyScale))); return true; }
            SphereCollider sc = c as SphereCollider;
            if (sc != null) { float r = sc.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y)); b = new Bounds(t.TransformPoint(sc.center), new Vector3(r * 2, r * 2, r * 2)); return true; }
            CapsuleCollider cc = c as CapsuleCollider;
            if (cc != null) { float r = cc.radius * Mathf.Abs(t.lossyScale.x); float h = Mathf.Max(cc.height * Mathf.Abs(t.lossyScale.y), r * 2); b = new Bounds(t.TransformPoint(cc.center), new Vector3(r * 2, h, r * 2)); return true; }
            return false;
        }

        private static Vector3 Abs(Vector3 v) { return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)); }

        /// <summary>Arena walls for the needle and Hibari's needle collider.</summary>
        private static void FindArena()
        {
            if (_oobOk) return;
            _oobOk = true;
            _oobL = -10f; _oobR = 10f; _oobF = 0f; _oobC = 8.6f;
            List<string> found = new List<string>();
            try
            {
                foreach (Collider col in Resources.FindObjectsOfTypeAll<Collider>())
                {
                    if (col == null || !col.gameObject.scene.IsValid()) continue;
                    string n = col.name.ToLowerInvariant();
                    if (n.IndexOf("outofbound", StringComparison.Ordinal) < 0) continue;
                    Bounds cb;
                    if (!ColBounds(col, out cb)) continue;
                    found.Add(col.name);
                    if (n.StartsWith("left")) _oobL = Mathf.Max(_oobL, cb.max.x);
                    else if (n.StartsWith("right")) _oobR = Mathf.Min(_oobR, cb.min.x);
                    else if (n.StartsWith("floor")) _oobF = Mathf.Max(_oobF, cb.max.y);
                    else if (n.StartsWith("ceiling")) _oobC = Mathf.Min(_oobC, cb.min.y);
                }
                foreach (Collider col in _sc.GetComponentsInChildren<Collider>(true))
                    if (col.name == "ProjectileReflectionCollider") { _hibariCol = col; break; }
            }
            catch (Exception e) { Log.Error("FindArena", e); }
            if (!_arenaLogged)
            {
                _arenaLogged = true;
                Bounds hb = HibariBox();
                Log.Write("Hibari-Arena: links " + _oobL.ToString("F2") + " rechts " + _oobR.ToString("F2") + " Boden " + _oobF.ToString("F2") + " Decke " + _oobC.ToString("F2")
                    + ", Kollider gefunden: " + (found.Count > 0 ? string.Join(", ", found.ToArray()) : "keine (Spielgrenzen)")
                    + ", Nadel-Kollider " + (_hibariCol != null ? "" : "fehlt, ") + hb.center.ToString("F2") + " Groesse " + hb.size.ToString("F2"));
            }
        }

        /// <summary>Where the needle hits Hibari: her needle collider, or a box around her if it cannot be measured.</summary>
        private static Bounds HibariBox()
        {
            Bounds b;
            if (!ColBounds(_hibariCol, out b) || b.size.x < 0.05f || b.size.y < 0.05f)
                b = new Bounds(_sc.transform.position + new Vector3(0f, 1.2f, 0f), new Vector3(1.2f, 2f, 1f));
            b.Expand(0.3f); // the needle's own size
            return b;
        }

        private static bool ShieldBox(out Bounds b)
        {
            b = new Bounds();
            GameObject g = _fBlocker != null ? _fBlocker.GetValue(_sc) as GameObject : null;
            if (g == null || !g.activeInHierarchy) return false;
            bool any = false;
            foreach (Collider c in g.GetComponentsInChildren<Collider>())
            {
                Bounds cb;
                if (!ColBounds(c, out cb)) continue;
                if (!any) { b = cb; any = true; } else b.Encapsulate(cb);
            }
            return any;
        }

        /// <summary>Simulates the homing needle in 3D (speed 10, it bounces only at the side walls, floor and ceiling,
        /// at most 4 times, and keeps drifting in depth): true if it hits Hibari after at least one bounce. The shield
        /// in front of Hibari reflects it until the shield runs out (aim start + shield time).</summary>
        private static bool SimNeedle(Vector3 start, Vector3 target, float fireAt, bool log)
        {
            FindArena();
            Bounds hb = HibariBox();
            Bounds sb;
            bool shield = ShieldBox(out sb);
            float shieldEnd = _aimStart + (_fShield != null ? (float)_fShield.GetValue(_sc) : 6f);
            Vector3 p = start;
            Vector3 d = target - start;
            if (d.sqrMagnitude < 0.0001f) return false;
            d.Normalize();
            int bounces = 0;
            float travelled = 0f;
            for (int step = 0; step < 12; step++)
            {
                float tx = d.x > 0 ? (_oobR - p.x) / d.x : d.x < 0 ? (_oobL - p.x) / d.x : float.MaxValue;
                float ty = d.y > 0 ? (_oobC - p.y) / d.y : d.y < 0 ? (_oobF - p.y) / d.y : float.MaxValue;
                float tw = Mathf.Max(0f, Mathf.Min(tx, ty));
                float th; int axis;
                if (bounces > 0 && RayBox(p, d, hb, out th, out axis) && th <= tw)
                {
                    float at = fireAt + (travelled + th) / 10f;
                    if (!(shield && at < shieldEnd)) return true;
                    float ts; int sAxis;
                    if (RayBox(p, d, sb, out ts, out sAxis) && ts <= th + 0.01f)
                    {
                        p += d * ts; travelled += ts;
                        if (sAxis == 0) d.x = -d.x; else d.y = -d.y;
                        if (++bounces > 4) return false;
                        continue;
                    }
                    return true;
                }
                p += d * tw; travelled += tw;
                if (bounces >= 4) return false;
                if (tx <= ty) d.x = -d.x; else d.y = -d.y;
                bounces++;
            }
            return false;
        }

        /// <summary>Ray against a box; axis = side it enters through (0 x, 1 y, 2 z).</summary>
        private static bool RayBox(Vector3 p, Vector3 d, Bounds b, out float t, out int axis)
        {
            t = 0f; axis = 0;
            float tmin = float.MinValue, tmax = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                float pi = p[i], di = d[i], mn = b.min[i], mx = b.max[i];
                if (Mathf.Abs(di) < 1e-6f) { if (pi < mn || pi > mx) return false; continue; }
                float t1 = (mn - pi) / di, t2 = (mx - pi) / di;
                if (t1 > t2) { float tt = t1; t1 = t2; t2 = tt; }
                if (t1 > tmin) { tmin = t1; axis = i; }
                tmax = Mathf.Min(tmax, t2);
            }
            if (tmax < Mathf.Max(0f, tmin)) return false;
            t = Mathf.Max(0f, tmin);
            return true;
        }

        /// <summary>While Hibari aims: finds the spot nearest to the player (side and depth) from which the needle
        /// would bounce into her, says it once and guides there with a tone (steady high tone = stay here).</summary>
        private static void NeedleSpot(Vector3 pp, float now, bool first)
        {
            try
            {
                if (Settings.BossAssist)
                {
                    if (!_spotAnnounced && Settings.BossMoves)
                    {
                        _spotAnnounced = true;
                        Log.Write("Hibari: Zielnadel, Erleichterung an");
                        Speech.Say(Loc.T("hibari_reflector_short") + " " + Loc.T("needle_assist"), true);
                    }
                    return;
                }
                if (now >= _spotCalcAt || first)
                {
                    _spotCalcAt = now + 0.25f;
                    BulletHellController bhc = _fBhc != null ? _fBhc.GetValue(_sc) as BulletHellController : null;
                    BulletReflectiveController brc0 = bhc != null && _fBrc != null ? _fBrc.GetValue(bhc) as BulletReflectiveController : null;
                    Vector3 start = brc0 != null && brc0.gameObject.activeInHierarchy ? brc0.transform.position
                        : bhc != null && bhc.m_FireLocation_NeedleHunter != null ? bhc.m_FireLocation_NeedleHunter.position : _sc.transform.position;
                    float fireAt = _aimStart + (_fWait != null ? (float)_fWait.GetValue(_sc) : 3f) + 0.8f;
                    _spotX = float.NaN;
                    FindArena();
                    // depths to try: where you are, Hibari's depth and in between
                    float hz = HibariBox().center.z;
                    List<float> zs = new List<float> { pp.z };
                    if (Mathf.Abs(hz - pp.z) > 0.2f) { zs.Add(hz); zs.Add((hz + pp.z) / 2f); }
                    float best = float.MaxValue;
                    foreach (float z in zs)
                    {
                        if (!PathGrid.HasFloor(new Vector3(pp.x, pp.y, z))) continue;
                        for (float x = _oobL + 0.6f; x <= _oobR - 0.6f; x += 0.25f)
                        {
                            if (!SimNeedle(start, new Vector3(x, pp.y + 1f, z - 0.1f), fireAt, false)) continue;
                            float cost = Mathf.Abs(x - pp.x) + 1.5f * Mathf.Abs(z - pp.z);
                            if (cost < best) { best = cost; _spotX = x; _spotZ = z; }
                        }
                    }
                }
                if (!_spotAnnounced && Settings.BossMoves)
                {
                    _spotAnnounced = true;
                    bool here = !float.IsNaN(_spotX) && Mathf.Abs(_spotX - pp.x) < 0.4f && Mathf.Abs(_spotZ - pp.z) < 0.25f;
                    string where = float.IsNaN(_spotX) ? Loc.T("needle_nospot") : here ? Loc.T("needle_spot_here")
                        : Loc.T("needle_spot", GameInfo.Direction(pp, new Vector3(_spotX, pp.y, _spotZ)));
                    Log.Write("Hibari: Zielnadel, gute Stelle " + (float.IsNaN(_spotX) ? "keine" : _spotX.ToString("F2") + "/" + _spotZ.ToString("F2")) + ", Spieler " + pp.ToString("F2") + ", Hibari " + _sc.transform.position.ToString("F2"));
                    Speech.Say(Hint("hibari_reflector", "Hibari") + " " + where, true);
                }
                if (float.IsNaN(_spotX) || !Settings.BossBeacon || now < _spotToneAt) return;
                float dx = _spotX - pp.x;
                float off = pp.z - _spotZ; // > 0: you are above the spot
                float v = Settings.BossVolume / 100f * 0.7f;
                if (Mathf.Abs(dx) < 0.4f && Mathf.Abs(off) < 0.25f)
                {
                    _spotToneAt = now + 0.3f;
                    Speech.RawTone(988, 90, 0, v, 0, 988, 0);
                }
                else
                {
                    _spotToneAt = now + Mathf.Lerp(0.2f, 0.6f, Mathf.Clamp01(Mathf.Abs(dx) / 8f));
                    float f = 587f;
                    if (off > 0.25f) f *= Mathf.Lerp(1.15f, 1.5f, Mathf.Clamp01(off / 2f));
                    else if (off < -0.25f) f *= Mathf.Lerp(0.87f, 0.65f, Mathf.Clamp01(-off / 2f));
                    Speech.RawTone(f, 60, Mathf.Abs(dx) < 0.4f ? 0f : Mathf.Clamp(dx / 3f, -1f, 1f), v, 0, f, 0);
                }
            }
            catch (Exception e) { Log.Error("NeedleSpot", e); }
        }

        /// <summary>Postfix of SeamstressController.DoNeedleCycloneAttack.</summary>
        public static void OnCyclone()
        {
            Log.Write("Hibari: Nadelwirbel");
            if (Settings.BossMoves) Speech.Say(Loc.T("hibari_cyclone", "Hibari"), true);
        }

        // ---------------- generic boss moves (other bosses) ----------------
        private static readonly Dictionary<int, string> _lastStates = new Dictionary<int, string>();
        private static float _moveSaidAt;

        private static readonly string[][] Words =
        {
            new[] { "attack", "Angriff" }, new[] { "charge", "Ansturm" }, new[] { "dash", "Sprint" }, new[] { "rush", "Ansturm" },
            new[] { "jump", "Sprung" }, new[] { "throw", "Wurf" }, new[] { "grab", "Griff" }, new[] { "slam", "Stampfer" },
            new[] { "stomp", "Stampfer" }, new[] { "kick", "Tritt" }, new[] { "punch", "Schlag" }, new[] { "shoot", "Schuss" },
            new[] { "fire", "Feuer" }, new[] { "spin", "Wirbel" }, new[] { "combo", "Kombo" }, new[] { "summon", "ruft Hilfe" },
            new[] { "call", "ruft" }, new[] { "rock", "Felsbrocken" }, new[] { "pillar", "Säule" }, new[] { "shockwave", "Schockwelle" },
            new[] { "projectile", "Geschoss" }, new[] { "uppercut", "Aufwärtshaken" }, new[] { "headbutt", "Kopfstoß" },
            new[] { "tackle", "Rempler" }, new[] { "swing", "Schwung" }, new[] { "beam", "Strahl" }, new[] { "laser", "Laser" },
            new[] { "bomb", "Bombe" }, new[] { "dive", "Sturzflug" }, new[] { "air", "aus der Luft" }, new[] { "block", "Abwehr" },
            new[] { "taunt", "verhöhnt" }, new[] { "riff", "Gitarrenriff" }, new[] { "portal", "Portal" }, new[] { "orb", "Kugel" },
        };

        private static readonly string[] MoveWords = { "attack", "charge", "dash", "rush", "throw", "grab", "slam", "stomp", "kick", "punch", "shoot", "fire", "spin", "combo", "summon", "rock", "pillar", "shockwave", "projectile", "uppercut", "headbutt", "tackle", "swing", "beam", "laser", "bomb", "dive", "riff", "portal", "orb" };

        private static void UpdateMoves(float now)
        {
            EnemyEnitity b = _boss;
            if (b == null || b.Fsm == null) return;
            string st;
            try { st = b.Fsm.GetCurrentState(); } catch { return; }
            if (string.IsNullOrEmpty(st)) return;
            int id = b.GetInstanceID();
            string prev;
            _lastStates.TryGetValue(id, out prev);
            if (st == prev) return;
            _lastStates[id] = st;
            Log.Write("Boss-Zustand " + Combat.EnemyName(b) + ": " + st);
            if (b is SeamstressEnitity || !Settings.BossMoves) return; // Hibari: own announcements above
            string l = st.ToLowerInvariant();
            if (l.Contains("getup") || l.Contains("recover") || l.Contains("end") || l.Contains("land") || l.Contains("gethit") || l.Contains("idle") || l.Contains("walk")) return;
            bool move = false;
            foreach (string w in MoveWords) if (l.Contains(w)) { move = true; break; }
            if (!move || now - _moveSaidAt < 1.2f) return;
            _moveSaidAt = now;
            Speech.Say(MoveName(st), true);
        }

        /// <summary>"YamadaJumpAttack" → "Sprung Angriff"; unknown words stay as they are.</summary>
        public static string MoveName(string state)
        {
            string s = state;
            int dot = s.LastIndexOf('.');
            if (dot >= 0) s = s.Substring(dot + 1);
            foreach (string pre in new[] { "AI_", "Yamada", "Abobo", "Misuzu", "NOIZE_", "NOIZE", "Sabuko", "HasabeAndMami", "Hasabe", "Mami", "Seamstress", "Boss", "Enemy" })
                if (s.StartsWith(pre, StringComparison.OrdinalIgnoreCase)) s = s.Substring(pre.Length);
            // split CamelCase and underscores
            List<string> parts = new List<string>();
            StringBuilder cur = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '_' || c == ' ') { if (cur.Length > 0) { parts.Add(cur.ToString()); cur.Length = 0; } continue; }
                if (char.IsUpper(c) && cur.Length > 0 && !char.IsUpper(cur[cur.Length - 1])) { parts.Add(cur.ToString()); cur.Length = 0; }
                cur.Append(c);
            }
            if (cur.Length > 0) parts.Add(cur.ToString());
            List<string> outp = new List<string>();
            foreach (string w in parts)
            {
                string lw = w.ToLowerInvariant();
                string tr = null;
                if (Loc.IsGerman)
                    foreach (string[] pair in Words) if (lw == pair[0]) { tr = pair[1]; break; }
                outp.Add(tr ?? lw);
            }
            string res = string.Join(" ", outp.ToArray());
            return res.Length == 0 ? state : char.ToUpper(res[0]) + res.Substring(1);
        }
    }
}
