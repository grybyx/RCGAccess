// RCGAccessSpeech - 32-bit speech and tone server for RCGAccess (River City Girls accessibility mod).
// Reads one command per line from standard input:
//   S|flags|base64(utf8 text)   speak; flags: i = interrupt, q = queue
//   X                           stop speech
//   C|key|value                 rate, volume, lang (hex LCID, e.g. 407), voice (name, empty = automatic)
//   B|freq|ms|pan|vol|wave|endfreq|delay   tone; wave 0 sine, 1 triangle, 2 square, 3 noise
//   V                           write voice list file again
//   L|base64(text)              log line
//   Q                           quit
// Writes "D1"/"D0" to standard output when speaking starts/stops (used by the mod for ducking).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace RCGAccessSpeech
{
    internal static class Program
    {
        static object _voice;
        static readonly object _lock = new object();
        static string _dir;
        static StreamWriter _log;
        static int _lang = 0x407;
        static string _voiceName = "";
        static int _rate = 2;
        static int _volume = 100;
        static bool _speaking;
        static Mixer _mixer;
        static volatile bool _quit;

        [MTAThread]
        static int Main(string[] args)
        {
            _dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try { _log = new StreamWriter(Path.Combine(_dir, "RCGAccessSpeech.log"), false, new UTF8Encoding(false)); _log.AutoFlush = true; } catch { }
            Log("Sprachserver gestartet. Argumente: " + string.Join(" ", args));
            try
            {
                Type t = Type.GetTypeFromProgID("SAPI.SpVoice");
                _voice = Activator.CreateInstance(t);
                Log("SAPI bereit.");
            }
            catch (Exception e) { Log("SAPI nicht verfuegbar: " + e.Message); }
            WriteVoices();
            ApplyVoice();
            try { _mixer = new Mixer(); _mixer.Start(); Log("Ton-Mixer bereit."); }
            catch (Exception e) { Log("Ton-Mixer Fehler: " + e.Message); }

            Thread status = new Thread(StatusLoop);
            status.IsBackground = true;
            status.Start();

            Stream stdin = Console.OpenStandardInput();
            StreamReader reader = new StreamReader(stdin, Encoding.ASCII);
            string line;
            while (!_quit && (line = reader.ReadLine()) != null)
            {
                try { Handle(line); }
                catch (Exception e) { Log("Fehler bei '" + Short(line) + "': " + e.Message); }
            }
            _quit = true;
            try { if (_mixer != null) _mixer.Stop(); } catch { }
            try { Stop(); } catch { }
            Log("Sprachserver beendet.");
            return 0;
        }

        static string Short(string s) { return s.Length > 60 ? s.Substring(0, 60) : s; }

        static void Log(string s)
        {
            try { if (_log != null) _log.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + s); } catch { }
        }

        static string B64(string s)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch { return ""; }
        }

        static float F(string s, float def)
        {
            float v;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            return def;
        }

        static void Handle(string line)
        {
            if (line.Length == 0) return;
            string[] p = line.Split('|');
            switch (p[0])
            {
                case "S":
                    {
                        string flags = p.Length > 1 ? p[1] : "";
                        string text = p.Length > 2 ? B64(p[2]) : "";
                        Speak(text, flags.IndexOf('i') >= 0);
                        break;
                    }
                case "X": Stop(); break;
                case "C":
                    if (p.Length >= 3) Config(p[1], p[2]);
                    break;
                case "B":
                    if (_mixer != null && p.Length >= 3)
                    {
                        float freq = F(p[1], 440);
                        float ms = F(p[2], 100);
                        float pan = p.Length > 3 ? F(p[3], 0) : 0;
                        float vol = p.Length > 4 ? F(p[4], 0.7f) : 0.7f;
                        int wave = p.Length > 5 ? (int)F(p[5], 0) : 0;
                        float end = p.Length > 6 ? F(p[6], freq) : freq;
                        float delay = p.Length > 7 ? F(p[7], 0) : 0;
                        _mixer.Add(freq, end, ms, pan, vol, wave, delay);
                    }
                    break;
                case "V": WriteVoices(); break;
                case "L": if (p.Length > 1) Log("Mod: " + B64(p[1])); break;
                case "Q": _quit = true; break;
            }
        }

        // ---------------- SAPI ----------------
        static object Call(object o, string name, params object[] a)
        {
            return o.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, o, a);
        }
        static object Get(object o, string name)
        {
            return o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, o, null);
        }
        static void Set(object o, string name, object v)
        {
            o.GetType().InvokeMember(name, BindingFlags.SetProperty, null, o, new object[] { v });
        }

        static void Speak(string text, bool interrupt)
        {
            if (_voice == null) return;
            lock (_lock)
            {
                // SVSFlagsAsync = 1, SVSFPurgeBeforeSpeak = 2, SVSFIsNotXML = 16
                int flags = 1 | 16 | (interrupt ? 2 : 0);
                if (string.IsNullOrEmpty(text)) { if (interrupt) Call(_voice, "Speak", "", 1 | 2); return; }
                Call(_voice, "Speak", text, flags);
            }
        }

        static void Stop()
        {
            if (_voice == null) return;
            lock (_lock) { Call(_voice, "Speak", "", 1 | 2); }
        }

        static void Config(string key, string val)
        {
            switch (key)
            {
                case "rate":
                    _rate = Math.Max(-10, Math.Min(10, (int)F(val, 2)));
                    if (_voice != null) lock (_lock) Set(_voice, "Rate", _rate);
                    break;
                case "volume":
                    _volume = Math.Max(0, Math.Min(100, (int)F(val, 100)));
                    if (_voice != null) lock (_lock) Set(_voice, "Volume", _volume);
                    break;
                case "lang":
                    {
                        int l;
                        if (int.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out l)) { _lang = l; ApplyVoice(); }
                        break;
                    }
                case "voice":
                    _voiceName = val ?? "";
                    ApplyVoice();
                    break;
            }
        }

        static int ParseLang(string attr)
        {
            if (string.IsNullOrEmpty(attr)) return 0;
            string first = attr.Split(';')[0].Trim();
            int l;
            if (int.TryParse(first, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out l)) return l;
            return 0;
        }

        class VoiceInfo { public object Token; public string Name; public int Lang; }

        static List<VoiceInfo> ListVoices()
        {
            List<VoiceInfo> list = new List<VoiceInfo>();
            if (_voice == null) return list;
            lock (_lock)
            {
                object tokens = Call(_voice, "GetVoices", "", "");
                int n = (int)Get(tokens, "Count");
                for (int i = 0; i < n; i++)
                {
                    try
                    {
                        object tok = Call(tokens, "Item", i);
                        VoiceInfo v = new VoiceInfo();
                        v.Token = tok;
                        v.Name = (string)Call(tok, "GetDescription", 0);
                        string la = "";
                        try { la = (string)Call(tok, "GetAttribute", "Language"); } catch { }
                        v.Lang = ParseLang(la);
                        list.Add(v);
                    }
                    catch (Exception e) { Log("Stimme " + i + ": " + e.Message); }
                }
            }
            return list;
        }

        static void WriteVoices()
        {
            try
            {
                List<VoiceInfo> list = ListVoices();
                StringBuilder sb = new StringBuilder();
                foreach (VoiceInfo v in list) sb.Append(v.Name.Replace("|", "/")).Append('|').Append(v.Lang.ToString("X")).Append("\r\n");
                File.WriteAllText(Path.Combine(_dir, "RCGAccessVoices.txt"), sb.ToString(), new UTF8Encoding(false));
                Log("Stimmen gefunden: " + list.Count);
                foreach (VoiceInfo v in list) Log("  " + v.Name + " (" + v.Lang.ToString("X") + ")");
            }
            catch (Exception e) { Log("Stimmenliste Fehler: " + e.Message); }
        }

        static void ApplyVoice()
        {
            if (_voice == null) return;
            try
            {
                List<VoiceInfo> list = ListVoices();
                VoiceInfo pick = null;
                int prim = _lang & 0x3FF;
                if (_voiceName.Length > 0)
                    foreach (VoiceInfo v in list)
                        if (v.Name == _voiceName && (v.Lang & 0x3FF) == prim) { pick = v; break; }
                if (pick == null)
                    foreach (VoiceInfo v in list) if (v.Lang == _lang) { pick = v; break; }
                if (pick == null)
                    foreach (VoiceInfo v in list) if ((v.Lang & 0x3FF) == prim) { pick = v; break; }
                if (pick == null)
                {
                    Log("Keine Stimme fuer Sprache " + _lang.ToString("X") + " gefunden, Stimme bleibt.");
                    Console.Out.WriteLine("N" + _lang.ToString("X"));
                    Console.Out.Flush();
                }
                else
                {
                    lock (_lock)
                    {
                        _voice.GetType().InvokeMember("Voice", BindingFlags.PutRefDispProperty, null, _voice, new object[] { pick.Token });
                        Set(_voice, "Rate", _rate);
                        Set(_voice, "Volume", _volume);
                    }
                    Log("Stimme: " + pick.Name + " (" + pick.Lang.ToString("X") + ")");
                    Console.Out.WriteLine("V" + pick.Name);
                    Console.Out.Flush();
                }
            }
            catch (Exception e) { Log("Stimme setzen Fehler: " + e.Message); }
        }

        static void StatusLoop()
        {
            while (!_quit)
            {
                Thread.Sleep(40);
                if (_voice == null) continue;
                bool speaking = false;
                try
                {
                    lock (_lock)
                    {
                        object st = Get(_voice, "Status");
                        int rs = (int)Get(st, "RunningState");
                        speaking = rs == 2;
                    }
                }
                catch { }
                if (speaking != _speaking)
                {
                    _speaking = speaking;
                    try { Console.Out.WriteLine(speaking ? "D1" : "D0"); Console.Out.Flush(); } catch { }
                }
            }
        }
    }

    // ---------------- waveOut tone mixer ----------------
    internal sealed class Mixer
    {
        [StructLayout(LayoutKind.Sequential)]
        struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr hwo, uint dev, ref WAVEFORMATEX fmt, IntPtr cb, IntPtr inst, uint flags);
        [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr hdr, uint size);
        [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr hwo, IntPtr hdr, uint size);
        [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr hwo, IntPtr hdr, uint size);
        [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr hwo);
        [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr hwo);

        const int Rate = 44100;
        const int Frames = 882; // 20 ms
        const int NBuf = 4;

        class Voice
        {
            public double Phase;
            public double F0, F1;
            public int Start, Len, Pos;
            public float L, R;
            public int Wave;
            public uint Noise = 22222;
        }

        IntPtr _h;
        IntPtr[] _hdr = new IntPtr[NBuf];
        IntPtr[] _data = new IntPtr[NBuf];
        bool[] _used = new bool[NBuf];
        readonly List<Voice> _voices = new List<Voice>();
        readonly object _vl = new object();
        Thread _thread;
        volatile bool _run;
        int _flagsOffset;
        uint _hdrSize;
        float[] _mix = new float[Frames * 2];
        short[] _pcm = new short[Frames * 2];

        public void Start()
        {
            WAVEFORMATEX f = new WAVEFORMATEX();
            f.wFormatTag = 1; f.nChannels = 2; f.nSamplesPerSec = Rate; f.wBitsPerSample = 16;
            f.nBlockAlign = 4; f.nAvgBytesPerSec = Rate * 4; f.cbSize = 0;
            int r = waveOutOpen(out _h, 0xFFFFFFFF, ref f, IntPtr.Zero, IntPtr.Zero, 0);
            if (r != 0) throw new Exception("waveOutOpen " + r);
            _hdrSize = (uint)Marshal.SizeOf(typeof(WAVEHDR));
            _flagsOffset = (int)Marshal.OffsetOf(typeof(WAVEHDR), "dwFlags");
            for (int i = 0; i < NBuf; i++)
            {
                _data[i] = Marshal.AllocHGlobal(Frames * 4);
                _hdr[i] = Marshal.AllocHGlobal((int)_hdrSize);
                WAVEHDR h = new WAVEHDR();
                h.lpData = _data[i];
                h.dwBufferLength = Frames * 4;
                Marshal.StructureToPtr(h, _hdr[i], false);
                waveOutPrepareHeader(_h, _hdr[i], _hdrSize);
            }
            _run = true;
            _thread = new Thread(Loop);
            _thread.IsBackground = true;
            _thread.Priority = ThreadPriority.AboveNormal;
            _thread.Start();
        }

        public void Stop()
        {
            _run = false;
            try { waveOutReset(_h); } catch { }
            for (int i = 0; i < NBuf; i++) try { waveOutUnprepareHeader(_h, _hdr[i], _hdrSize); } catch { }
            try { waveOutClose(_h); } catch { }
        }

        public void Add(float f0, float f1, float ms, float pan, float vol, int wave, float delayMs)
        {
            if (ms <= 0 || vol <= 0) return;
            Voice v = new Voice();
            v.F0 = Math.Max(20, f0); v.F1 = Math.Max(20, f1);
            v.Len = Math.Max(1, (int)(ms * Rate / 1000f));
            v.Start = Math.Max(0, (int)(delayMs * Rate / 1000f));
            pan = Math.Max(-1f, Math.Min(1f, pan));
            double a = (pan + 1) * Math.PI / 4;
            vol = Math.Min(2f, vol);
            v.L = (float)(Math.Cos(a) * vol * 0.5);
            v.R = (float)(Math.Sin(a) * vol * 0.5);
            v.Wave = wave;
            lock (_vl) { if (_voices.Count < 64) _voices.Add(v); }
        }

        void Loop()
        {
            while (_run)
            {
                bool wrote = false;
                for (int i = 0; i < NBuf; i++)
                {
                    int flags = Marshal.ReadInt32(_hdr[i], _flagsOffset);
                    bool done = (flags & 1) != 0; // WHDR_DONE
                    if (!_used[i] || done)
                    {
                        Fill(_data[i]);
                        waveOutWrite(_h, _hdr[i], _hdrSize);
                        _used[i] = true;
                        wrote = true;
                    }
                }
                if (!wrote) Thread.Sleep(3);
            }
        }

        void Fill(IntPtr dst)
        {
            Array.Clear(_mix, 0, _mix.Length);
            lock (_vl)
            {
                for (int vi = _voices.Count - 1; vi >= 0; vi--)
                {
                    Voice v = _voices[vi];
                    for (int n = 0; n < Frames; n++)
                    {
                        if (v.Start > 0) { v.Start--; continue; }
                        if (v.Pos >= v.Len) break;
                        double t = (double)v.Pos / v.Len;
                        double freq = v.F0 + (v.F1 - v.F0) * t;
                        v.Phase += freq / Rate;
                        if (v.Phase >= 1) v.Phase -= Math.Floor(v.Phase);
                        double s;
                        switch (v.Wave)
                        {
                            case 1: s = 4 * Math.Abs(v.Phase - 0.5) - 1; break;
                            case 2: s = v.Phase < 0.5 ? 0.6 : -0.6; break;
                            case 3:
                                v.Noise ^= v.Noise << 13; v.Noise ^= v.Noise >> 17; v.Noise ^= v.Noise << 5;
                                s = ((v.Noise & 0xFFFF) / 32768.0 - 1.0) * 0.7;
                                break;
                            default: s = Math.Sin(v.Phase * 2 * Math.PI); break;
                        }
                        // envelope: 4 ms attack, release over the last 25 % (at least 6 ms)
                        int att = Rate * 4 / 1000;
                        int rel = Math.Max(Rate * 6 / 1000, v.Len / 4);
                        double env = 1;
                        if (v.Pos < att) env = (double)v.Pos / att;
                        int left = v.Len - v.Pos;
                        if (left < rel) env = Math.Min(env, (double)left / rel);
                        s *= env;
                        _mix[n * 2] += (float)(s * v.L);
                        _mix[n * 2 + 1] += (float)(s * v.R);
                        v.Pos++;
                    }
                    if (v.Pos >= v.Len) _voices.RemoveAt(vi);
                }
            }
            for (int i = 0; i < _mix.Length; i++)
            {
                float x = _mix[i];
                if (x > 1f) x = 1f; else if (x < -1f) x = -1f;
                _pcm[i] = (short)(x * 32000);
            }
            Marshal.Copy(_pcm, 0, dst, _pcm.Length);
        }
    }
}
