using System;
using System.Collections.Generic;
using System.Reflection;
using RCG;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCGAccess
{
    /// <summary>The phone (the game's pause menu): pages, map, quests, inventory, moves, recruits; reward pop-up.</summary>
    public static class PhoneHelp
    {
        private static bool _phoneWasOpen;
        private static bool _phoneHintGiven;
        private static UI_MenuScreen _lastPage;
        private static float _pageAt = -1f;
        private static bool _openedNow;
        private static bool _extWasOpen;
        private static bool _extFirst;
        private static int _extIdx = -1;
        private static float _extAt = -1f;
        private static bool _questWasOpen;
        private static float _questAt = -1f;
        private static UI_PhoneScreen_MapV2 _map;

        // ---------------- reward pop-up ----------------
        /// <summary>UI_ItemPickup.SetWhatYouGotText: one complete line per reward ("1000 Erfahrung"), with how to go on.</summary>
        public static void OnReward(UI_ItemPickup ip, int idx)
        {
            try
            {
                System.Collections.IList list = UiReader.Traverse(ip, "_currentDisplay") as System.Collections.IList;
                if (list == null || idx < 0 || idx >= list.Count) return;
                UI_ItemPickup.ItemPickupDisplay d = list[idx] as UI_ItemPickup.ItemPickupDisplay;
                if (d == null) return;
                TMP_Text what = UiReader.Traverse(ip, "_whatYouGotText") as TMP_Text;
                TMP_Text you = UiReader.Traverse(ip, "_youGotText") as TMP_Text;
                string label = what != null ? GameInfo.Clean(what.text) : "";
                string line;
                switch (d.DisplayType)
                {
                    case UI_ItemPickup.ItemPickupDisplayType.Money: line = Loc.T("reward_money", d.DisplayDataAmount); break;
                    case UI_ItemPickup.ItemPickupDisplayType.XP: line = d.DisplayDataAmount + " " + (label.Length > 0 ? label : Loc.T("reward_xp")); break;
                    default: line = label; break;
                }
                if (line.Length == 0) return;
                string s;
                if (idx == 0)
                {
                    string head = you != null ? GameInfo.Clean(you.text).TrimEnd(':', ' ') : "";
                    s = Loc.T("reward_first", head.Length > 0 ? head : Loc.T("reward_head"), line)
                        + (list.Count > 1 ? " " + Loc.T("reward_of", 1, list.Count) : "") + " " + Loc.T("reward_next", GameInfo.ButtonName(0));
                }
                else s = line + (list.Count > 1 ? ", " + Loc.T("reward_of", idx + 1, list.Count) : "");
                Log.Text("Belohnung", s);
                if (Settings.Hints || Settings.Menus) Speech.Say(s, idx == 0 ? false : true);
            }
            catch (Exception e) { Log.Error("OnReward", e); }
        }

        /// <summary>Map markers shown after a new quest (the map pops up by itself and waits for A or B).</summary>
        public static void OnMapIndicators()
        {
            try
            {
                if (UI_MapIndicator.Instance == null) return;
                List<string> scenes = UI_MapIndicator.Instance.MapScenesToIndicate;
                if (scenes == null || scenes.Count == 0) return;
                List<string> names = new List<string>();
                foreach (string sc in scenes)
                {
                    string n = GameInfo.RoomName(sc);
                    if (!names.Contains(n)) names.Add(n);
                }
                string s = Loc.T("map_new_marks", string.Join(", ", names.ToArray())) + " " + Loc.T("reward_next", GameInfo.ButtonName(0));
                Log.Text("Karte", s);
                Speech.Say(s, false);
            }
            catch (Exception e) { Log.Error("OnMapIndicators", e); }
        }

        // ---------------- map data ----------------
        private static MethodInfo _mdmCur = typeof(MapDataManager).GetMethod("GetCurrentScene", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo _mdmUpd = typeof(MapDataManager).GetMethod("UpdateMapData", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>The game only refreshes the map markers when the phone map is opened; do it without side effects.</summary>
        private static float _refreshAt = -10f;

        public static void RefreshMapData()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                if (now - _refreshAt < 1f) return;
                _refreshAt = now;
                MapDataManager mdm = MapDataManager.Instance;
                if (mdm == null || mdm.MapData == null || _mdmUpd == null) return;
                if (_mdmCur != null) _mdmCur.Invoke(mdm, null);
                object[] a = new object[1];
                for (int i = 0; i < mdm.MapData.Count; i++) { a[0] = i; _mdmUpd.Invoke(mdm, a); }
            }
            catch (Exception e) { Log.Error("RefreshMapData", e); }
        }

        private static void Marks(string scene, out bool ex, out bool qu)
        {
            ex = false; qu = false;
            MapDataManager mdm = MapDataManager.Instance;
            if (mdm == null || mdm.MapData == null) return;
            foreach (MapData md in mdm.MapData)
            {
                if (md == null || md.Icons == null || md.SceneName == null || !md.SceneName.Equals(scene, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (MapData.IconData ic in md.Icons)
                {
                    if (ic == null || ic.Icon == null || !ic.ShowingIcon || !ic.ShowRequirementsMet) continue;
                    string n = ic.Icon.name.ToLower();
                    if (n.Contains("exclamation")) ex = true;
                    else if (n.Contains("question")) qu = true;
                }
            }
        }

        private static string Norm(string s)
        {
            return (s ?? "").ToLower().Replace("_", "").Replace(" ", "");
        }

        /// <summary>Index into the extended map's worlds for an overworld entry (its object name holds the world prefix).</summary>
        private static int WorldIndex(UI_PhoneScreen_ExtendedMap ext, string name, int fallback)
        {
            if (ext == null || ext._worlds == null) return fallback;
            string n = Norm(name);
            for (int k = 0; k < ext._worlds.Length; k++)
            {
                string pre = Norm(ext._worlds[k].WorldPrefix);
                if (pre.Length > 0 && n.Contains(pre)) return k;
            }
            return fallback < ext._worlds.Length ? fallback : -1;
        }

        private static string WorldName(UI_PhoneScreen_ExtendedMap ext, int k)
        {
            try
            {
                if (ext != null && ext._worldTabs != null && k >= 0 && k < ext._worldTabs.Length && ext._worldTabs[k] != null)
                {
                    string key = UiReader.Traverse(ext._worldTabs[k], "_worldNameKey") as string;
                    string t = GameInfo.Translate(key);
                    if (!string.IsNullOrEmpty(t) && t != "null") return t;
                }
            }
            catch { }
            return Loc.T("phone_world", k + 1);
        }

        /// <summary>"You are here: …. Mission: …. 4 of 12 rooms visited."</summary>
        private static string WorldSummary(UI_PhoneScreen_ExtendedMap ext, int k)
        {
            if (ext == null || ext._worlds == null || k < 0 || k >= ext._worlds.Length) return "";
            RefreshMapData();
            string cur = SceneManager.GetActiveScene().name;
            int seen = 0, total = 0;
            string here = null;
            List<string> ex = new List<string>(), qu = new List<string>();
            foreach (UI_PhoneScreen_ExtendedMap.MapLevelRoom r in ext._worlds[k].Rooms)
            {
                if (r == null || string.IsNullOrEmpty(r.RoomName) || r.RoomName.IndexOf("Cutscene", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                total++;
                bool beenThere = false;
                try { beenThere = PlayerGlobalInventory.instance != null && PlayerGlobalInventory.instance.HaveYouSeenScene(r.RoomName); } catch { }
                if (beenThere) seen++;
                string name = GameInfo.RoomName(r.RoomName);
                if (r.RoomName.Equals(cur, StringComparison.OrdinalIgnoreCase)) here = name;
                bool e, q;
                Marks(r.RoomName, out e, out q);
                if (e && !ex.Contains(name)) ex.Add(name);
                if (q && !qu.Contains(name)) qu.Add(name);
            }
            List<string> parts = new List<string>();
            if (here != null) parts.Add(Loc.T("phone_here", here));
            if (ex.Count > 0) parts.Add(Loc.T("phone_marks_ex", string.Join(", ", ex.ToArray())));
            if (qu.Count > 0) parts.Add(Loc.T("phone_marks_qu", string.Join(", ", qu.ToArray())));
            if (total > 0) parts.Add(Loc.T("phone_seen", seen, total));
            return string.Join(". ", parts.ToArray());
        }

        // ---------------- focus texts ----------------
        /// <summary>Specific text for entries on the phone pages; null for everything else.</summary>
        public static string FocusText(Component sel)
        {
            try
            {
                if (sel == null) return null;
                string setting = SettingText(sel);
                if (setting != null) return setting;
                UI_PhoneScreen_MapV2 map = sel.GetComponentInParent<UI_PhoneScreen_MapV2>();
                if (map != null)
                {
                    _map = map;
                    UI_OverworldMapInputSelectable[] ow = UiReader.Traverse(map, "_overworldMapSelectables") as UI_OverworldMapInputSelectable[];
                    if (ow != null)
                        for (int i = 0; i < ow.Length; i++)
                        {
                            if (ow[i] == null || ow[i].Selectable != sel) continue;
                            UI_PhoneScreen_ExtendedMap ext = UiReader.Traverse(map, "_extendedMap") as UI_PhoneScreen_ExtendedMap;
                            int k = WorldIndex(ext, ow[i].name, i);
                            string name = WorldName(ext, k);
                            if (!ow[i].CanExplore()) return name + ", " + Loc.T("phone_world_locked");
                            string sum = WorldSummary(ext, k);
                            return name + (sum.Length > 0 ? ". " + sum : "") + ". " + Loc.T("phone_world_open", GameInfo.ButtonName(0));
                        }
                    UI_QuestItemDisplayV2 q = sel.GetComponentInParent<UI_QuestItemDisplayV2>();
                    if (q != null)
                    {
                        TMP_Text qn = UiReader.Traverse(q, "_collapsedQuestNameText") as TMP_Text;
                        TMP_Text ql = UiReader.Traverse(q, "_collapsedQuestLocationText") as TMP_Text;
                        GameObject done = UiReader.Traverse(q, "_collapsedQuestCheckmark") as GameObject;
                        string s = Loc.T("phone_quest", qn != null ? GameInfo.Clean(qn.text) : "");
                        if (ql != null && GameInfo.Clean(ql.text).Length > 0) s += ", " + Loc.T("phone_quest_loc", GameInfo.Clean(ql.text));
                        if (done != null && done.activeSelf) s += ", " + Loc.T("phone_quest_done");
                        return s + ". " + Loc.T("phone_quest_open", GameInfo.ButtonName(0));
                    }
                    return null;
                }
                UI_PhoneScreen_CharacterV2 ch = sel.GetComponentInParent<UI_PhoneScreen_CharacterV2>();
                if (ch != null)
                {
                    UI_InventoryItemDisplay slot = sel.GetComponent<UI_InventoryItemDisplay>();
                    if (slot == null) return null;
                    int no = SlotNumber(ch, sel);
                    string head = Loc.T("phone_slot", no);
                    if (!slot.Unlocked) return head + ", " + Loc.T("phone_slot_locked");
                    TMP_Text n = UiReader.Traverse(ch, "_descItemName") as TMP_Text;
                    string name = n != null ? GameInfo.Clean(n.text) : "";
                    if (name.Length == 0) return head + ", " + Loc.T("phone_slot_empty");
                    TMP_Text d = UiReader.Traverse(ch, "_descItemDescription") as TMP_Text;
                    TMP_Text b = UiReader.Traverse(ch, "_descItemBonus") as TMP_Text;
                    string bonus = b != null ? GameInfo.Clean(b.text) : "";
                    if (bonus.Contains("???")) bonus = Loc.T("phone_slot_unknown");
                    else bonus = bonus.Replace("ST +", Loc.T("phone_stamina") + " +");
                    string s = head + ": " + name;
                    if (d != null && GameInfo.Clean(d.text).Length > 0) s += ". " + GameInfo.Clean(d.text);
                    if (bonus.Length > 0) s += ". " + bonus;
                    return s + ". " + Loc.T("phone_slot_use", GameInfo.ButtonName(0));
                }
                UI_MovesItemDisplay mv = sel.GetComponent<UI_MovesItemDisplay>();
                if (mv != null) return MoveText(mv);
                UI_PhoneScreen_Recruits rc = sel.GetComponentInParent<UI_PhoneScreen_Recruits>();
                if (rc != null)
                {
                    TMP_Text n = UiReader.Traverse(rc, "_characterNameText") as TMP_Text;
                    TMP_Text d = UiReader.Traverse(rc, "_characterDescriptionText") as TMP_Text;
                    string name = n != null && n.isActiveAndEnabled ? GameInfo.Clean(n.text) : "";
                    if (name.Length == 0) return null;
                    string desc = d != null && d.isActiveAndEnabled ? GameInfo.Clean(d.text) : "";
                    return name + (desc.Length > 0 ? ". " + desc : "");
                }
            }
            catch (Exception e) { Log.Error("PhoneHelp.FocusText", e); }
            return null;
        }

        private static int SlotNumber(UI_MenuScreen screen, Component sel)
        {
            try
            {
                List<UI_InputSelectable> list = UiReader.Traverse(screen, "_selectables") as List<UI_InputSelectable>;
                if (list != null) { int i = list.IndexOf(sel as UI_InputSelectable); if (i >= 0) return i + 1; }
            }
            catch { }
            return sel.transform.GetSiblingIndex() + 1;
        }

        /// <summary>Move name, type and the full button sequence (the game shows the buttons as icons).</summary>
        private static string MoveText(UI_MovesItemDisplay mv)
        {
            List<string> parts = new List<string>();
            TMP_Text title = UiReader.Traverse(mv, "_textTitle") as TMP_Text;
            TMP_Text type = UiReader.Traverse(mv, "_textType") as TMP_Text;
            if (title != null) parts.Add(GameInfo.Clean(title.text));
            if (type != null && GameInfo.Clean(type.text).Length > 0) parts.Add(GameInfo.Clean(type.text));
            List<string> seq = new List<string>();
            UI_MovesItemInput[] inputs = UiReader.Traverse(mv, "_spawnedInputs") as UI_MovesItemInput[];
            if (inputs != null)
                foreach (UI_MovesItemInput mi in inputs)
                {
                    if (mi == null) continue;
                    TMP_Text it = UiReader.Traverse(mi, "_inputText") as TMP_Text;
                    string s = it != null ? GameInfo.Clean(it.text) : "";
                    if (s.Length == 0) s = Loc.T("move_dash");
                    else if (s == "+") s = Loc.T("move_plus");
                    else if (s == ",") s = Loc.T("move_then");
                    seq.Add(s);
                }
            string r = string.Join(", ", parts.ToArray());
            if (seq.Count > 0) r += ": " + string.Join(" ", seq.ToArray()).Replace(" ,", ",");
            return r;
        }


        // ---------------- game settings (phone and title screen) ----------------
        /// <summary>On/off switches, text speed and volume sliders of the game's settings: name plus the chosen value.</summary>
        private static string SettingText(Component sel)
        {
            UI_SettingsSelector ss = sel.GetComponent<UI_SettingsSelector>();
            if (ss == null) ss = sel.GetComponentInChildren<UI_SettingsSelector>();
            if (ss == null || ss is UI_SettingsLanguage) return null;
            string value = null;
            List<TMP_Text> skip = new List<TMP_Text>();
            if (ss is UI_SettingsSubtitles || ss is UI_SettingsFriendlyFire || ss is UI_SettingsRumble)
            {
                TMP_Text on = UiReader.Traverse(ss, "_onText") as TMP_Text;
                TMP_Text off = UiReader.Traverse(ss, "_offText") as TMP_Text;
                if (on != null) skip.Add(on);
                if (off != null) skip.Add(off);
                bool v = ss is UI_SettingsSubtitles ? ((UI_SettingsSubtitles)ss).ShowSubtitles
                       : ss is UI_SettingsFriendlyFire ? ((UI_SettingsFriendlyFire)ss).FriendlyFire
                       : ((UI_SettingsRumble)ss).RumbleOn;
                TMP_Text chosen = v ? on : off;
                value = chosen != null ? GameInfo.Clean(chosen.text) : "";
                if (value.Length == 0) value = Loc.T(v ? "on" : "off");
            }
            else if (ss is UI_SettingsTextSpeed)
            {
                value = Loc.T("speed_" + (int)((UI_SettingsTextSpeed)ss).TextSpeed);
            }
            else if (ss is UI_SettingsSlider)
            {
                UnityEngine.UI.Slider sl = UiReader.Traverse(ss, "_slider") as UnityEngine.UI.Slider;
                if (sl == null) return null;
                float range = sl.maxValue - sl.minValue;
                int pct = range > 0 ? Mathf.RoundToInt((sl.value - sl.minValue) / range * 100f) : 0;
                value = pct + " %";
            }
            else return null;
            // name of the setting: the highlighted label, else the other texts of the entry
            string label = "";
            TMP_Text lt = UiReader.Traverse(ss, "_selectedText") as TMP_Text;
            if (lt != null && !skip.Contains(lt)) label = GameInfo.Clean(lt.text);
            if (label.Length == 0)
            {
                List<string> parts = new List<string>();
                foreach (TMP_Text t in sel.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (skip.Contains(t) || !t.isActiveAndEnabled) continue;
                    string c = GameInfo.Clean(t.text);
                    if (c.Length > 0 && !parts.Contains(c)) parts.Add(c);
                }
                label = string.Join(", ", parts.ToArray());
            }
            return label.Length > 0 ? label + ": " + value : value;
        }

        // ---------------- shop ----------------
        private static UI_StoreScreenV2 _store;
        private static float _storeScanAt;
        private static float _storeMoney = -1f;

        /// <summary>Says the money when a shop opens and what is left after buying.</summary>
        private static void UpdateStore(float now)
        {
            if (now >= _storeScanAt)
            {
                _storeScanAt = now + 0.3f;
                _store = null;
                foreach (UI_StoreScreenV2 st in UnityEngine.Object.FindObjectsOfType<UI_StoreScreenV2>())
                    if (st != null && st.isActiveAndEnabled) { _store = st; break; }
            }
            TMP_Text mt = _store != null ? UiReader.Traverse(_store, "_playerMoneyText") as TMP_Text : null;
            if (_store == null || mt == null || !mt.isActiveAndEnabled || !GameInfo.OnScreen(mt.rectTransform)) { _storeMoney = -1f; return; }
            RCG.Player p = GameInfo.Player;
            if (p == null) return;
            float m = Combat.Money(p);
            if (m < 0) return;
            if (_storeMoney < 0)
            {
                _storeMoney = m;
                string s = Loc.T("shop_money", Combat.FormatMoney(m));
                Log.Text("Laden", s);
                Speech.Say(s, false);
            }
            else if (m < _storeMoney - 0.001f)
            {
                _storeMoney = m;
                string s = Loc.T("shop_left", Combat.FormatMoney(m));
                Log.Text("Laden", s);
                Speech.Say(s, false);
            }
            else if (m > _storeMoney) _storeMoney = m;
        }

        // ---------------- phone pages ----------------
        private static UI_PhoneDisplayManager Display()
        {
            foreach (UI_PhoneDisplayManager d in UnityEngine.Object.FindObjectsOfType<UI_PhoneDisplayManager>())
                if (d != null && d.PlayerNumber == 0) return d;
            return null;
        }

        private static string NavText(UI_PhoneDisplayManager d, string name)
        {
            foreach (TMP_Text t in d.GetComponentsInChildren<TMP_Text>(false))
                if (t.name == name && t.isActiveAndEnabled && t.color.a > 0.05f) return GameInfo.Clean(t.text);
            return "";
        }

        public static void OnPhoneOpened()
        {
            _openedNow = true;
            _lastPage = null;
        }

        /// <summary>Called every frame (not while the mod menu is open).</summary>
        public static void Update()
        {
            try
            {
                UpdateStore(Time.realtimeSinceStartup);
                bool open = UI_PhoneManager.instance != null && UI_PhoneManager.instance.Open;
                if (!open)
                {
                    _phoneWasOpen = false; _lastPage = null; _extWasOpen = false; _questWasOpen = false; _extIdx = -1;
                    return;
                }
                float now = Time.realtimeSinceStartup;
                if (!_phoneWasOpen) { _phoneWasOpen = true; _openedNow = true; _lastPage = null; }
                UI_PhoneDisplayManager d = Display();
                if (d == null || d.ScreenManager == null) return;
                UI_MenuScreen page = d.ScreenManager.CurrentScreen;
                if (page != null && page != _lastPage)
                {
                    _lastPage = page;
                    _pageAt = now;
                }
                // page hint shortly after the page has been built (the page title is read by the menu reader)
                if (_pageAt > 0 && now - _pageAt > 0.35f)
                {
                    _pageAt = -1f;
                    List<string> parts = new List<string>();
                    if (_openedNow) parts.Add(Loc.T("phone_open"));
                    string prev = NavText(d, "PreviousScreen"), next = NavText(d, "NextScreen");
                    if (prev.Length > 0) parts.Add(Loc.T("phone_prev", prev));
                    if (next.Length > 0) parts.Add(Loc.T("phone_next", next));
                    UI_PhoneScreen_CharacterV2 ch = page as UI_PhoneScreen_CharacterV2;
                    if (ch != null) { string lv = LevelText(); if (lv.Length > 0) parts.Insert(0, lv); }
                    if (_openedNow && !_phoneHintGiven) { parts.Add(Loc.T("phone_close", "Start")); _phoneHintGiven = true; }
                    _openedNow = false;
                    if (parts.Count > 0 && Settings.Menus)
                    {
                        string s = string.Join(". ", parts.ToArray()) + ".";
                        Log.Text("Handy", s);
                        Speech.Say(s, false);
                    }
                }
                // extended world map and quest details on the map page
                UI_PhoneScreen_MapV2 map = page as UI_PhoneScreen_MapV2;
                if (map == null) { _extWasOpen = false; _questWasOpen = false; return; }
                _map = map;
                bool ext = UI_PhoneScreen_MapV2._mapOpen;
                int idx = UiReader.Traverse(map, "_currentExtendedMap") is int ? (int)UiReader.Traverse(map, "_currentExtendedMap") : -1;
                if (ext && !_extWasOpen) { _extWasOpen = true; _extFirst = true; _extIdx = idx; _extAt = now; }
                else if (ext && idx != _extIdx) { _extIdx = idx; _extAt = now; }
                if (ext && _extAt > 0 && now - _extAt > 0.4f && !UI_PhoneScreen_MapV2._mapAnimating)
                {
                    _extAt = -1f;
                    SpeakWorldMap(map, idx, _extFirst);
                    _extFirst = false;
                }
                if (!ext) { _extWasOpen = false; _extAt = -1f; }
                bool qOpen = UiReader.Traverse(map, "_questOpen") is bool && (bool)UiReader.Traverse(map, "_questOpen");
                bool qTrans = UiReader.Traverse(map, "_questTransitioning") is bool && (bool)UiReader.Traverse(map, "_questTransitioning");
                if (qOpen && !_questWasOpen) { _questWasOpen = true; _questAt = now; }
                if (!qOpen) _questWasOpen = false;
                if (_questAt > 0 && !qTrans && now - _questAt > 0.3f)
                {
                    _questAt = -1f;
                    SpeakQuest(map);
                }
            }
            catch (Exception e) { Log.Error("PhoneHelp.Update", e); }
        }

        private static string LevelText()
        {
            try
            {
                RCG.Player p = GameInfo.Player;
                if (p == null || PlayerAttributes.Instance == null) return "";
                int c = (int)p.ClassNameToPlayerCharacter;
                PlayerAttributes a = PlayerAttributes.Instance;
                return Loc.T("phone_level", a.CurrentLevel(c), a.CurrentXPAmount(c), a.NextXPAmount(c));
            }
            catch { return ""; }
        }

        private static void SpeakWorldMap(UI_PhoneScreen_MapV2 map, int idx, bool first)
        {
            UI_OverworldMapInputSelectable[] ow = UiReader.Traverse(map, "_overworldMapSelectables") as UI_OverworldMapInputSelectable[];
            UI_PhoneScreen_ExtendedMap ext = UiReader.Traverse(map, "_extendedMap") as UI_PhoneScreen_ExtendedMap;
            int k = (ow != null && idx >= 0 && idx < ow.Length) ? WorldIndex(ext, ow[idx].name, idx) : idx;
            string s = Loc.T("phone_worldmap", WorldName(ext, k));
            string sum = WorldSummary(ext, k);
            if (sum.Length > 0) s += ". " + sum;
            if (first) s += ". " + Loc.T("phone_worldmap_keys");
            Log.Text("Karte", s);
            if (Settings.Menus) Speech.Say(s, !first);
        }

        private static void SpeakQuest(UI_PhoneScreen_MapV2 map)
        {
            List<string> parts = new List<string>();
            foreach (string f in new string[] { "_expandedQuestTitleText", "_expandedQuestLocationText", "_expandedQuestDescriptionText", "_expandedQuestRequirementsText" })
            {
                TMP_Text t = UiReader.Traverse(map, f) as TMP_Text;
                if (t == null || !t.isActiveAndEnabled) continue;
                string s = GameInfo.Clean(t.text);
                if (s.Length > 0 && !parts.Contains(s)) parts.Add(s);
            }
            if (parts.Count == 0) return;
            parts.Add(Loc.T("phone_quest_back"));
            string all = string.Join(". ", parts.ToArray());
            Log.Text("Aufgabe", all);
            if (Settings.Menus) Speech.Say(all, true);
        }
    }
}
