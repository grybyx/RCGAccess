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

        private class ActionEntry : Entry
        {
            public Action Run;
            public override string Value() { return ""; }
            public override void Activate() { Run(); }
            public override bool IsAction { get { return true; } }
        }

        private static readonly List<Entry> _entries = new List<Entry>();
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
            _entries.Clear();
            _entries.Add(new Choice
            {
                Key = "m_modlang",
                Options = delegate { return new List<string> { Loc.T("lang_auto"), "Deutsch", "English" }; },
                Get = delegate { return Settings.ModLang; },
                Set = delegate (int v) { Settings.ModLang = v; Speech.ApplyLanguage(true); }
            });
            _entries.Add(VoiceChoice("m_voice_de", 0x07, true));
            _entries.Add(VoiceChoice("m_voice_en", 0x09, false));
            _entries.Add(new Number
            {
                Key = "m_rate", Min = -5, Max = 10, Step = 1,
                Get = delegate { return Settings.Rate; },
                Set = delegate (int v) { Settings.Rate = v; Speech.Config("rate", v.ToString()); }
            });
            _entries.Add(new Number
            {
                Key = "m_speechvol", Min = 10, Max = 100, Step = 10, Unit = "%",
                Get = delegate { return Settings.SpeechVolume; },
                Set = delegate (int v) { Settings.SpeechVolume = v; Speech.Config("volume", v.ToString()); }
            });
            _entries.Add(new Choice
            {
                Key = "m_damage",
                Options = delegate { return new List<string> { Loc.T("off"), Loc.T("dmg_half"), Loc.T("dmg_none") }; },
                Get = delegate { return Settings.DamageHelp; },
                Set = delegate (int v) { Settings.DamageHelp = v; }
            });
            AddToggle("m_menus", () => Settings.Menus, v => Settings.Menus = v);
            AddToggle("m_dialogs", () => Settings.Dialogs, v => Settings.Dialogs = v);
            AddToggle("m_chatter", () => Settings.Chatter, v => Settings.Chatter = v);
            AddToggle("m_hints", () => Settings.Hints, v => Settings.Hints = v);
            AddToggle("m_interact", () => Settings.Interact, v => Settings.Interact = v);
            AddToggle("m_radar", () => Settings.Radar, v => Settings.Radar = v);
            _entries.Add(new Number
            {
                Key = "m_knockvol", Min = 0, Max = 200, Step = 10, Unit = "%",
                Get = delegate { return Settings.KnockVolume; },
                Set = delegate (int v) { Settings.KnockVolume = v; },
                Preview = delegate { Combat.PreviewKnock(); }
            });
            _entries.Add(new Number
            {
                Key = "m_radarrange", Min = 4, Max = 20, Step = 1, Unit = Loc.T("unit_m"),
                Get = delegate { return Settings.RadarRange; },
                Set = delegate (int v) { Settings.RadarRange = v; }
            });
            AddToggle("m_enemyannounce", () => Settings.EnemyAnnounce, v => Settings.EnemyAnnounce = v);
            AddToggle("m_attackwarn", () => Settings.AttackWarn, v => Settings.AttackWarn = v);
            AddToggle("m_health", () => Settings.HealthTones, v => Settings.HealthTones = v);
            AddToggle("m_boss", () => Settings.BossHealth, v => Settings.BossHealth = v);
            AddToggle("m_room", () => Settings.RoomAnnounce, v => Settings.RoomAnnounce = v);
            AddToggle("m_deadend", () => Settings.DeadEnd, v => Settings.DeadEnd = v);
            AddToggle("m_hazards", () => Settings.Hazards, v => Settings.Hazards = v);
            AddToggle("m_pickupspeak", () => Settings.PickupSpeak, v => Settings.PickupSpeak = v);
            AddToggle("m_itemnav", () => Settings.ItemNav, v => Settings.ItemNav = v);
            _entries.Add(new Choice
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
            AddToggle("m_moneytones", () => Settings.MoneyTones, v => Settings.MoneyTones = v);
            AddToggle("m_moneyspeak", () => Settings.MoneySpeak, v => Settings.MoneySpeak = v);
            _entries.Add(new Number
            {
                Key = "m_tonevol", Min = 0, Max = 100, Step = 10, Unit = "%",
                Get = delegate { return Settings.ToneVolume; },
                Set = delegate (int v) { Settings.ToneVolume = v; },
                Preview = delegate { Navigator.PreviewBeacon(); }
            });
            AddToggle("m_padkeys", () => Settings.PadKeys, v => Settings.PadKeys = v);
            AddToggle("m_autowalkstop", () => Settings.AutoWalkStopForEnemies, v => Settings.AutoWalkStopForEnemies = v);
            AddToggle("m_walkrun", () => Settings.WalkRun, v => Settings.WalkRun = v);
            AddToggle("m_autodoor", () => Settings.AutoDoor, v => Settings.AutoDoor = v);
            AddToggle("m_duckspeech", () => Settings.DuckSpeech, v => Settings.DuckSpeech = v);
            AddToggle("m_ducktones", () => Settings.DuckTones, v => Settings.DuckTones = v);
            _entries.Add(new Number
            {
                Key = "m_ducklevel", Min = 0, Max = 100, Step = 10, Unit = "%",
                Get = delegate { return Settings.DuckLevel; },
                Set = delegate (int v) { Settings.DuckLevel = v; }
            });
            AddToggle("m_verbose", () => Settings.VerboseLog, v => Settings.VerboseLog = v);
            AddToggle("m_everystart", () => Settings.MenuEveryStart, v => Settings.MenuEveryStart = v);
            _entries.Add(new ActionEntry { Key = "m_donation", Run = delegate { OpenUrl("https://buymeacoffee.com/blindretrogamer/", Loc.T("m_donation")); } });
            _entries.Add(new ActionEntry { Key = "m_grybyx", Run = delegate { OpenUrl("https://github.com/grybyx/", Loc.T("m_grybyx")); } });
            _entries.Add(new ActionEntry { Key = "m_save", Run = delegate { Close(true); } });
            _entries.Add(new ActionEntry { Key = "m_discard", Run = delegate { Close(false); } });
        }

        private static void AddToggle(string key, Func<bool> get, Action<bool> set)
        {
            _entries.Add(new Toggle { Key = key, Get = get, Set = set });
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

        private static Entry VoiceChoice(string key, int prim, bool german)
        {
            Func<List<string>> names = delegate
            {
                List<string> o = new List<string> { Loc.T("voice_auto") };
                foreach (KeyValuePair<string, int> v in ReadVoices())
                    if ((v.Value & 0x3FF) == prim) o.Add(v.Key);
                return o;
            };
            return new Choice
            {
                Key = key,
                Options = names,
                Get = delegate
                {
                    string cur = german ? Settings.VoiceDe : Settings.VoiceEn;
                    List<string> o = names();
                    int i = string.IsNullOrEmpty(cur) ? 0 : o.IndexOf(cur);
                    return i < 0 ? 0 : i;
                },
                Set = delegate (int i)
                {
                    List<string> o = names();
                    string v = (i <= 0 || i >= o.Count) ? "" : o[i];
                    if (german) Settings.VoiceDe = v; else Settings.VoiceEn = v;
                    Speech.ApplyLanguage(true);
                }
            };
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
        private static string EntryName(Entry e) { return Loc.T(e.Key); }

        private static void SpeakEntry(bool interrupt)
        {
            Entry e = _entries[_index];
            string v = e.Value();
            string s = EntryName(e) + (v.Length > 0 ? ": " + v : "") + ". " + Loc.T("pos", _index + 1, _entries.Count);
            Speech.Say(s, interrupt);
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
                Close(false);
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
    }
}
