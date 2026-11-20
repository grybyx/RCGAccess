using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

namespace RCGAccess
{
    /// <summary>Spoken mod menu (F11). Shown before the game on first start.</summary>
    public static class ModMenu
    {
        private abstract class Entry
        {
            public string Key;
            public Action Sample; // example of the sound this entry is about, played after the entry is read
            public abstract string Value();
            public virtual void Change(int dir) { }
            public virtual void Activate() { Change(1); }
            public virtual bool IsAction { get { return false; } }
        }

        private class Toggle : Entry
        {
            public Func<bool> Get; public Action<bool> Set;
            public override string Value() { return Loc.T(Get() ? "on" : "off"); }
            public override void Change(int dir) { Set(!Get()); }
        }

        private class Number : Entry
        {
            public Func<int> Get; public Action<int> Set; public int Min, Max, Step; public string Unit; public Action Preview;
            public override string Value() { return Get().ToString() + (Unit != null ? " " + Unit : ""); }
            public override void Change(int dir)
            {
                int v = Mathf.Clamp(Get() + dir * Step, Min, Max);
                Set(v);
                if (Preview != null) Preview();
            }
            public override void Activate() { }
        }

        private class Choice : Entry
        {
            public Func<List<string>> Options; public Func<int> Get; public Action<int> Set;
            public override string Value()
            {
                List<string> o = Options();
                int i = Get();
                return (i >= 0 && i < o.Count) ? o[i] : "";
            }
            public override void Change(int dir)
            {
                List<string> o = Options();
                if (o.Count == 0) return;
                int i = Get() + dir;
                if (i < 0) i = o.Count - 1;
                if (i >= o.Count) i = 0;
                Set(i);
            }
        }

        private class SubMenu : Entry
        {
            public List<Entry> Children;
            public override string Value() { return ""; }
            public override void Activate() { Enter(this); }
            public override bool IsAction { get { return true; } }
        }

        private class ActionEntry : Entry
        {
            public Action Run;
            public override string Value() { return ""; }
            public override void Activate() { Run(); }
            public override bool IsAction { get { return true; } }
        }

        private static readonly List<Entry> _root = new List<Entry>();
        private static List<Entry> _entries = _root;
        private static readonly List<KeyValuePair<List<Entry>, int>> _stack = new List<KeyValuePair<List<Entry>, int>>();
        private static int _index;
        private static bool _open;
        private static bool _closing;
        private static bool _firstStart;
        private static Dictionary<string, object> _snapshot;
        private static float _savedTimeScale = 1f;
        private static readonly List<KeyValuePair<ControllerMap, bool>> _maps = new List<KeyValuePair<ControllerMap, bool>>();
        private static readonly List<VideoPlayer> _videos = new List<VideoPlayer>();
        private static EventSystem _eventSystem;
        private static bool _eventSystemWasEnabled;
        private static float _openedAt;

        public static bool IsOpen { get { return _open; } }

        private static void Build()
        {
            _root.Clear();
            _stack.Clear();
            List<Entry> speech = new List<Entry>();
            speech.Add(new Choice
            {
                Key = "m_modlang",
                Options = delegate { return new List<string> { Loc.T("lang_auto"), "Deutsch", "English" }; },
                Get = delegate { return Settings.ModLang; },
                Set = delegate (int v) { Settings.ModLang = v; Speech.ApplyLanguage(true); }
            });
            speech.Add(new Choice
            {
                Key = "m_speech",
                Options = delegate { return new List<string> { Loc.T("speech_onecore"), Loc.T("speech_sapi"), Loc.T("speech_reader") }; },
                Get = delegate { return Settings.SpeechEngine; },
                Set = delegate (int v) { Settings.SpeechEngine = v; Speech.EnsureEngine(); }
            });
            speech.Add(VoiceChoice());
            speech.Add(new Number
            {
                Key = "m_rate", Min = -5, Max = 10, Step = 1,
                Get = delegate { return Settings.Rate; },
                Set = delegate (int v) { Settings.Rate = v; Speech.Config("rate", v.ToString()); }
            });
            speech.Add(new Number
            {
                Key = "m_speechvol", Min = 10, Max = 100, Step = 10, Unit = "%",
                Get = delegate { return Settings.SpeechVolume; },
                Set = delegate (int v) { Settings.SpeechVolume = v; Speech.Config("volume", v.ToString()); }
            });

            List<Entry> texts = new List<Entry>();
            Add(texts, "m_menus", () => Settings.Menus, v => Settings.Menus = v);
            Add(texts, "m_dialogs", () => Settings.Dialogs, v => Settings.Dialogs = v);
            Add(texts, "m_chatter", () => Settings.Chatter, v => Settings.Chatter = v);
            Add(texts, "m_hints", () => Settings.Hints, v => Settings.Hints = v);
            Add(texts, "m_room", () => Settings.RoomAnnounce, v => Settings.RoomAnnounce = v);
            Add(texts, "m_pickupspeak", () => Settings.PickupSpeak, v => Settings.PickupSpeak = v, delegate { Samples.Pickup(0); });
            Vol(texts, "m_vol_pickup", () => Settings.PickupVolume, v => Settings.PickupVolume = v, delegate { Samples.Pickup(0); });
            Add(texts, "m_moneyspeak", () => Settings.MoneySpeak, v => Settings.MoneySpeak = v);

            List<Entry> fight = new List<Entry>();
            Add(fight, "m_radar", () => Settings.Radar, v => Settings.Radar = v, delegate { Samples.Knock(0); });
            fight.Add(new Number
            {
                Key = "m_knockvol", Min = 0, Max = 200, Step = 10, Unit = "%",
                Get = delegate { return Settings.KnockVolume; },
                Set = delegate (int v) { Settings.KnockVolume = v; },
                Preview = delegate { Combat.PreviewKnock(); },
                Sample = delegate { Combat.PreviewKnock(); }
            });
            Add(fight, "m_linetone", () => Settings.LineTone, v => Settings.LineTone = v, delegate { Samples.Line(0); });
            Vol(fight, "m_vol_line", () => Settings.LineVolume, v => Settings.LineVolume = v, delegate { Samples.LineHigh(0); });
            Add(fight, "m_radarheight", () => Settings.RadarHeight, v => Settings.RadarHeight = v, delegate
            {
                float kv = Settings.KnockVolume / 100f;
                Speech.RawTone(Combat.LowPitch * 1.22f, 60, 0, kv, 0, Combat.LowPitch, 0);
                Speech.RawTone(Combat.LowPitch * 0.8f, 60, 0, kv, 0, Combat.LowPitch * 0.68f, 500);
            });
            fight.Add(new Number
            {
                Key = "m_radarrange", Min = 4, Max = 20, Step = 1, Unit = Loc.T("unit_m"),
                Get = delegate { return Settings.RadarRange; },
                Set = delegate (int v) { Settings.RadarRange = v; }
            });
            Add(fight, "m_bossbeacon", () => Settings.BossBeacon, v => Settings.BossBeacon = v, delegate { Samples.Boss(0); });
            Vol(fight, "m_vol_boss", () => Settings.BossVolume, v => Settings.BossVolume = v, delegate { BossHelp.Beep(-1.5f, 0f, false, false, 0); });
            Add(fight, "m_bossmoves", () => Settings.BossMoves, v => Settings.BossMoves = v);
            Add(fight, "m_enemyannounce", () => Settings.EnemyAnnounce, v => Settings.EnemyAnnounce = v, delegate { Samples.Fight(0); });
            Vol(fight, "m_vol_fight", () => Settings.FightVolume, v => Settings.FightVolume = v, delegate { Samples.Fight(0); });
            Add(fight, "m_attackwarn", () => Settings.AttackWarn, v => Settings.AttackWarn = v, delegate { Samples.Attack(0); });
            Vol(fight, "m_vol_attack", () => Settings.AttackVolume, v => Settings.AttackVolume = v, delegate { Samples.Attack(0); });
            Add(fight, "m_health", () => Settings.HealthTones, v => Settings.HealthTones = v, delegate { Samples.Health(0); });
            Vol(fight, "m_vol_health", () => Settings.HealthVolume, v => Settings.HealthVolume = v, delegate { Samples.Health(0); });
            Add(fight, "m_boss", () => Settings.BossHealth, v => Settings.BossHealth = v);
            Add(fight, "m_recruitinfo", () => Settings.RecruitInfo, v => Settings.RecruitInfo = v, delegate { Samples.Recruit(0); });
            Vol(fight, "m_vol_recruit", () => Settings.RecruitVolume, v => Settings.RecruitVolume = v, delegate { Samples.Recruit(0); });
            fight.Add(new Choice
            {
                Key = "m_countbtn",
                Options = delegate
                {
                    Bindings.Btn? auto = Bindings.AutoCountButton();
                    string a = Loc.T("lang_auto_short") + (auto.HasValue ? " (" + Bindings.Name(auto.Value) + ")" : (Bindings.Known ? " (" + Loc.T("none_free") + ")" : ""));
                    return new List<string> { a, "LB / L1", "RB / R1", "LT / L2", "RT / R2", Loc.T("btn_back"), Loc.T("off") };
                },
                Get = delegate { return Settings.EnemyCountButton; },
                Set = delegate (int v) { Settings.EnemyCountButton = v; }
            });
            fight.Add(new Choice
            {
                Key = "m_damage",
                Options = delegate { return new List<string> { Loc.T("off"), Loc.T("dmg_half"), Loc.T("dmg_none") }; },
                Get = delegate { return Settings.DamageHelp; },
                Set = delegate (int v) { Settings.DamageHelp = v; }
            });
            fight.Add(new Choice
            {
                Key = "m_bossdmg",
                Options = delegate { return new List<string> { Loc.T("bossdmg_0"), Loc.T("bossdmg_1"), Loc.T("bossdmg_2"), Loc.T("bossdmg_3") }; },
                Get = delegate { return Settings.BossDamage; },
                Set = delegate (int v) { Settings.BossDamage = v; }
            });
            Add(fight, "m_bossassist", () => Settings.BossAssist, v => Settings.BossAssist = v);

            List<Entry> nav = new List<Entry>();
            Add(nav, "m_padkeys", () => Settings.PadKeys, v => Settings.PadKeys = v);
            Add(nav, "m_lswalk", () => Settings.LsWalk, v => Settings.LsWalk = v);
            Vol(nav, "m_vol_beacon", () => Settings.BeaconVolume, v => Settings.BeaconVolume = v, delegate { Samples.Beacon(0); });
            Add(nav, "m_interact", () => Settings.Interact, v => Settings.Interact = v, delegate { Samples.Interact(0); });
            Vol(nav, "m_vol_interact", () => Settings.InteractVolume, v => Settings.InteractVolume = v, delegate { Samples.Interact(0); });
            Add(nav, "m_itemnav", () => Settings.ItemNav, v => Settings.ItemNav = v, delegate { Samples.ItemAppears(0); });
            Vol(nav, "m_vol_item", () => Settings.ItemVolume, v => Settings.ItemVolume = v, delegate { Samples.ItemAppears(0); });
            Add(nav, "m_deadend", () => Settings.DeadEnd, v => Settings.DeadEnd = v, delegate { Samples.Wall(0); Samples.Obstacle(700); });
            Vol(nav, "m_vol_wall", () => Settings.WallVolume, v => Settings.WallVolume = v, delegate { Samples.Wall(0); Samples.Obstacle(700); });
            Add(nav, "m_hazards", () => Settings.Hazards, v => Settings.Hazards = v, delegate { Samples.Pit(0); Samples.Toy(700); });
            Vol(nav, "m_vol_hazard", () => Settings.HazardVolume, v => Settings.HazardVolume = v, delegate { Samples.Pit(0); Samples.Toy(700); });
            Add(nav, "m_autowalkstop", () => Settings.AutoWalkStopForEnemies, v => Settings.AutoWalkStopForEnemies = v);
            Add(nav, "m_walkrun", () => Settings.WalkRun, v => Settings.WalkRun = v);
            Add(nav, "m_autodoor", () => Settings.AutoDoor, v => Settings.AutoDoor = v);

            List<Entry> sound = new List<Entry>();
            sound.Add(new Number
            {
                Key = "m_tonevol", Min = 0, Max = 100, Step = 10, Unit = "%",
                Get = delegate { return Settings.ToneVolume; },
                Set = delegate (int v) { Settings.ToneVolume = v; },
                Preview = delegate { Navigator.PreviewBeacon(); },
                Sample = delegate { Samples.Beacon(0); }
            });
            Add(sound, "m_moneytones", () => Settings.MoneyTones, v => Settings.MoneyTones = v, delegate { Samples.Money(0); });
            Vol(sound, "m_vol_money", () => Settings.MoneyVolume, v => Settings.MoneyVolume = v, delegate { Samples.Money(0); });
            Add(sound, "m_duckspeech", () => Settings.DuckSpeech, v => Settings.DuckSpeech = v);
            Add(sound, "m_ducktones", () => Settings.DuckTones, v => Settings.DuckTones = v);
            sound.Add(new Number
            {
                Key = "m_ducklevel", Min = 0, Max = 100, Step = 10, Unit = "%",
                Get = delegate { return Settings.DuckLevel; },
                Set = delegate (int v) { Settings.DuckLevel = v; }
            });
            Add(sound, "m_menusamples", () => Settings.MenuSamples, v => Settings.MenuSamples = v);
            sound.Add(new ActionEntry { Key = "m_tonetest", Run = delegate { Samples.All(); } });

            List<Entry> more = new List<Entry>();
            Add(more, "m_verbose", () => Settings.VerboseLog, v => Settings.VerboseLog = v);
            Add(more, "m_everystart", () => Settings.MenuEveryStart, v => Settings.MenuEveryStart = v);

            _root.Add(Sub("m_sub_speech", speech));
            _root.Add(Sub("m_sub_texts", texts));
            _root.Add(Sub("m_sub_fight", fight));
            _root.Add(Sub("m_sub_nav", nav));
            _root.Add(Sub("m_sub_sound", sound));
            _root.Add(Sub("m_sub_more", more));
            _root.Add(new ActionEntry { Key = "m_donation", Run = delegate { OpenUrl("https://buymeacoffee.com/blindretrogamer/", Loc.T("m_donation")); } });
            _root.Add(new ActionEntry { Key = "m_grybyx", Run = delegate { OpenUrl("https://github.com/grybyx/", Loc.T("m_grybyx")); } });
            _root.Add(new ActionEntry { Key = "m_save", Run = delegate { Close(true); } });
            _root.Add(new ActionEntry { Key = "m_discard", Run = delegate { Close(false); } });
            _entries = _root;
        }

        private static SubMenu Sub(string key, List<Entry> children)
        {
            children.Add(new ActionEntry { Key = "m_back", Run = delegate { Back(); } });
            return new SubMenu { Key = key, Children = children };
        }

        /// <summary>Volume of one group of sounds, 0 to 200 percent; each change plays the sound.</summary>
        private static void Vol(List<Entry> list, string key, Func<int> get, Action<int> set, Action sample)
        {
            list.Add(new Number
            {
                Key = key, Min = 0, Max = 200, Step = 10, Unit = "%",
                Get = delegate { return get(); },
                Set = delegate (int v) { set(v); },
                Preview = sample,
                Sample = sample
            });
        }

        private static void Add(List<Entry> list, string key, Func<bool> get, Action<bool> set)
        {
            list.Add(new Toggle { Key = key, Get = get, Set = set });
        }

        private static void Add(List<Entry> list, string key, Func<bool> get, Action<bool> set, Action sample)
        {
            list.Add(new Toggle { Key = key, Get = get, Set = set, Sample = sample });
        }

        /// <summary>Opens a submenu.</summary>
        private static void Enter(SubMenu m)
        {
            _stack.Add(new KeyValuePair<List<Entry>, int>(_entries, _index));
            _entries = m.Children;
            _index = 0;
            Speech.Say(Loc.T(m.Key), true);
            SpeakEntry(false);
        }

        /// <summary>Back to the parent menu; false at the top level.</summary>
        private static bool Back()
        {
            if (_stack.Count == 0) return false;
            KeyValuePair<List<Entry>, int> top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            _entries = top.Key;
            _index = top.Value;
            Speech.MenuClick();
            SpeakEntry(true);
            return true;
        }

        // the example sound waits until the entry has been read
        private static Action _sample;
        private static float _sampleAt;

        private static void UpdateSample()
        {
            if (_sample == null) return;
            float now = Time.realtimeSinceStartup;
            if (now - _sampleAt > 8f) { _sample = null; return; }
            if (now - _sampleAt < 0.35f || Speech.IsSpeaking) return;
            Action a = _sample;
            _sample = null;
            a();
        }

        private static List<KeyValuePair<string, int>> ReadVoices()
        {
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
            try
            {
                string p = Path.Combine(Mod.DataDir, "RCGAccessVoices.txt");
                if (!File.Exists(p)) return list;
                foreach (string line in File.ReadAllLines(p, Encoding.UTF8))
                {
                    int bar = line.LastIndexOf('|');
                    if (bar <= 0) continue;
                    int lcid;
                    try { lcid = Convert.ToInt32(line.Substring(bar + 1), 16); } catch { lcid = 0; }
                    list.Add(new KeyValuePair<string, int>(line.Substring(0, bar), lcid));
                }
            }
            catch { }
            return list;
        }

        /// <summary>The voice for the language the mod speaks now (game language or the fixed mod language).</summary>
        private static Entry VoiceChoice()
        {
            Func<int> prim = delegate { return Speech.VoiceLcid() & 0x3FF; };
            Func<List<string>> names = delegate
            {
                List<string> o = new List<string> { Loc.T("voice_auto") };
                if (Settings.SpeechEngine == 2) return o; // the screen reader keeps its own voice
                foreach (KeyValuePair<string, int> v in ReadVoices())
                    if ((v.Value & 0x3FF) == prim()) o.Add(v.Key);
                return o;
            };
            return new Choice
            {
                Key = "m_voice",
                Options = names,
                Get = delegate
                {
                    string cur = Speech.PreferredVoice(prim());
                    List<string> o = names();
                    int i = string.IsNullOrEmpty(cur) ? 0 : o.IndexOf(cur);
                    return i < 0 ? 0 : i;
                },
                Set = delegate (int i)
                {
                    List<string> o = names();
                    string v = (i <= 0 || i >= o.Count) ? "" : o[i];
                    Speech.SetPreferredVoice(prim(), v);
                    Speech.ApplyLanguage(true);
                }
            };
        }

        private static string LanguageName(int prim)
        {
            if (prim == 0x07) return Loc.T("lang_de");
            if (prim == 0x09) return Loc.T("lang_en");
            try { return new System.Globalization.CultureInfo(prim).DisplayName; } catch { return prim.ToString("X"); }
        }

        private static void OpenUrl(string url, string name)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(url);
                psi.UseShellExecute = true;
                Process.Start(psi);
                Speech.Say(Loc.T("url_opened", name), true);
            }
            catch (Exception e)
            {
                Log.Error("OpenUrl", e);
                Speech.Say(Loc.T("url_failed", url), true);
            }
        }

        // ---------------- open / close ----------------
        public static void Open(bool firstStart)
        {
            if (_open) return;
            _firstStart = firstStart;
            Build();
            _snapshot = Settings.Snapshot();
            _index = 0;
            _open = true;
            _closing = false;
            _openedAt = Time.realtimeSinceStartup;
            SuspendGame();
            string intro = Loc.T(firstStart ? (Settings.MenuVersion > 0 ? "menu_updated" : "menu_first") : "menu_open");
            Speech.Say(intro, true);
            SpeakEntry(false);
            Log.Write("Mod-Menue geoeffnet" + (firstStart ? " (Erststart)" : ""));
        }

        private static void Close(bool save)
        {
            if (!save) Settings.Restore(_snapshot);
            Settings.MenuVersion = Settings.CurrentMenuVersion;
            Settings.Save();
            Speech.EnsureEngine();
            Speech.ApplyLanguage(true);
            Speech.Config("rate", Settings.Rate.ToString());
            Speech.Config("volume", Settings.SpeechVolume.ToString());
            Speech.Say(Loc.T(save ? "menu_saved" : "menu_discarded"), true);
            _closing = true; // wait until all keys are released, then resume the game
            Log.Write("Mod-Menue geschlossen, " + (save ? "gespeichert" : "verworfen"));
        }

        private static void FinishClose()
        {
            _open = false;
            _closing = false;
            ResumeGame();
        }

        private static void SuspendGame()
        {
            try { _savedTimeScale = Time.timeScale; Time.timeScale = 0f; } catch { }
            _maps.Clear();
            try
            {
                if (ReInput.isReady)
                {
                    foreach (Player p in ReInput.players.GetPlayers(true))
                        foreach (ControllerMap m in p.controllers.maps.GetAllMaps())
                        {
                            _maps.Add(new KeyValuePair<ControllerMap, bool>(m, m.enabled));
                            m.enabled = false;
                        }
                }
            }
            catch (Exception e) { Log.Error("SuspendGame.Rewired", e); }
            try
            {
                _eventSystem = EventSystem.current;
                if (_eventSystem != null) { _eventSystemWasEnabled = _eventSystem.enabled; _eventSystem.enabled = false; }
            }
            catch { }
            _videos.Clear();
            try
            {
                foreach (VideoPlayer v in UnityEngine.Object.FindObjectsOfType<VideoPlayer>())
                    if (v.isPlaying) { v.Pause(); _videos.Add(v); }
            }
            catch { }
            Speech.SetGamePaused(true);
        }

        private static void ResumeGame()
        {
            try { Time.timeScale = _savedTimeScale; } catch { }
            foreach (KeyValuePair<ControllerMap, bool> kv in _maps)
            {
                try { if (kv.Key != null) kv.Key.enabled = kv.Value; } catch { }
            }
            _maps.Clear();
            try { if (_eventSystem != null) _eventSystem.enabled = _eventSystemWasEnabled; } catch { }
            foreach (VideoPlayer v in _videos) try { if (v != null) v.Play(); } catch { }
            _videos.Clear();
            Speech.SetGamePaused(false);
        }

        // ---------------- input ----------------
        private static string EntryName(Entry e)
        {
            if (e is SubMenu) return Loc.T(e.Key) + ", " + Loc.T("submenu");
            if (e.Key == "m_voice") return Loc.T("m_voice", LanguageName(Speech.VoiceLcid() & 0x3FF));
            return Loc.T(e.Key);
        }

        private static void SpeakEntry(bool interrupt)
        {
            Entry e = _entries[_index];
            string v = e.Value();
            string s = EntryName(e) + (v.Length > 0 ? ": " + v : "") + ". " + Loc.T("pos", _index + 1, _entries.Count);
            Speech.Say(s, interrupt);
            _sample = Settings.MenuSamples ? e.Sample : null;
            _sampleAt = Time.realtimeSinceStartup;
        }

        public static void Update()
        {
            if (_closing)
            {
                bool anyKey = Input.anyKey || ModInput.AnyPadButton();
                if (!anyKey) FinishClose();
                return;
            }
            // keep the game frozen even if something else changed it
            if (Time.timeScale != 0f) { _savedTimeScale = Time.timeScale; Time.timeScale = 0f; }
            if (Time.realtimeSinceStartup - _openedAt < 0.25f) return;
            UpdateSample();

            int mv = ModInput.MenuMove();
            if (mv == -1 || mv == 1)
            {
                _index = (_index + mv + _entries.Count) % _entries.Count;
                Speech.MenuClick();
                SpeakEntry(true);
            }
            else if (mv == -2 || mv == 2)
            {
                Entry e = _entries[_index];
                if (!e.IsAction)
                {
                    e.Change(mv > 0 ? 1 : -1);
                    Speech.MenuClick();
                    Speech.Say(e.Value(), true);
                }
            }
            if (ModInput.Key(KeyCode.Return) || ModInput.Key(KeyCode.KeypadEnter) || ModInput.Key(KeyCode.Space) || ModInput.PadDown(Pad.A))
            {
                Entry e = _entries[_index];
                if (e.IsAction) e.Activate();
                else if (e is Toggle || e is Choice) { e.Activate(); Speech.Say(e.Value(), true); }
                else SpeakEntry(true);
            }
            else if (ModInput.Key(KeyCode.F1) || ModInput.PadDown(Pad.Y))
            {
                Entry e = _entries[_index];
                string help = Loc.T(e.Key + "_help");
                if (e.Key == "m_countbtn" || e.Key == "m_padkeys") help += " " + Bindings.UsageNote();
                Speech.Say(help, true);
            }
            else if (ModInput.Key(KeyCode.Backspace) || ModInput.Key(KeyCode.Escape) || ModInput.PadDown(Pad.B))
            {
                if (!Back()) Close(false);
            }
            else if (ModInput.Key(KeyCode.F11) || ModInput.PadDown(Pad.Start))
            {
                Close(true);
            }
            else if (ModInput.Key(KeyCode.F9)) Speech.Repeat();
            else if (ModInput.Key(KeyCode.F10)) Speech.Stop();
            else if (ModInput.Key(KeyCode.Home)) { _index = 0; SpeakEntry(true); }
            else if (ModInput.Key(KeyCode.End)) { _index = _entries.Count - 1; SpeakEntry(true); }
        }

        public static void LateUpdate() { }

        // ---------------- on-screen display (for sighted helpers; speech stays the same) ----------------
        private static Texture2D _texDim, _texPanel, _texSel, _texLine;
        private static GUIStyle _stTitle, _stPath, _stName, _stNameSel, _stValue, _stValueSel, _stHelp, _stKeys;
        private static int _styleFor = -1;
        private static bool _drawErr;

        private static Texture2D Tex(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private static GUIStyle Style(int size, Color c, TextAnchor a, bool bold, bool wrap)
        {
            GUIStyle st = new GUIStyle(GUI.skin.label);
            st.fontSize = size;
            st.normal.textColor = c;
            st.alignment = a;
            st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            st.wordWrap = wrap;
            st.richText = false;
            st.clipping = TextClipping.Clip;
            return st;
        }

        private static void MakeStyles(float k)
        {
            if (_texDim == null) _texDim = Tex(new Color(0f, 0f, 0f, 0.6f));
            if (_texPanel == null) _texPanel = Tex(new Color(0.08f, 0.06f, 0.14f, 0.94f));
            if (_texSel == null) _texSel = Tex(new Color(1f, 0.84f, 0.25f, 1f));
            if (_texLine == null) _texLine = Tex(new Color(0.95f, 0.35f, 0.65f, 1f));
            int key = Mathf.RoundToInt(k * 100);
            if (_styleFor == key && _stTitle != null) return;
            _styleFor = key;
            Color white = new Color(0.97f, 0.97f, 0.97f), grey = new Color(0.78f, 0.78f, 0.84f), dark = new Color(0.08f, 0.06f, 0.14f), pink = new Color(1f, 0.55f, 0.78f);
            _stTitle = Style(Mathf.RoundToInt(34 * k), pink, TextAnchor.MiddleLeft, true, false);
            _stPath = Style(Mathf.RoundToInt(24 * k), grey, TextAnchor.MiddleRight, false, false);
            _stName = Style(Mathf.RoundToInt(27 * k), white, TextAnchor.MiddleLeft, false, false);
            _stNameSel = Style(Mathf.RoundToInt(27 * k), dark, TextAnchor.MiddleLeft, true, false);
            _stValue = Style(Mathf.RoundToInt(27 * k), new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleRight, false, false);
            _stValueSel = Style(Mathf.RoundToInt(27 * k), dark, TextAnchor.MiddleRight, true, false);
            _stHelp = Style(Mathf.RoundToInt(23 * k), white, TextAnchor.UpperLeft, false, true);
            _stKeys = Style(Mathf.RoundToInt(20 * k), grey, TextAnchor.MiddleCenter, false, true);
        }

        /// <summary>Called from the runner's OnGUI while the menu is open: a panel with the current (sub)menu, the
        /// entries with their values, the selected entry highlighted, its help text and the controls.</summary>
        public static void Draw()
        {
            if (!_open || _closing || _entries == null || _entries.Count == 0) return;
            try
            {
                float k = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2.5f);
                MakeStyles(k);
                GUI.depth = -1000;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _texDim);
                float pw = Mathf.Min(1150f * k, Screen.width - 40f), ph = Mathf.Min(900f * k, Screen.height - 40f);
                float px = (Screen.width - pw) / 2f, py = (Screen.height - ph) / 2f;
                GUI.DrawTexture(new Rect(px, py, pw, ph), _texPanel);
                GUI.DrawTexture(new Rect(px, py, pw, 5f * k), _texLine);
                float pad = 28f * k;
                float x = px + pad, w = pw - 2 * pad, y = py + 18f * k;

                // title and where you are
                GUI.Label(new Rect(x, y, w * 0.45f, 48f * k), "RCGAccess " + Mod.Version, _stTitle);
                List<string> path = new List<string>();
                foreach (KeyValuePair<List<Entry>, int> kv in _stack)
                    if (kv.Value >= 0 && kv.Value < kv.Key.Count) path.Add(Loc.T(kv.Key[kv.Value].Key));
                string where = path.Count > 0 ? string.Join(" › ", path.ToArray()) : Loc.T("view_main");
                GUI.Label(new Rect(x + w * 0.45f, y, w * 0.55f, 48f * k), where + "   " + Loc.T("pos", _index + 1, _entries.Count), _stPath);
                y += 56f * k;
                GUI.DrawTexture(new Rect(x, y, w, 2f * k), _texLine);
                y += 12f * k;

                // help text of the selected entry (measured first so the list knows its room)
                Entry cur = _entries[_index];
                string help = cur is SubMenu ? null : Loc.TryT(cur.Key + "_help");
                if (help != null && (cur.Key == "m_countbtn" || cur.Key == "m_padkeys")) help += " " + Bindings.UsageNote();
                float keysH = 58f * k;
                float helpH = help != null ? Mathf.Min(_stHelp.CalcHeight(new GUIContent(help), w), 190f * k) : 0f;
                float listH = py + ph - pad - keysH - (helpH > 0 ? helpH + 22f * k : 0f) - y;

                // the entries
                float rowH = 46f * k;
                int rows = Mathf.Max(3, Mathf.FloorToInt(listH / rowH));
                int first = Mathf.Clamp(_index - rows / 2, 0, Mathf.Max(0, _entries.Count - rows));
                for (int i = first; i < _entries.Count && i < first + rows; i++)
                {
                    Entry e = _entries[i];
                    bool sel = i == _index;
                    Rect r = new Rect(x, y + (i - first) * rowH, w, rowH - 4f * k);
                    if (sel) GUI.DrawTexture(r, _texSel);
                    string name = e is SubMenu ? Loc.T(e.Key) + "  ›" : e.Key == "m_voice" ? Loc.T("m_voice", LanguageName(Speech.VoiceLcid() & 0x3FF)) : Loc.T(e.Key);
                    string val = "";
                    try { val = e.Value(); } catch { }
                    Rect rn = new Rect(r.x + 14f * k, r.y, r.width * (val.Length > 0 ? 0.6f : 1f) - 14f * k, r.height);
                    Rect rv = new Rect(r.x + r.width * 0.6f, r.y, r.width * 0.4f - 14f * k, r.height);
                    GUI.Label(rn, (sel ? "► " : "   ") + name, sel ? _stNameSel : _stName);
                    if (val.Length > 0) GUI.Label(rv, val, sel ? _stValueSel : _stValue);
                }
                if (first > 0) GUI.Label(new Rect(x, y - 14f * k, w, 14f * k), "▲", _stKeys);
                if (first + rows < _entries.Count) GUI.Label(new Rect(x, y + rows * rowH - 6f * k, w, 18f * k), "▼", _stKeys);

                // help and controls
                float by = py + ph - pad - keysH;
                if (help != null)
                {
                    GUI.DrawTexture(new Rect(x, by - helpH - 14f * k, w, 2f * k), _texLine);
                    GUI.Label(new Rect(x, by - helpH - 6f * k, w, helpH), help, _stHelp);
                }
                GUI.Label(new Rect(x, by, w, keysH), Loc.T("view_keys"), _stKeys);
            }
            catch (Exception e) { if (!_drawErr) { _drawErr = true; Log.Error("ModMenu.Draw", e); } }
        }
    }
}
