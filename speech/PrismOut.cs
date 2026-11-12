// Speech output through Prism (https://github.com/ethindp/prism, MPL-2.0) by Ethin Probst.
// prism.dll (64 bit) lies next to RCGAccessSpeech64.exe. Only used by the 64-bit server.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace RCGAccessSpeech
{
    internal static class PrismOut
    {
        const string Dll = "prism.dll";

        const int PRISM_OK = 0;
        const int PRISM_ERROR_NOT_IMPLEMENTED = 3;
        const int PRISM_ERROR_BACKEND_NOT_AVAILABLE = 16;

        const ulong SUPPORTS_IS_SPEAKING = 1UL << 6;
        const ulong SUPPORTS_SET_VOLUME = 1UL << 10;
        const ulong SUPPORTS_SET_RATE = 1UL << 12;
        const ulong SUPPORTS_COUNT_VOICES = 1UL << 17;
        const ulong SUPPORTS_SET_VOICE = 1UL << 21;

        const ulong ID_ONE_CORE = 0x6797D32F0D994CB4UL;
        const ulong ID_SAPI = 0x1D6DF72422CEEE66UL;

        // screen readers in the order they are tried
        static readonly ulong[] Readers =
        {
            0x89CC19C5C4AC1A56UL, // NVDA
            0xAC3D60E9BD84B53EUL, // JAWS
            0x3D93C56C9E7F2A2EUL, // ZDSR
            0x8380F2A37B2C3EB6UL, // System Access
            0xAE439D62DC7B1479UL, // ZoomText
            0x344B951962E3B835UL, // PC-Talker
            0xED4760890B55C2F2UL, // Sense Reader
            0x285ABA1C16F3300FUL, // BoyPC Reader
            0x9120D89908785C13UL, // Window-Eyes
        };

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr prism_init(IntPtr cfg);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern void prism_shutdown(IntPtr ctx);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern UIntPtr prism_registry_count(IntPtr ctx);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern ulong prism_registry_id_at(IntPtr ctx, UIntPtr index);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr prism_registry_name(IntPtr ctx, ulong id);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_registry_priority(IntPtr ctx, ulong id);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] static extern bool prism_registry_exists(IntPtr ctx, ulong id);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr prism_registry_create(IntPtr ctx, ulong id);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern void prism_backend_free(IntPtr backend);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr prism_backend_name(IntPtr backend);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern ulong prism_backend_get_features(IntPtr backend);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_initialize(IntPtr backend);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_speak(IntPtr backend, byte[] text, [MarshalAs(UnmanagedType.I1)] bool interrupt);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_output(IntPtr backend, byte[] text, [MarshalAs(UnmanagedType.I1)] bool interrupt);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_stop(IntPtr backend);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_is_speaking(IntPtr backend, out byte speaking);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_set_volume(IntPtr backend, float volume);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_set_rate(IntPtr backend, float rate);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_count_voices(IntPtr backend, out UIntPtr count);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_get_voice_name(IntPtr backend, UIntPtr voice, out IntPtr name);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_get_voice_language(IntPtr backend, UIntPtr voice, out IntPtr lang);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern int prism_backend_set_voice(IntPtr backend, UIntPtr voice);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr prism_error_string(int error);

        static IntPtr _ctx;
        static IntPtr _b;
        static ulong _features;
        static bool _wantReader;
        static bool _estimate;          // backend cannot report speaking: estimate from text length
        static DateTime _estUntil = DateTime.MinValue;
        static Action<string> _log;

        public static bool Active { get { return _b != IntPtr.Zero; } }
        public static bool IsReader { get; private set; }
        public static string Name { get; private set; }

        static void Log(string s) { if (_log != null) _log("Prism: " + s); }

        static string Utf8(IntPtr p)
        {
            if (p == IntPtr.Zero) return "";
            int n = 0;
            while (Marshal.ReadByte(p, n) != 0 && n < 4096) n++;
            byte[] b = new byte[n];
            Marshal.Copy(p, b, 0, n);
            return Encoding.UTF8.GetString(b);
        }

        static byte[] Z(string s)
        {
            byte[] t = Encoding.UTF8.GetBytes(s ?? "");
            byte[] r = new byte[t.Length + 1];
            Buffer.BlockCopy(t, 0, r, 0, t.Length);
            return r;
        }

        static string Err(int e)
        {
            try { return e + " (" + Utf8(prism_error_string(e)) + ")"; } catch { return e.ToString(); }
        }

        /// <summary>Starts Prism. reader = use the running screen reader, otherwise OneCore. Falls back to OneCore
        /// when no screen reader runs; returns false when no Prism output works at all.</summary>
        public static bool Init(bool reader, Action<string> log)
        {
            _log = log;
            _wantReader = reader;
            try
            {
                _ctx = prism_init(IntPtr.Zero);
                if (_ctx == IntPtr.Zero) { Log("prism_init fehlgeschlagen."); return false; }
                int n = (int)(ulong)prism_registry_count(_ctx);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < n; i++)
                {
                    ulong id = prism_registry_id_at(_ctx, new UIntPtr((uint)i));
                    sb.Append(i == 0 ? "" : ", ").Append(Utf8(prism_registry_name(_ctx, id))).Append(" (").Append(prism_registry_priority(_ctx, id)).Append(')');
                }
                Log("Backends: " + sb);
            }
            catch (Exception e) { Log("prism.dll nicht ladbar: " + e.Message); _ctx = IntPtr.Zero; return false; }
            return Connect();
        }

        static bool Connect()
        {
            IsReader = false;
            if (_wantReader)
                foreach (ulong id in Readers)
                    if (TryBackend(id)) { IsReader = true; return true; }
            if (_wantReader) Log("Kein laufender Screenreader gefunden, nehme OneCore.");
            if (TryBackend(ID_ONE_CORE)) return true;
            Log("OneCore nicht verfuegbar.");
            return false;
        }

        static bool TryBackend(ulong id)
        {
            try
            {
                if (!prism_registry_exists(_ctx, id)) return false;
                IntPtr b = prism_registry_create(_ctx, id);
                if (b == IntPtr.Zero) return false;
                string name = Utf8(prism_backend_name(b));
                int e = prism_backend_initialize(b);
                if (e != PRISM_OK) { Log(name + " startet nicht: " + Err(e)); prism_backend_free(b); return false; }
                _b = b;
                Name = name;
                _features = prism_backend_get_features(b);
                _estimate = (_features & SUPPORTS_IS_SPEAKING) == 0;
                Log("Ausgabe ueber " + name + (_estimate ? " (Sprechende wird geschaetzt)" : ""));
                return true;
            }
            catch (Exception e) { Log("Backend " + id.ToString("X") + ": " + e.Message); return false; }
        }

        public static void Shutdown()
        {
            if (_b != IntPtr.Zero) { try { prism_backend_free(_b); } catch { } _b = IntPtr.Zero; }
            if (_ctx != IntPtr.Zero) { try { prism_shutdown(_ctx); } catch { } _ctx = IntPtr.Zero; }
        }

        public static void Speak(string text, bool interrupt)
        {
            if (_b == IntPtr.Zero) return;
            if (string.IsNullOrEmpty(text)) { if (interrupt) Stop(); return; }
            int e = DoSpeak(text, interrupt);
            if (e == PRISM_ERROR_BACKEND_NOT_AVAILABLE)
            {
                // e.g. the screen reader was restarted: connect again once
                Log(Name + " nicht mehr erreichbar, verbinde neu.");
                try { prism_backend_free(_b); } catch { }
                _b = IntPtr.Zero;
                if (Connect()) e = DoSpeak(text, interrupt);
            }
            if (e != PRISM_OK) Log("Sprechen fehlgeschlagen: " + Err(e));
            else if (_estimate)
            {
                DateTime now = DateTime.UtcNow;
                DateTime from = (interrupt || _estUntil < now) ? now : _estUntil;
                double ms = Math.Min(10000, 250 + text.Length * 45);
                _estUntil = from.AddMilliseconds(ms);
            }
        }

        static int DoSpeak(string text, bool interrupt)
        {
            if (_b == IntPtr.Zero) return PRISM_ERROR_BACKEND_NOT_AVAILABLE;
            // screen readers: speech and braille
            return IsReader ? prism_backend_output(_b, Z(text), interrupt) : prism_backend_speak(_b, Z(text), interrupt);
        }

        public static void Stop()
        {
            if (_b == IntPtr.Zero) return;
            _estUntil = DateTime.MinValue;
            int e = prism_backend_stop(_b);
            if (e != PRISM_OK && e != PRISM_ERROR_NOT_IMPLEMENTED) Log("Stopp: " + Err(e));
        }

        public static bool IsSpeaking()
        {
            if (_b == IntPtr.Zero) return false;
            if (_estimate) return DateTime.UtcNow < _estUntil;
            byte s;
            int e = prism_backend_is_speaking(_b, out s);
            if (e == PRISM_ERROR_NOT_IMPLEMENTED) { _estimate = true; return false; }
            return e == PRISM_OK && s != 0;
        }

        /// <summary>SAPI-like rate -10..10 to Prism 0..1 (0.5 = normal).</summary>
        public static void SetRate(int rate)
        {
            if (_b == IntPtr.Zero || IsReader || (_features & SUPPORTS_SET_RATE) == 0) return;
            float v = Math.Max(0f, Math.Min(1f, 0.5f + rate / 20f));
            int e = prism_backend_set_rate(_b, v);
            if (e != PRISM_OK) Log("Tempo: " + Err(e));
        }

        public static void SetVolume(int volume)
        {
            if (_b == IntPtr.Zero || IsReader || (_features & SUPPORTS_SET_VOLUME) == 0) return;
            int e = prism_backend_set_volume(_b, Math.Max(0f, Math.Min(1f, volume / 100f)));
            if (e != PRISM_OK) Log("Lautstaerke: " + Err(e));
        }

        public static void SetVoice(int index)
        {
            if (_b == IntPtr.Zero || IsReader || (_features & SUPPORTS_SET_VOICE) == 0) return;
            int e = prism_backend_set_voice(_b, new UIntPtr((uint)index));
            if (e != PRISM_OK) Log("Stimme: " + Err(e));
        }

        public static List<Program.VoiceInfo> Voices()
        {
            List<Program.VoiceInfo> list = new List<Program.VoiceInfo>();
            if (_b == IntPtr.Zero || IsReader || (_features & SUPPORTS_COUNT_VOICES) == 0) return list;
            UIntPtr cnt;
            if (prism_backend_count_voices(_b, out cnt) != PRISM_OK) return list;
            int n = (int)(ulong)cnt;
            for (int i = 0; i < n; i++)
            {
                IntPtr pn, pl;
                if (prism_backend_get_voice_name(_b, new UIntPtr((uint)i), out pn) != PRISM_OK) continue;
                string name = Utf8(pn);
                string lang = prism_backend_get_voice_language(_b, new UIntPtr((uint)i), out pl) == PRISM_OK ? Utf8(pl) : "";
                Program.VoiceInfo v = new Program.VoiceInfo();
                v.Index = i;
                v.Name = name;
                v.Lang = Lcid(lang);
                list.Add(v);
            }
            return list;
        }

        /// <summary>BCP 47 tag ("de-DE") to a Windows LCID (0x407).</summary>
        static int Lcid(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;
            tag = tag.Replace('_', '-').Trim();
            try { return new CultureInfo(tag).LCID; } catch { }
            try { return new CultureInfo(tag.Split('-')[0]).LCID; } catch { }
            int hex;
            if (int.TryParse(tag, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex)) return hex;
            return 0;
        }
    }
}
