using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RCG;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RCGAccess
{
    /// <summary>Helpers to read the game: language, player, quiet states, translations, text cleanup.</summary>
    public static class GameInfo
    {
        public static bool LanguageKnown;
        public static LocalizationLanguages Language = LocalizationLanguages.English;
        public static string SceneName = "";
        private static LocalizationLanguages _lastLang = (LocalizationLanguages)(-1);

        public static void Update()
        {
            try
            {
                GlobalSettings gs = GlobalSettings.instance;
                if (gs != null)
                {
                    Language = gs.SubtitleLanguage;
                    LanguageKnown = true;
                    if (Language != _lastLang)
                    {
                        if ((int)_lastLang >= 0) Log.Write("Spielsprache gewechselt: " + Language);
                        else Log.Write("Spielsprache: " + Language);
                        _lastLang = Language;
                        _transCache.Clear();
                    }
                }
            }
            catch { }
            SceneName = SceneManager.GetActiveScene().name;
        }

        // ---------------- player ----------------
        private static FieldInfo _pmInstance = typeof(PlayerManager).GetField("s_instance", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo _pmPlayers = typeof(PlayerManager).GetField("m_players", BindingFlags.NonPublic | BindingFlags.Instance);

        public static PlayerManager PM
        {
            get { try { return _pmInstance.GetValue(null) as PlayerManager; } catch { return null; } }
        }

        /// <summary>Player one (the blind player), null if not in gameplay.</summary>
        public static RCG.Player Player
        {
            get
            {
                PlayerManager pm = PM;
                if (pm == null) return null;
                try
                {
                    RCG.Player[] ps = _pmPlayers.GetValue(pm) as RCG.Player[];
                    if (ps == null) return null;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        RCG.Player p = ps[i];
                        if (p != null && p.isActiveAndEnabled && p.PlayerID == 0) return p;
                    }
                    for (int i = 0; i < ps.Length; i++) if (ps[i] != null && ps[i].isActiveAndEnabled) return ps[i];
                }
                catch { }
                return null;
            }
        }

        private static UI_SceneTransitioner _trans;
        public static bool Fading
        {
            get
            {
                try
                {
                    if (_trans == null) _trans = UnityEngine.Object.FindObjectOfType<UI_SceneTransitioner>();
                    return _trans != null && _trans.Fading;
                }
                catch { return false; }
            }
        }

        public static bool PortraitPlaying
        {
            get { try { return UI_PortraitDialogSystem.instance != null && UI_PortraitDialogSystem.instance.IsPlaying; } catch { return false; } }
        }

        public static bool PhoneOpen
        {
            get { try { return UI_PhoneManager.instance != null && UI_PhoneManager.instance.Open; } catch { return false; } }
        }

        /// <summary>True when orientation sounds and gameplay announcements must be silent.</summary>
        public static bool Quiet
        {
            get
            {
                if (ModMenu.IsOpen) return true;
                if (GameState.CurrentState != GameStates.Playing) return true;
                if (Time.timeScale < 0.01f) return true;
                if (PortraitPlaying || PhoneOpen) return true;
                if (Fading) return true;
                if (Time.realtimeSinceStartup - UiReader.LastTutorialAt < 1.5f) return true;
                try { if (UI_TutorialSlideIn.Instance != null && UI_TutorialSlideIn.Instance.Open) return true; } catch { }
                RCG.Player p = Player;
                if (p == null) return true;
                if (ControlsLocked(p)) return true;
                if (VideoPlaying) return true;
                return false;
            }
        }

        /// <summary>Scripted sequences take the controls away from the player (PlayerInput.LockInput).</summary>
        public static bool ControlsLocked(RCG.Player p)
        {
            try
            {
                PlayerInput pi = p.CombatInput as PlayerInput;
                return pi != null && pi.LockInput;
            }
            catch { return false; }
        }

        private static float _videoCheckAt;
        private static bool _videoPlaying;

        /// <summary>A movie or video sequence is running (checked twice a second).</summary>
        public static bool VideoPlaying
        {
            get
            {
                float now = Time.realtimeSinceStartup;
                if (now < _videoCheckAt) return _videoPlaying;
                _videoCheckAt = now + 0.5f;
                bool v = false;
                try
                {
                    foreach (wfExternals.MoviePlayer m in UnityEngine.Object.FindObjectsOfType<wfExternals.MoviePlayer>())
                    {
                        try { if (m.isActiveAndEnabled && m.IsPlaying()) { v = true; break; } } catch { }
                    }
                    if (!v)
                        foreach (UnityEngine.Video.VideoPlayer vp in UnityEngine.Object.FindObjectsOfType<UnityEngine.Video.VideoPlayer>())
                            if (vp.isActiveAndEnabled && vp.isPlaying) { v = true; break; }
                }
                catch { }
                if (v != _videoPlaying) Log.Write(v ? "Videosequenz laeuft" : "Videosequenz beendet");
                _videoPlaying = v;
                return v;
            }
        }

        // ---------------- translation ----------------
        private static FieldInfo _locDb = typeof(Localizer).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly Dictionary<string, string> _transCache = new Dictionary<string, string>();

        /// <summary>Translate a localization key without error spam. Returns null when unknown.</summary>
        public static string Translate(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            string r;
            if (_transCache.TryGetValue(key, out r)) return r;
            r = null;
            try
            {
                if (Singleton<Localizer>.HasInstance())
                {
                    Localizer loc = Singleton<Localizer>.instance;
                    Data_LocalizationDatabase db = _locDb.GetValue(loc) as Data_LocalizationDatabase;
                    if (db != null && db.Keys != null)
                    {
                        List<LocalizationKey> keys = db.Keys;
                        for (int i = 0; i < keys.Count; i++)
                        {
                            if (keys[i] != null && keys[i].Key == key)
                            {
                                r = keys[i].GetTranslatedKey(Language);
                                if (string.IsNullOrEmpty(r)) r = keys[i].GetTranslatedKey(LocalizationLanguages.English);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Log.Error("Translate", e); }
            if (r != null) r = Clean(r);
            if (_transCache.Count > 4000) _transCache.Clear();
            _transCache[key] = r;
            return r;
        }

        /// <summary>Translate or fall back to a readable form of the key.</summary>
        public static string TranslateOr(string key, string fallback)
        {
            string t = Translate(key);
            if (!string.IsNullOrEmpty(t) && t != "null") return t;
            return fallback;
        }

        public static string RoomName(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return Loc.T("unknown_room");
            string t = Translate(scene.ToUpper());
            if (!string.IsNullOrEmpty(t) && t != "null") return t;
            // cutscene versions of a room ("World_1_Lobby_Cutscene") carry the name of the room itself
            int ci = scene.IndexOf("_Cutscene", StringComparison.OrdinalIgnoreCase);
            if (ci > 0)
            {
                string b = scene.Substring(0, ci);
                t = Translate(b.ToUpper());
                if (!string.IsNullOrEmpty(t) && t != "null") return t;
                return Readable(b);
            }
            return Readable(scene);
        }

        /// <summary>"Downtown_02" -> "Downtown 02", "SchoolGirl" -> "School Girl".</summary>
        public static string Readable(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("(Clone)", "").Replace('_', ' ').Replace('-', ' ').Trim();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
        }

        // ---------------- text cleanup ----------------
        private static readonly Regex _sprite = new Regex("<sprite[^>]*?(?:=\\s*\"?(\\d+)\"?|name\\s*=\\s*\"?([^\">]+)\"?)[^>]*>", RegexOptions.IgnoreCase);
        private static readonly Regex _tag = new Regex("<[^>]*>");
        private static readonly Regex _space = new Regex("[ \\t]+");

        public static string ButtonName(int idx)
        {
            switch (idx)
            {
                case 0: return "A";
                case 1: return "B";
                case 2: return "X";
                case 3: return "Y";
                case 4: return Loc.T("btn_lt");
                case 5: return Loc.T("btn_rt");
                case 6: return "Start";
                case 7: return Loc.T("btn_up");
                case 8: return Loc.T("btn_down");
                case 9: return Loc.T("btn_left");
                case 10: return Loc.T("btn_right");
                case 11: return Loc.T("btn_ls");
                case 12: return Loc.T("btn_rs");
                default: return Loc.T("btn_n", idx);
            }
        }

        /// <summary>Strip rich text tags, turn button sprites into names, fix all-caps.</summary>
        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = _sprite.Replace(s, delegate (Match m)
            {
                int n;
                if (m.Groups[1].Success && int.TryParse(m.Groups[1].Value, out n)) return " " + ButtonName(n) + " ";
                if (m.Groups[2].Success) return " " + Readable(m.Groups[2].Value) + " ";
                return " ";
            });
            s = s.Replace("<br>", ". ");
            s = _tag.Replace(s, "");
            s = s.Replace("\r", " ").Replace("\n", " ").Replace("\u00A0", " ").Replace("\u200B", "");
            s = _space.Replace(s, " ").Trim();
            s = FixCaps(s);
            return s;
        }

        private static string FixCaps(string s)
        {
            int letters = 0, upper = 0;
            foreach (char c in s) if (char.IsLetter(c)) { letters++; if (char.IsUpper(c)) upper++; }
            if (letters < 4 || upper != letters) return s;
            // all caps: keep short words (abbreviations), lower the rest, capitalise word starts
            string[] words = s.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                int wl = 0;
                foreach (char c in w) if (char.IsLetter(c)) wl++;
                if (wl <= 3) continue;
                words[i] = w.Substring(0, 1) + w.Substring(1).ToLower();
            }
            return string.Join(" ", words);
        }

        public static bool HasLetters(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (char.IsLetter(c)) return true;
            return false;
        }

        // ---------------- visibility ----------------
        public static bool Visible(TMP_Text t)
        {
            if (t == null || !t.isActiveAndEnabled) return false;
            if (t.color.a < 0.05f) return false;
            if (t.alpha < 0.05f) return false;
            try
            {
                if (t is TextMeshProUGUI)
                {
                    CanvasRenderer cr = t.canvasRenderer;
                    if (cr != null && cr.GetAlpha() < 0.05f) return false;
                }
                Transform tr = t.transform;
                while (tr != null)
                {
                    CanvasGroup cg = tr.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        if (cg.alpha < 0.05f) return false;
                        if (cg.ignoreParentGroups) break;
                    }
                    tr = tr.parent;
                }
                if (t.rectTransform.localScale.x < 0.01f || t.rectTransform.localScale.y < 0.01f) return false;
            }
            catch { }
            return true;
        }

        private static readonly Vector3[] _corners = new Vector3[4];

        private static Rect ScreenRect(RectTransform rt)
        {
            Canvas c = rt.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (c != null)
            {
                Canvas root = c.rootCanvas;
                if (root != null && root.renderMode != RenderMode.ScreenSpaceOverlay) cam = root.worldCamera;
                if (root != null && root.renderMode == RenderMode.WorldSpace && cam == null) cam = Camera.main;
            }
            rt.GetWorldCorners(_corners);
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, _corners[i]);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>True when the element lies (mostly) inside the screen and inside every mask above it.</summary>
        public static bool OnScreen(RectTransform rt)
        {
            if (rt == null) return true;
            try
            {
                Rect r = ScreenRect(rt);
                Vector2 c = r.center;
                float mx = Screen.width * 0.02f, my = Screen.height * 0.02f;
                if (c.x < -mx || c.x > Screen.width + mx || c.y < -my || c.y > Screen.height + my) return false;
                Transform p = rt.parent;
                int n = 0;
                while (p != null && n < 30)
                {
                    RectTransform prt = p as RectTransform;
                    if (prt != null)
                    {
                        Mask m = p.GetComponent<Mask>();
                        RectMask2D m2 = p.GetComponent<RectMask2D>();
                        if ((m != null && m.enabled) || (m2 != null && m2.enabled))
                        {
                            Rect mr = ScreenRect(prt);
                            if (mr.width < 2f || mr.height < 2f) return false;
                            if (c.x < mr.xMin - 2 || c.x > mr.xMax + 2 || c.y < mr.yMin - 2 || c.y > mr.yMax + 2) return false;
                        }
                    }
                    p = p.parent;
                    n++;
                }
            }
            catch { }
            return true;
        }

        public static string Path(Transform t)
        {
            if (t == null) return "";
            StringBuilder sb = new StringBuilder(t.name);
            Transform p = t.parent;
            int n = 0;
            while (p != null && n < 8) { sb.Insert(0, p.name + "/"); p = p.parent; n++; }
            return sb.ToString();
        }

        // ---------------- spoken numbers ----------------
        public static string Meters(float m)
        {
            m = Mathf.Abs(m);
            if (m >= 10f) return Loc.T("meters", Mathf.RoundToInt(m).ToString());
            float r = Mathf.Round(m * 2f) / 2f;
            if (r < 0.5f) r = 0.5f;
            string s = r.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            if (Loc.IsGerman) s = s.Replace('.', ',');
            if (r == 1f) return Loc.T("meter1");
            return Loc.T("meters", s);
        }

        /// <summary>"rechts, 5 Meter, 2 Meter nach oben" relative to the player (x = side, z = up/down on screen).</summary>
        public static string Direction(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            List<string> parts = new List<string>();
            if (Mathf.Abs(dx) >= 0.6f) parts.Add(Loc.T(dx > 0 ? "dir_right" : "dir_left") + " " + Meters(dx));
            if (Mathf.Abs(dz) >= 0.6f) parts.Add(Meters(dz) + " " + Loc.T(dz > 0 ? "dir_up" : "dir_down"));
            if (parts.Count == 0) return Loc.T("dir_here");
            return string.Join(", ", parts.ToArray());
        }

        public static string SideWord(float dx)
        {
            if (Mathf.Abs(dx) < 0.6f) return Loc.T("side_center");
            return Loc.T(dx > 0 ? "side_right" : "side_left");
        }

        public static float Pan(float dx, float range)
        {
            return Mathf.Clamp(dx / range, -1f, 1f);
        }
    }
}
