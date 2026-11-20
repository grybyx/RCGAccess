using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace RCGAccess
{
    /// <summary>Groups of sounds with their own volume in the mod menu.</summary>
    public enum Snd { Radar, Line, Boss, Attack, Health, Fight, Recruit, Beacon, Interact, Item, Wall, Hazard, Pickup, Money }

    /// <summary>All switches and values of the mod (RCGAccess\RCGAccess.ini).</summary>
    public static class Settings
    {
        public const int CurrentMenuVersion = 14;

        // Funktionen
        public static bool Menus = true;
        public static bool Dialogs = true;
        public static bool Chatter = false;
        public static bool Hints = true;
        public static bool Interact = true;
        public static bool Radar = true;
        public static bool EnemyAnnounce = true;
        public static bool AttackWarn = true;
        public static bool HealthTones = true;
        public static bool BossHealth = true;
        public static bool RoomAnnounce = true;
        public static bool DeadEnd = true;
        public static bool Hazards = true;
        public static bool MoneyTones = true;
        public static bool MoneySpeak = false;
        public static bool PickupSpeak = true;
        public static bool ItemNav = true;
        public static int DamageHelp = 0;        // 0 aus, 1 halber Schaden, 2 unverwundbar
        public static int BossDamage = 0;        // 0 normal, 1 doppelt, 2 vierfach, 3 ein Treffer pro Phase
        public static bool BossAssist = false;   // boss mechanics succeed (Hibari's homing needle always hits)
        public static int EnemyCountButton = 0; // 0 automatisch, 1 LB, 2 RB, 3 LT, 4 RT, 5 Back, 6 aus
        public static bool PadKeys = true;
        public static bool AutoWalkStopForEnemies = true;
        public static bool WalkRun = true;   // guided walking runs on long straight stretches
        public static bool AutoDoor = true;  // guided walking opens the door it arrives at
        public static bool LineTone = true;
        public static bool MenuSamples = true; // mod menu plays the sound an entry is about  // bright click when the nearest enemy is on your line (same depth)
        public static bool DuckSpeech = true;
        public static bool DuckTones = false;

        // Werte
        public static int ModLang = 0;          // 0 automatisch, 1 Deutsch, 2 English
        public static bool RecruitInfo = true;
        public static bool LsWalk = true;
        // own volume (percent, 0 to 200) for each group of sounds
        public static int LineVolume = 100, BossVolume = 100, AttackVolume = 100, HealthVolume = 100, FightVolume = 100,
            RecruitVolume = 100, BeaconVolume = 100, InteractVolume = 100, ItemVolume = 100, WallVolume = 100,
            HazardVolume = 100, PickupVolume = 100, MoneyVolume = 100;

        public static int GroupVolume(Snd g)
        {
            switch (g)
            {
                case Snd.Radar: return KnockVolume;
                case Snd.Line: return LineVolume;
                case Snd.Boss: return BossVolume;
                case Snd.Attack: return AttackVolume;
                case Snd.Health: return HealthVolume;
                case Snd.Fight: return FightVolume;
                case Snd.Recruit: return RecruitVolume;
                case Snd.Beacon: return BeaconVolume;
                case Snd.Interact: return InteractVolume;
                case Snd.Item: return ItemVolume;
                case Snd.Wall: return WallVolume;
                case Snd.Hazard: return HazardVolume;
                case Snd.Pickup: return PickupVolume;
                case Snd.Money: return MoneyVolume;
            }
            return 100;
        }

        public static bool BossBeacon = true;   // own locator sound for bosses (and Hibari's needle)
        public static bool BossMoves = true;    // announce boss moves and phases
        public static bool RadarHeight = false; // off-line radar tone a little higher/lower for enemies above/below       // left stick press = automatic guiding (instead of the game's taunt), right stick press = guided navigation  // feedback for calling the recruit (LT)
        public static int SpeechEngine = 0;     // 0 OneCore (Prism), 1 SAPI, 2 Screenreader (Prism)
        // chosen voice per language (primary language id in hex): "7:Microsoft Katja;9:Microsoft Zira"
        public static string VoicesSapi = "";
        public static string VoicesOneCore = "";
        public static int Rate = 2;
        public static int SpeechVolume = 100;
        public static int ToneVolume = 70;
        public static int KnockVolume = 100;
        public static int RadarRange = 10;
        public static int DuckLevel = 60;

        // Feinheiten
        public static bool VerboseLog = false;
        public static bool MenuEveryStart = false;
        public static int MenuVersion = 0;

        private static string FilePath { get { return Path.Combine(Mod.DataDir, "RCGAccess.ini"); } }

        public static bool FirstStartPending()
        {
            return MenuVersion < CurrentMenuVersion || MenuEveryStart || !File.Exists(FilePath);
        }

        private static IEnumerable<FieldInfo> Fields()
        {
            foreach (FieldInfo f in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsLiteral) continue;
                yield return f;
            }
        }

        public static Dictionary<string, object> Snapshot()
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (FieldInfo f in Fields()) d[f.Name] = f.GetValue(null);
            return d;
        }

        public static void Restore(Dictionary<string, object> d)
        {
            foreach (FieldInfo f in Fields())
            {
                object v;
                if (d.TryGetValue(f.Name, out v)) f.SetValue(null, v);
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) { Log.Write("Keine Einstellungen gefunden, Standardwerte."); return; }
                Dictionary<string, FieldInfo> map = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (FieldInfo f in Fields()) map[f.Name] = f;
                foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("[")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    FieldInfo fi;
                    if (!map.TryGetValue(k, out fi)) { Legacy(k, v); continue; }
                    try
                    {
                        if (fi.FieldType == typeof(bool)) fi.SetValue(null, v == "1" || v.ToLower() == "true" || v.ToLower() == "an");
                        else if (fi.FieldType == typeof(int)) fi.SetValue(null, int.Parse(v, CultureInfo.InvariantCulture));
                        else if (fi.FieldType == typeof(string)) fi.SetValue(null, v);
                    }
                    catch { }
                }
                Log.Write("Einstellungen geladen.");
            }
            catch (Exception e) { Log.Error("Settings.Load", e); }
        }

        /// <summary>Settings of older versions (voice per German/English up to 0.3.5).</summary>
        private static void Legacy(string k, string v)
        {
            if (v.Length == 0) return;
            switch (k.ToLowerInvariant())
            {
                case "voicede": VoicesSapi = SetVoice(VoicesSapi, 0x07, v); break;
                case "voiceen": VoicesSapi = SetVoice(VoicesSapi, 0x09, v); break;
                case "voicedeonecore": VoicesOneCore = SetVoice(VoicesOneCore, 0x07, v); break;
                case "voiceenonecore": VoicesOneCore = SetVoice(VoicesOneCore, 0x09, v); break;
            }
        }

        /// <summary>Voice stored for a primary language in a list "7:Name;9:Name" ("" = automatic).</summary>
        public static string GetVoice(string list, int prim)
        {
            if (string.IsNullOrEmpty(list)) return "";
            string key = prim.ToString("X") + ":";
            foreach (string part in list.Split(';'))
                if (part.StartsWith(key, StringComparison.OrdinalIgnoreCase)) return part.Substring(key.Length);
            return "";
        }

        public static string SetVoice(string list, int prim, string name)
        {
            string key = prim.ToString("X") + ":";
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(list))
                foreach (string part in list.Split(';'))
                    if (part.Length > 0 && !part.StartsWith(key, StringComparison.OrdinalIgnoreCase)) parts.Add(part);
            if (!string.IsNullOrEmpty(name)) parts.Add(key + name.Replace(";", ","));
            return string.Join(";", parts.ToArray());
        }

        public static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("; RCGAccess Einstellungen (werden vom Mod-Menue geschrieben, F11)\r\n[RCGAccess]\r\n");
                foreach (FieldInfo f in Fields())
                {
                    object v = f.GetValue(null);
                    string s;
                    if (v is bool) s = ((bool)v) ? "1" : "0";
                    else if (v is int) s = ((int)v).ToString(CultureInfo.InvariantCulture);
                    else s = v == null ? "" : v.ToString();
                    sb.Append(f.Name).Append('=').Append(s).Append("\r\n");
                }
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
                Log.Write("Einstellungen gespeichert.");
            }
            catch (Exception e) { Log.Error("Settings.Save", e); }
        }
    }
}
