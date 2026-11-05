using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RCGAccess
{
    public static class Log
    {
        private static StreamWriter _main;
        private static StreamWriter _texts;
        private static readonly Dictionary<string, int> _errCount = new Dictionary<string, int>();
        private static readonly object _lock = new object();

        public static void Init()
        {
            try
            {
                string path = Path.Combine(Mod.DataDir, "RCGAccess.log");
                // keep the previous log once
                try { if (File.Exists(path)) File.Copy(path, Path.Combine(Mod.DataDir, "RCGAccess_vorher.log"), true); } catch { }
                _main = new StreamWriter(path, false, new UTF8Encoding(false));
                _main.AutoFlush = true;
                string tp = Path.Combine(Mod.ProtoDir, "texte_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                _texts = new StreamWriter(tp, true, new UTF8Encoding(false));
                _texts.AutoFlush = true;
                _main.WriteLine("RCGAccess Protokoll " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch { }
        }

        private static string Stamp() { return DateTime.Now.ToString("HH:mm:ss.fff"); }

        public static void Write(string s)
        {
            lock (_lock) { try { if (_main != null) _main.WriteLine(Stamp() + " " + s); } catch { } }
        }

        public static void Verbose(string s)
        {
            if (Settings.VerboseLog) Write(s);
        }

        public static void Text(string where, string s)
        {
            lock (_lock) { try { if (_texts != null) _texts.WriteLine(Stamp() + " [" + where + "] " + s); } catch { } }
        }

        public static void Error(string where, Exception e)
        {
            string key = where + ":" + e.GetType().Name;
            int n;
            _errCount.TryGetValue(key, out n);
            n++;
            _errCount[key] = n;
            if (n <= 5) Write("FEHLER in " + where + ": " + e);
            else if (n == 6) Write("FEHLER in " + where + " wiederholt sich, weitere Meldungen unterdrueckt.");
        }
    }
}
