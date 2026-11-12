using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Connection to the speech/tone server: RCGAccessSpeech.exe (32 bit, SAPI) or RCGAccessSpeech64.exe (Prism: OneCore or screen reader).</summary>
    public static class Speech
    {
        private static Process _proc;
        private static StreamWriter _in;
        private static volatile bool _speaking;
        private static float _toneUntil;
        private static float _duck = 1f;
        private static float _appliedDuck = -1f;
        private static bool _gamePaused;
        private static bool _appliedPause;
        private static int _lang = -1;
        private static string _voice = null;
        public static string LastText = "";
        public static string ActiveVoice = "";
        public static string MissingVoiceLang = "";
        private static readonly object _lock = new object();

        public static bool Running { get { return _proc != null && _in != null; } }
        public static bool IsSpeaking { get { return _speaking; } }

        /// <summary>Speech output of the running server: 0 OneCore, 1 SAPI, 2 screen reader (Settings.SpeechEngine).</summary>
        private static int _runningEngine = -1;
        /// <summary>Name of the speech output in use (e.g. "OneCore", "NVDA", "SAPI").</summary>
        public static string EngineName = "";
        private static volatile string _pendingNotice;

        public static void Start()
        {
            try
            {
                int engine = Settings.SpeechEngine;
                string exe = Path.Combine(Mod.DataDir, engine == 1 ? "RCGAccessSpeech.exe" : "RCGAccessSpeech64.exe");
                string args = engine == 2 ? "reader" : (engine == 1 ? "sapi" : "onecore");
                if (!File.Exists(exe) || (engine != 1 && IntPtr.Size != 8))
                {
                    Log.Write("Sprachserver fehlt oder passt nicht: " + exe + ", nehme SAPI.");
                    exe = Path.Combine(Mod.DataDir, "RCGAccessSpeech.exe");
                    args = "sapi";
                }
                if (!File.Exists(exe)) { Log.Write("Sprachserver fehlt: " + exe); return; }
                ProcessStartInfo psi = new ProcessStartInfo(exe, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = Mod.DataDir;
                Process proc = Process.Start(psi);
                lock (_lock)
                {
                    _proc = proc;
                    _in = proc.StandardInput;
                    _in.AutoFlush = true;
                }
                _runningEngine = engine;
                _speaking = false;
                Thread t = new Thread(ReadLoop);
                t.IsBackground = true;
                t.Start(proc);
                Log.Write("Sprachserver gestartet (PID " + proc.Id + ", " + Path.GetFileName(exe) + " " + args + ").");
                Config("rate", Settings.Rate.ToString(CultureInfo.InvariantCulture));
                Config("volume", Settings.SpeechVolume.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e) { Log.Error("Speech.Start", e); _proc = null; _in = null; }
        }

        /// <summary>Restarts the server when the chosen speech output changed (mod menu).</summary>
        public static void EnsureEngine()
        {
            if (_runningEngine == Settings.SpeechEngine) return;
            Log.Write("Sprachausgabe wechselt zu " + Settings.SpeechEngine);
            Shutdown();
            _lang = -1; _voice = null;
            ActiveVoice = ""; MissingVoiceLang = ""; EngineName = "";
            Start();
            ApplyLanguage(true);
        }

        private static void ReadLoop(object o)
        {
            Process proc = (Process)o;
            try
            {
                StreamReader r = proc.StandardOutput;
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    if (proc != _proc) break;
                    if (line == "D1") _speaking = true;
                    else if (line == "D0") _speaking = false;
                    else if (line.StartsWith("V")) { ActiveVoice = line.Substring(1); MissingVoiceLang = ""; Log.Write("Aktive Stimme: " + ActiveVoice); }
                    else if (line.StartsWith("N")) { MissingVoiceLang = line.Substring(1); Log.Write("Keine Stimme fuer Sprache " + MissingVoiceLang); }
                    else if (line.StartsWith("E"))
                    {
                        string[] p = line.Substring(1).Split('|');
                        EngineName = p.Length > 1 ? p[1] : "";
                        Log.Write("Sprachausgabe: " + line.Substring(1));
                        if (p[0] == "fallback") _pendingNotice = Loc.T(_runningEngine == 2 ? "speech_noreader" : "speech_fallback", EngineName);
                    }
                }
            }
            catch { }
            if (proc == _proc) _speaking = false;
        }

        private static void Send(string line)
        {
            lock (_lock)
            {
                if (_in == null) return;
                try { _in.WriteLine(line); }
                catch (Exception e) { Log.Error("Speech.Send", e); _in = null; }
            }
        }

        private static string B64(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? ""));
        }

        public static void Config(string key, string value)
        {
            Send("C|" + key + "|" + (value ?? ""));
        }

        /// <summary>Speak text. interrupt = cut off what is being said.</summary>
        public static void Say(string text, bool interrupt)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Trim();
            if (text.Length == 0) return;
            LastText = text;
            Log.Write("Sage" + (interrupt ? "" : " (angehaengt)") + ": " + text);
            Send("S|" + (interrupt ? "i" : "q") + "|" + B64(text));
        }

        public static void Say(string text) { Say(text, true); }
        public static void Queue(string text) { Say(text, false); }

        public static void Stop()
        {
            Send("X");
        }

        public static void Repeat()
        {
            if (!string.IsNullOrEmpty(LastText)) Say(LastText, true);
        }

        /// <summary>Raw tone. vol 0..1 (before the user's tone volume), pan -1..1, wave 0 sine 1 triangle 2 square 3 noise.</summary>
        public static void RawTone(float freq, float ms, float pan, float vol, int wave, float endFreq, float delayMs)
        {
            if (vol <= 0.001f) return;
            float until = Time.realtimeSinceStartup + (ms + delayMs) / 1000f;
            if (until > _toneUntil) _toneUntil = until;
            Send(string.Format(CultureInfo.InvariantCulture, "B|{0:0.#}|{1:0}|{2:0.###}|{3:0.###}|{4}|{5:0.#}|{6:0}",
                freq, ms, pan, vol, wave, endFreq, delayMs));
        }

        /// <summary>Orientation tone scaled by the orientation tone volume.</summary>
        public static void Tone(float freq, float ms, float pan, float vol, int wave, float endFreq, float delayMs)
        {
            RawTone(freq, ms, pan, vol * Settings.ToneVolume / 100f, wave, endFreq, delayMs);
        }

        public static void Tone(float freq, float ms, float pan, float vol, int wave)
        {
            Tone(freq, ms, pan, vol, wave, freq, 0);
        }

        public static void Tone(float freq, float ms) { Tone(freq, ms, 0, 1, 0, freq, 0); }

        public static void MenuClick() { RawTone(1000, 18, 0, 0.35f, 0, 1000, 0); }

        public static int LcidFor(LocalizationLanguages l)
        {
            switch (l)
            {
                case LocalizationLanguages.German: return 0x407;
                case LocalizationLanguages.French: return 0x40C;
                case LocalizationLanguages.Italian: return 0x410;
                case LocalizationLanguages.Spanish: return 0xC0A;
                case LocalizationLanguages.Russian: return 0x419;
                case LocalizationLanguages.Japanese: return 0x411;
                case LocalizationLanguages.Korean: return 0x412;
                case LocalizationLanguages.ChineseSimplified: return 0x804;
                case LocalizationLanguages.ChineseTraditional: return 0x404;
                default: return 0x409;
            }
        }

        /// <summary>Language of the voice: fixed mod language or the game language.</summary>
        public static int VoiceLcid()
        {
            if (Settings.ModLang == 1) return 0x407;
            if (Settings.ModLang == 2) return 0x409;
            if (GameInfo.LanguageKnown) return LcidFor(GameInfo.Language);
            return Loc.IsGerman ? 0x407 : 0x409;
        }

        /// <summary>Send language and voice to the server when they changed.</summary>
        public static void ApplyLanguage(bool force)
        {
            int lcid = VoiceLcid();
            string voice = (lcid & 0x3FF) == 0x07 ? PreferredVoice(true) : ((lcid & 0x3FF) == 0x09 ? PreferredVoice(false) : "");
            if (!force && lcid == _lang && voice == _voice) return;
            _lang = lcid; _voice = voice;
            Config("voice", voice);
            Config("lang", lcid.ToString("X"));
            Log.Write("Sprache der Stimme: " + lcid.ToString("X") + ", Wunschstimme '" + voice + "'");
        }

        /// <summary>Chosen voice for German or English; OneCore and SAPI voices are kept apart.</summary>
        public static string PreferredVoice(bool german)
        {
            if (Settings.SpeechEngine == 1) return german ? Settings.VoiceDe : Settings.VoiceEn;
            return german ? Settings.VoiceDeOneCore : Settings.VoiceEnOneCore;
        }

        public static void SetPreferredVoice(bool german, string name)
        {
            if (Settings.SpeechEngine == 1) { if (german) Settings.VoiceDe = name; else Settings.VoiceEn = name; }
            else { if (german) Settings.VoiceDeOneCore = name; else Settings.VoiceEnOneCore = name; }
        }

        public static void SetGamePaused(bool paused) { _gamePaused = paused; ApplyAudio(); }

        public static void Update()
        {
            string notice = _pendingNotice;
            if (notice != null) { _pendingNotice = null; Say(notice, false); }
            ApplyLanguage(false);
            float target = 1f;
            bool duck = (Settings.DuckSpeech && _speaking) || (Settings.DuckTones && Time.realtimeSinceStartup < _toneUntil);
            if (duck) target = Mathf.Clamp01(Settings.DuckLevel / 100f);
            float dt = Time.unscaledDeltaTime;
            if (_duck > target) _duck = Mathf.Max(target, _duck - dt * 4f);
            else if (_duck < target) _duck = Mathf.Min(target, _duck + dt * 1.2f);
            ApplyAudio();
        }

        private static void ApplyAudio()
        {
            try
            {
                if (!FMODUnity.RuntimeManager.IsInitialized) return;
                if (Mathf.Abs(_appliedDuck - _duck) < 0.005f && _appliedPause == _gamePaused) return;
                FMOD.ChannelGroup master;
                FMODUnity.RuntimeManager.LowlevelSystem.getMasterChannelGroup(out master);
                master.setVolume(_duck);
                master.setPaused(_gamePaused);
                _appliedDuck = _duck;
                _appliedPause = _gamePaused;
            }
            catch (Exception e) { Log.Error("Speech.ApplyAudio", e); }
        }

        public static void Shutdown()
        {
            Process p = _proc;
            try { Send("Q"); } catch { }
            lock (_lock) { _proc = null; _in = null; }
            _speaking = false;
            try { if (p != null && !p.WaitForExit(600)) p.Kill(); } catch { }
            try { if (p != null && !p.HasExited) p.Kill(); } catch { }
        }
    }
}
