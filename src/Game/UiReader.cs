using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RCG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RCGAccess
{
    /// <summary>Menus (hovered entries), popups, tutorials, dialogs and the text log.</summary>
    public static class UiReader
    {
        // ---------- focus (hovered menu entry) ----------
        private static Component _pendingFocus;
        private static string _pendingSpecific;
        private static int _pendingFrame;
        private static Component _focus;
        private static string _focusText = "";
        private static float _focusTime;
        private static string _lastFocusSpoken = "";
        private static float _lastFocusSpokenTime;
        private static float _nextFocusCheck;

        // ---------- text watcher ----------
        private class TextState
        {
            public string Text = "";
            public bool Visible;
            public int ContainerKind = -1; // -1 unknown, 0 none
            public Component Container;
            public bool InSelectable;
            public UI_InputSelectable Selectable;
            public bool Excluded;
            public float[] Changes = new float[6];
            public int ChangeIdx;
            public float MutedUntil;
            public string Logged = "";
            public string Candidate;
            public Component Gate;
        }

        // ---------------- gates: windows that stay active while hidden ----------------
        private static FieldInfo _ipShowing = typeof(UI_ItemPickup).GetField("_showingMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _luOpen = typeof(UI_LevelUpIndicator).GetField("_indicatorOpen", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Component FindGate(Transform tr)
        {
            while (tr != null)
            {
                Component c = tr.GetComponent<UI_PhoneDisplayManager>();
                if (c == null) c = tr.GetComponent<UI_ItemPickup>();
                if (c == null) c = tr.GetComponent<UI_LevelUpIndicator>();
                if (c == null) c = tr.GetComponent<UI_PhoneManager>();
                if (c != null) return c;
                tr = tr.parent;
            }
            return null;
        }

        /// <summary>False when the window is only kept active in the background (closed phone, hidden pop-up).</summary>
        public static bool GateOpen(Component g)
        {
            if (g == null) return true;
            try
            {
                if (g is UI_PhoneDisplayManager)
                {
                    if (UI_PhoneManager.instance == null || !UI_PhoneManager.instance.Open) return false;
                    return ((UI_PhoneDisplayManager)g).PlayerNumber == 0;
                }
                if (g is UI_PhoneManager) return ((UI_PhoneManager)g).Open;
                if (g is UI_ItemPickup) return _ipShowing == null || (bool)_ipShowing.GetValue(g);
                if (g is UI_LevelUpIndicator) return _luOpen == null || (bool)_luOpen.GetValue(g);
            }
            catch { }
            return true;
        }

        private static readonly Dictionary<int, Component> _selGate = new Dictionary<int, Component>();

        private static bool SelectableShown(Component sel)
        {
            if (sel == null) return false;
            int id = sel.GetInstanceID();
            Component g;
            if (!_selGate.TryGetValue(id, out g)) { g = FindGate(sel.transform); _selGate[id] = g; }
            return GateOpen(g);
        }

        private static readonly Dictionary<int, TextState> _texts = new Dictionary<int, TextState>();
        private static float _nextPoll;
        private static readonly Dictionary<string, float> _recentSpoken = new Dictionary<string, float>();
        private static bool _portraitHintGiven;
        private static DialogMessage _lastDialog;
        private static string _lastPortraitLine = "";

        // Container kinds
        private const int K_None = 0, K_Popup = 1, K_Menu = 2, K_Ignore = 3, K_Tutorial = 4;

        private static readonly Type[] PopupTypes = new Type[]
        {
            typeof(UI_TutorialSlideIn), typeof(UI_LevelUpIndicator), typeof(UI_BossFightAnnouncer),
            typeof(UI_IncomingAlert), typeof(UI_PopupAYS), typeof(UI_Confirm), typeof(UI_CombatInstruction),
            typeof(UI_TextMessageManager), typeof(UI_CorruptSave), typeof(UI_BattleIntroSequence), typeof(UI_OutfitEquipDialog),
            typeof(UI_HoldSkipSystem), typeof(UI_QuestItemDisplayV2),
            typeof(UI_Saving), typeof(UI_StoreItemDisplayV2), typeof(UI_BusStopMenu)
        };

        private static readonly Type[] IgnoreTypes = new Type[]
        {
            typeof(UI_DialogSystem), typeof(UI_PortraitDialogSystem), typeof(UI_HUDManager), typeof(UI_HUD_Player), typeof(UI_HUD_Boss),
            typeof(DamageText), typeof(UI2DFloatyText), typeof(UI_Watermark), typeof(UI_Meter), typeof(UI_PlayerOffscreenIndicator),
            typeof(UI_DebugMenu_Manager), typeof(SceneInfoDisplay), typeof(UI_ItemPickup)
        };

        public static void OnSceneLoaded(string scene)
        {
            _texts.Clear();
            _selGate.Clear();
            _focus = null;
            _pendingFocus = null;
        }

        // ---------------- hooks (called from Harmony patches) ----------------
        public static void OnHover(Component sel)
        {
            if (sel == null) return;
            if (_pendingSpecific != null && _pendingFrame == Time.frameCount) return;
            _pendingFocus = sel;
            _pendingFrame = Time.frameCount;
        }

        /// <summary>A screen-specific hover with already prepared text (has priority over the generic hover).</summary>
        public static void OnSpecificFocus(Component sel, string text)
        {
            if (sel == null || string.IsNullOrEmpty(text)) return;
            _pendingFocus = sel;
            _pendingSpecific = text;
            _pendingFrame = Time.frameCount;
        }

        public static void OnDialogReset(UI_DialogSystem ds)
        {
            try
            {
                DialogMessage m = Traverse(ds, "_currentMessage") as DialogMessage;
                if (m == null || m == _lastDialog) return;
                _lastDialog = m;
                string msg = GameInfo.Clean(m.Message);
                if (msg.Length == 0) return;
                string who = GameInfo.Clean(m.Narrator);
                Log.Text("Dialog " + m.Character + "/" + m.Subtype, who + ": " + msg);
                bool speak;
                switch (m.Character)
                {
                    case DialogCharacterTypes.Quest:
                    case DialogCharacterTypes.System:
                        speak = Settings.Hints; who = ""; break;
                    case DialogCharacterTypes.Thug:
                        speak = Settings.Chatter; break;
                    case DialogCharacterTypes.Passive:
                        speak = (m.IsPlayerCharacterChat() || m.Subtype == DialogSubType.Narration) ? Settings.Dialogs : Settings.Chatter; break;
                    default:
                        speak = Settings.Dialogs; break;
                }
                if (m.Subtype == DialogSubType.QuestStart || m.Subtype == DialogSubType.QuestComplete || m.Subtype == DialogSubType.QuestFailed || m.Subtype == DialogSubType.ItemCollect || m.Subtype == DialogSubType.Tutorial)
                { speak = Settings.Hints; who = ""; }
                if (!speak) return;
                string text = (who.Length > 0 ? who + ": " : "") + msg;
                if (WasSpokenRecently(text, 4f)) return;
                MarkSpoken(text);
                Speech.Queue(text);
            }
            catch (Exception e) { Log.Error("OnDialogReset", e); }
        }

        public static void OnPortraitLine(UI_PortraitDialogSystem ps)
        {
            try
            {
                DialogLine line = Traverse(ps, "_currentLine") as DialogLine;
                if (line == null) return;
                string msg = GameInfo.TranslateOr(line.Dialog, "");
                if (msg.Length == 0) return;
                string who = line.Character ?? "";
                if (who.ToLower() == "kenji") who = "???";
                string text = (who.Length > 0 ? who + ": " : "") + msg;
                Log.Text("Portrait", text);
                if (!Settings.Dialogs) return;
                if (text == _lastPortraitLine) return;
                _lastPortraitLine = text;
                if (!_portraitHintGiven) { _portraitHintGiven = true; text += " " + Loc.T("portrait_hint"); }
                Speech.Say(text, true);
            }
            catch (Exception e) { Log.Error("OnPortraitLine", e); }
        }

        public static float LastTutorialAt = -10f;

        public static void OnTutorial(UI_TutorialSlideIn t, string headerKey, string bodyKey)
        {
            LastTutorialAt = Time.realtimeSinceStartup;
            try
            {
                TMP_Text h = Traverse(t, "_headerText") as TMP_Text;
                TMP_Text b = Traverse(t, "_bodyText") as TMP_Text;
                string text = "";
                if (h != null) text = GameInfo.Clean(h.text);
                if (b != null) text += (text.Length > 0 ? ". " : "") + GameInfo.Clean(b.text);
                Log.Text("Tutorial", text);
                Navigator.OnTutorialShown(headerKey + " " + bodyKey, text);
                if (!Settings.Hints || text.Length == 0) return;
                MarkSpoken(text);
                if (h != null) MarkSpoken(GameInfo.Clean(h.text));
                if (b != null) MarkSpoken(GameInfo.Clean(b.text));
                Speech.Say(Loc.T("tutorial") + ": " + text + " " + Loc.T("tut_close"), true);
            }
            catch (Exception e) { Log.Error("OnTutorial", e); }
        }

        public static object Traverse(object o, string field)
        {
            if (o == null) return null;
            Type t = o.GetType();
            while (t != null)
            {
                FieldInfo f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return f.GetValue(o);
                t = t.BaseType;
            }
            return null;
        }

        public static string ReadFields(object o, params string[] fields)
        {
            List<string> parts = new List<string>();
            foreach (string f in fields)
            {
                TMP_Text t = Traverse(o, f) as TMP_Text;
                if (t == null || !t.isActiveAndEnabled) continue;
                string s = GameInfo.Clean(t.text);
                if (s.Length > 0 && !parts.Contains(s)) parts.Add(s);
            }
            return string.Join(", ", parts.ToArray());
        }

        // ---------------- recent spoken dedupe ----------------
        private static bool WasSpokenRecently(string s, float secs)
        {
            float t;
            if (_recentSpoken.TryGetValue(s, out t) && Time.realtimeSinceStartup - t < secs) return true;
            return false;
        }

        private static void MarkSpoken(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (_recentSpoken.Count > 300) _recentSpoken.Clear();
            _recentSpoken[s] = Time.realtimeSinceStartup;
        }

        // ---------------- focus text ----------------
        private static string FocusText(Component sel)
        {
            if (sel == null) return "";
            string phone = PhoneHelp.FocusText(sel);
            if (phone != null) return phone;
            List<string> parts = new List<string>();
            foreach (TMP_Text t in sel.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!VisibleLoose(t)) continue;
                if (t.name.ToLower().Contains("debug")) continue;
                string s = GameInfo.Clean(t.text);
                if (s.Length == 0 || parts.Contains(s)) continue;
                parts.Add(s);
            }
            if (parts.Count == 0)
            {
                foreach (Text t in sel.GetComponentsInChildren<Text>(false))
                {
                    string s = GameInfo.Clean(t.text);
                    if (s.Length > 0 && !parts.Contains(s)) parts.Add(s);
                }
            }
            if (parts.Count == 0)
            {
                // label next to the entry (same parent, not part of another selectable)
                Transform p = sel.transform.parent;
                if (p != null)
                {
                    List<string> around = new List<string>();
                    foreach (TMP_Text t in p.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (!VisibleLoose(t)) continue;
                        UI_InputSelectable other = t.GetComponentInParent<UI_InputSelectable>();
                        if (other != null && other != sel) continue;
                        string s = GameInfo.Clean(t.text);
                        if (s.Length > 0 && !around.Contains(s)) around.Add(s);
                    }
                    if (around.Count > 0 && around.Count <= 3) parts.AddRange(around);
                }
            }
            if (parts.Count == 0) parts.Add(GameInfo.Readable(sel.name));
            return string.Join(", ", parts.ToArray());
        }

        private static bool VisibleLoose(TMP_Text t)
        {
            return t != null && t.isActiveAndEnabled && t.color.a > 0.05f && t.alpha > 0.05f;
        }

        private static FieldInfo _screenSelectables = typeof(UI_MenuScreen).GetField("_selectables", BindingFlags.Instance | BindingFlags.NonPublic);

        private static string Position(Component sel)
        {
            try
            {
                UI_MenuScreen screen = sel.GetComponentInParent<UI_MenuScreen>();
                if (screen == null) return "";
                List<UI_InputSelectable> list = _screenSelectables.GetValue(screen) as List<UI_InputSelectable>;
                if (list == null) return "";
                List<UI_InputSelectable> active = new List<UI_InputSelectable>();
                foreach (UI_InputSelectable s in list) if (s != null && s.isActiveAndEnabled) active.Add(s);
                int i = active.IndexOf(sel as UI_InputSelectable);
                if (i < 0 || active.Count < 2) return "";
                return Loc.T("pos", i + 1, active.Count);
            }
            catch { return ""; }
        }

        // ---------------- update ----------------
        public static void Update()
        {
            HandleKeys();
            UpdateContinue();
        }

        // ---------------- continue / quit after a knock-out ----------------
        private static UI_ContinueOrExit_Main _cont;
        private static float _contScanAt;
        private static int _contSel = -2;
        private static FieldInfo _contSelected = typeof(UI_ContinueOrExit_Main).GetField("_selected", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void UpdateContinue()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (_cont == null && now >= _contScanAt)
                {
                    _contScanAt = now + 1f;
                    _cont = UnityEngine.Object.FindObjectOfType<UI_ContinueOrExit_Main>();
                }
                if (_cont == null || !_cont.isActiveAndEnabled) { _contSel = -2; return; }
                int sel = (int)_contSelected.GetValue(_cont);
                if (sel == _contSel) return;
                bool first = _contSel == -2;
                _contSel = sel;
                if (sel < 0 || !Settings.Menus) return;
                GameObject act = sel == 0 ? _cont._Continue_Active : _cont._Quit_Active;
                string label = "";
                if (act != null)
                    foreach (TMP_Text t in act.GetComponentsInChildren<TMP_Text>(true))
                    {
                        string c = GameInfo.Clean(t.text);
                        if (c.Length > 0) { label = c; break; }
                    }
                if (label.Length == 0) label = Loc.T(sel == 0 ? "cont_continue" : "cont_quit");
                string say = label + ", " + Loc.T("pos", sel + 1, 2);
                if (first) say = Loc.T("cont_title") + " " + say;
                MarkSpoken(label);
                Speech.Say(say, !first);
            }
            catch (Exception e) { Log.Error("UpdateContinue", e); }
        }

        public static void LateUpdate()
        {
            float now = Time.realtimeSinceStartup;
            bool focusNow = _pendingFocus != null;
            if (focusNow || now >= _nextPoll)
            {
                _nextPoll = now + 0.2f;
                PollTexts(focusNow);
            }
            if (!focusNow && _focus != null && now >= _nextFocusCheck)
            {
                _nextFocusCheck = now + 0.15f;
                CheckFocusChange();
            }
        }

        private static List<string> _contextNew = new List<string>();

        private static void PollTexts(bool withFocus)
        {
            _contextNew.Clear();
            bool menusOn = Settings.Menus;
            bool hintsOn = Settings.Hints;
            bool interruptContext = false;
            TMP_Text[] all;
            try { all = UnityEngine.Object.FindObjectsOfType<TMP_Text>(); }
            catch { return; }
            float now = Time.realtimeSinceStartup;
            List<KeyValuePair<Vector2, string>> menuItems = new List<KeyValuePair<Vector2, string>>();
            List<KeyValuePair<Vector2, string>> popupItems = new List<KeyValuePair<Vector2, string>>();
            HashSet<int> seen = new HashSet<int>();

            foreach (TMP_Text t in all)
            {
                if (t == null) continue;
                int id = t.GetInstanceID();
                seen.Add(id);
                TextState st;
                if (!_texts.TryGetValue(id, out st)) { st = new TextState(); _texts[id] = st; Classify(t, st); }
                if (st.Excluded) continue;
                bool vis = GameInfo.Visible(t);
                string raw = t.text ?? "";
                string txt = vis ? GameInfo.Clean(raw) : "";
                if (vis && txt.Length == 0) vis = false;
                bool wasVis = st.Visible;
                string old = st.Text;
                st.Visible = vis;
                if (vis && (!GateOpen(st.Gate) || !GameInfo.OnScreen(t.rectTransform))) { vis = false; st.Visible = false; }
                if (!vis) { continue; }
                if (wasVis && txt == old) continue;
                // typewriter: text grows -> wait one poll until it is stable
                if (wasVis && old.Length > 0 && txt.Length > old.Length && txt.StartsWith(old) && st.Candidate != txt)
                {
                    st.Candidate = txt;
                    continue;
                }
                st.Candidate = null;
                st.Text = txt;
                if (txt != st.Logged)
                {
                    st.Logged = txt;
                    Log.Text(GameInfo.Path(t.transform), txt);
                }
                // counters and animated texts
                if (wasVis && txt != old)
                {
                    st.Changes[st.ChangeIdx] = now;
                    st.ChangeIdx = (st.ChangeIdx + 1) % st.Changes.Length;
                    int recent = 0;
                    foreach (float c in st.Changes) if (now - c < 3f) recent++;
                    if (recent >= 5) st.MutedUntil = now + 10f;
                }
                if (now < st.MutedUntil) continue;
                if (txt.IndexOf('\u00A9') >= 0 || txt.IndexOf("Copyright", StringComparison.OrdinalIgnoreCase) >= 0
                    || txt.IndexOf("rights reserved", StringComparison.OrdinalIgnoreCase) >= 0 || txt.Contains(">>") || txt == "ARC System Works") continue;
                if (!(t is TextMeshProUGUI)) continue;

                // value change of the hovered entry
                if (st.InSelectable)
                {
                    continue; // handled by CheckFocusChange / hover
                }
                if (st.ContainerKind == K_None || st.ContainerKind == K_Ignore) continue;
                bool isPrompt = raw.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0;
                if (st.ContainerKind == K_Menu)
                {
                    if (!menusOn) continue;
                    if (isPrompt && withFocus) continue;
                    if (!GameInfo.HasLetters(txt)) continue;
                    menuItems.Add(new KeyValuePair<Vector2, string>(ScreenPos(t), txt));
                }
                else
                {
                    if (!hintsOn) continue;
                    if (st.ContainerKind == K_Tutorial) interruptContext = true;
                    popupItems.Add(new KeyValuePair<Vector2, string>(ScreenPos(t), txt));
                }
            }
            // forget destroyed texts now and then
            if (_texts.Count > 3000)
            {
                List<int> dead = new List<int>();
                foreach (int k in _texts.Keys) if (!seen.Contains(k)) dead.Add(k);
                foreach (int k in dead) _texts.Remove(k);
            }

            SortByScreen(menuItems);
            SortByScreen(popupItems);
            // a new menu screen: only the first lines (title), the rest is available with F2
            int maxMenu = 3;
            for (int i = 0; i < menuItems.Count && i < maxMenu; i++) AddContext(menuItems[i].Value);
            foreach (KeyValuePair<Vector2, string> kv in popupItems) AddContext(kv.Value);

            string focusText = null;
            if (withFocus) focusText = TakeFocus();

            List<string> outParts = new List<string>();
            foreach (string c in _contextNew)
            {
                if (WasSpokenRecently(c, 3f)) continue;
                MarkSpoken(c);
                outParts.Add(c);
            }
            if (focusText != null) outParts.Add(focusText);
            if (outParts.Count == 0) return;
            string all2 = string.Join(". ", outParts.ToArray());
            bool interrupt = focusText != null || interruptContext;
            Speech.Say(all2, interrupt);
        }

        private static void AddContext(string s)
        {
            if (!_contextNew.Contains(s)) _contextNew.Add(s);
        }

        private static string TakeFocus()
        {
            Component sel = _pendingFocus;
            string specific = _pendingSpecific;
            _pendingFocus = null;
            _pendingSpecific = null;
            if (sel == null) return null;
            if (!SelectableShown(sel)) { Log.Verbose("Auswahl verdeckt: " + GameInfo.Path(sel.transform)); return null; }
            string text = specific ?? FocusText(sel);
            if (text.Length == 0) return null;
            float now = Time.realtimeSinceStartup;
            bool same = sel == _focus && text == _focusText;
            _focus = sel;
            _focusText = text;
            _focusTime = now;
            if (same && now - _lastFocusSpokenTime < 1.5f) return null;
            if (!Settings.Menus) return null;
            string pos = specific == null ? Position(sel) : "";
            string spoken = text + (pos.Length > 0 ? ", " + pos : "");
            _lastFocusSpoken = spoken;
            _lastFocusSpokenTime = now;
            foreach (string part in text.Split(new string[] { ", " }, StringSplitOptions.None)) MarkSpoken(part);
            Log.Text("Auswahl", spoken);
            return spoken;
        }

        private static void CheckFocusChange()
        {
            try
            {
                if (_focus == null || !_focus.gameObject.activeInHierarchy) { _focus = null; return; }
                if (!(_focus is UI_InputSelectable)) return;
                string now = FocusText(_focus);
                if (now == _focusText || now.Length == 0) return;
                // speak only what changed
                List<string> oldParts = new List<string>(_focusText.Split(new string[] { ", " }, StringSplitOptions.None));
                List<string> diff = new List<string>();
                foreach (string p in now.Split(new string[] { ", " }, StringSplitOptions.None)) if (!oldParts.Contains(p)) diff.Add(p);
                _focusText = now;
                if (!Settings.Menus) return;
                string s = diff.Count > 0 ? string.Join(", ", diff.ToArray()) : now;
                Log.Text("Wert", s);
                Speech.Say(s, true);
            }
            catch (Exception e) { Log.Error("CheckFocusChange", e); _focus = null; }
        }

        private static void Classify(TMP_Text t, TextState st)
        {
            st.ContainerKind = K_None;
            try
            {
                string lname = t.name.ToLower();
                if (lname.Contains("debug")) { st.Excluded = true; return; }
                UI_InputSelectable sel = t.GetComponentInParent<UI_InputSelectable>();
                if (sel != null) { st.InSelectable = true; st.Selectable = sel; }
                st.Gate = FindGate(t.transform);
                Transform tr = t.transform;
                while (tr != null)
                {
                    foreach (Type it in IgnoreTypes)
                        if (tr.GetComponent(it) != null) { st.ContainerKind = K_Ignore; return; }
                    if (tr.GetComponent<UI_TutorialSlideIn>() != null) { st.ContainerKind = K_Ignore; return; }
                    foreach (Type pt in PopupTypes)
                    {
                        Component c = tr.GetComponent(pt);
                        if (c != null) { st.ContainerKind = K_Popup; st.Container = c; return; }
                    }
                    if (tr.GetComponent<UI_MenuScreen>() != null) { st.ContainerKind = K_Menu; return; }
                    tr = tr.parent;
                }
            }
            catch { }
        }

        private static Vector2 ScreenPos(TMP_Text t)
        {
            try
            {
                Canvas c = t.canvas;
                Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
                return RectTransformUtility.WorldToScreenPoint(cam, t.rectTransform.position);
            }
            catch { return Vector2.zero; }
        }

        private static void SortByScreen(List<KeyValuePair<Vector2, string>> list)
        {
            list.Sort(delegate (KeyValuePair<Vector2, string> a, KeyValuePair<Vector2, string> b)
            {
                float ra = Mathf.Round(a.Key.y / 25f), rb = Mathf.Round(b.Key.y / 25f);
                if (ra != rb) return rb.CompareTo(ra); // top first
                return a.Key.x.CompareTo(b.Key.x);
            });
        }

        // ---------------- keys ----------------
        private static void HandleKeys()
        {
            if (ModInput.KeyPlain(KeyCode.F2)) ReadScreen(false);
            else if (ModInput.KeyPlain(KeyCode.F3))
            {
                if (_focus != null && _focus.gameObject.activeInHierarchy) Speech.Say(FocusText(_focus) + ", " + Position(_focus), true);
                else Speech.Say(Loc.T("no_focus"), true);
            }
            else if (ModInput.KeyPlain(KeyCode.F4)) ReadScreen(true);
        }

        /// <summary>F2: all visible texts top to bottom. F4: only button hints.</summary>
        public static void ReadScreen(bool promptsOnly)
        {
            List<KeyValuePair<Vector2, string>> items = new List<KeyValuePair<Vector2, string>>();
            List<string> seen = new List<string>();
            foreach (TMP_Text t in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
            {
                if (!(t is TextMeshProUGUI) || !GameInfo.Visible(t)) continue;
                if (t.name.ToLower().Contains("debug")) continue;
                if (!GateOpen(FindGate(t.transform)) || !GameInfo.OnScreen(t.rectTransform)) continue;
                if (t.GetComponentInParent<UI_HUDManager>() != null && !promptsOnly) continue;
                if (t.GetComponentInParent<SceneInfoDisplay>() != null) continue;
                string raw = t.text ?? "";
                bool prompt = raw.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0;
                if (promptsOnly && !prompt) continue;
                string s = GameInfo.Clean(raw);
                if (s.Length == 0 || seen.Contains(s)) continue;
                if (!promptsOnly && !GameInfo.HasLetters(s) && s.Length < 2) continue;
                seen.Add(s);
                items.Add(new KeyValuePair<Vector2, string>(ScreenPos(t), s));
            }
            SortByScreen(items);
            List<string> parts = new List<string>();
            foreach (KeyValuePair<Vector2, string> kv in items) parts.Add(kv.Value);
            if (parts.Count == 0) { Speech.Say(Loc.T(promptsOnly ? "no_prompts" : "no_text"), true); return; }
            Log.Text(promptsOnly ? "F4" : "F2", string.Join(" | ", parts.ToArray()));
            Speech.Say(string.Join(". ", parts.ToArray()), true);
        }
    }
}
