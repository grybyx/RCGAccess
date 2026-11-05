using System;
using System.Reflection;
using RCG;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>What was picked up: items, food, quest items.</summary>
    public static class Pickups
    {
        private static FieldInfo _v2Item = typeof(InteractEntity_DropV2).GetField("_item", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _v2ItemName = typeof(InteractEntity_DropV2).GetField("_itemName", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _dropBehavior = typeof(InteractEntity_Drop).GetField("m_dropBehavior", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _healPct = typeof(InteractEntity_DropHealth).GetField("m_healPercentage", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _qItem = typeof(InteractEntity_DropQuestItem).GetField("_item", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _qItemName = typeof(InteractEntity_DropQuestItem).GetField("_itemName", BindingFlags.Instance | BindingFlags.NonPublic);

        public static string ItemName(Data_Item it, string fallbackKey, string objName)
        {
            if (it != null && !string.IsNullOrEmpty(it.ItemName)) return GameInfo.TranslateOr(it.ItemName, GameInfo.Readable(it.ItemName));
            if (!string.IsNullOrEmpty(fallbackKey)) return GameInfo.TranslateOr(fallbackKey, GameInfo.Readable(fallbackKey));
            return CleanObjName(objName);
        }

        public static string CleanObjName(string n)
        {
            n = GameInfo.Readable(n ?? "");
            foreach (string w in new string[] { "Dropped Item", "Dropped", "Drop V2", "Drop", "Pickup", "Interact Entity", "Prefab", "Yamada" })
                n = n.Replace(w, "");
            n = System.Text.RegularExpressions.Regex.Replace(n, "\\(\\d+\\)|\\d+$", "").Trim();
            return n.Length > 0 ? n : Loc.T("item");
        }

        /// <summary>Name of a lying item for announcements and the navigator; null when it is not worth navigating to.</summary>
        public static string Describe(InteractEntity e, out bool important)
        {
            important = false;
            InteractEntity_DropV2 v2 = e as InteractEntity_DropV2;
            if (v2 != null)
            {
                if (v2.DropType == InteractEntity_DropV2.DropTypes.Monies) return null;
                important = true;
                return ItemName(_v2Item.GetValue(v2) as Data_Item, _v2ItemName.GetValue(v2) as string, v2.name);
            }
            InteractEntity_Drop d = e as InteractEntity_Drop;
            if (d != null)
            {
                InteractEntity_DropBehavior b = _dropBehavior.GetValue(d) as InteractEntity_DropBehavior;
                if (b is InteractEntity_DropHealth)
                {
                    important = !HealthFull(); // food cannot be picked up with full health
                    float pct = _healPct != null ? (float)_healPct.GetValue(b) : 0f;
                    return Loc.T("food", CleanObjName(d.name), Mathf.RoundToInt(pct * 100));
                }
                if (b is InteractEntity_DropQuestItem)
                {
                    important = true;
                    return ItemName(_qItem.GetValue(b) as Data_Item, _qItemName.GetValue(b) as string, d.name);
                }
                return null;
            }
            return null;
        }

        public static bool IsFood(InteractEntity e)
        {
            InteractEntity_Drop d = e as InteractEntity_Drop;
            if (d == null) return false;
            try { return _dropBehavior.GetValue(d) is InteractEntity_DropHealth; } catch { return false; }
        }

        public static bool HealthFull()
        {
            RCG.Player p = GameInfo.Player;
            return p != null && p.Stamina >= p.StartingStamina;
        }

        private static void Announce(string what)
        {
            if (string.IsNullOrEmpty(what)) return;
            Speech.Tone(880, 70, 0, 0.6f, 1, 880, 0);
            Speech.Tone(1175, 70, 0, 0.6f, 1, 1175, 75);
            Speech.Tone(1760, 110, 0, 0.6f, 1, 1760, 150);
            if (Settings.PickupSpeak) Speech.Say(Loc.T("picked", what), !Combat.InFight);
        }

        public static void OnDropV2(InteractEntity_DropV2 d)
        {
            try
            {
                bool imp;
                string s = Describe(d, out imp);
                if (s != null) Announce(s);
            }
            catch (Exception e) { Log.Error("OnDropV2", e); }
        }

        public static void OnDrop(InteractEntity_Drop d)
        {
            try
            {
                bool imp;
                string s = Describe(d, out imp);
                if (s == null) s = CleanObjName(d.name);
                Announce(s);
            }
            catch (Exception e) { Log.Error("OnDrop", e); }
        }
    }
}
