using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace RCGAccess
{
    /// <summary>All switches and values of the mod (RCGAccess\RCGAccess.ini).</summary>
    public static class Settings
    {
        public const int CurrentMenuVersion = 6;

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
        public static string VoiceDe = "";
        public static string VoiceEn = "";
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
                    if (!map.TryGetValue(k, out fi)) continue;
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
