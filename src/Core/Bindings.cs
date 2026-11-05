using System;
using System.Collections.Generic;
using System.Text;
using Rewired;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Reads the game's own controller bindings (Rewired) to find buttons the game does not use.</summary>
    public static class Bindings
    {
        public enum Btn { LB, RB, LT, RT, Back, LS, RS }

        private static readonly HashSet<Btn> _used = new HashSet<Btn>();
        public static bool Known;
        private static float _nextTry;

        public static bool IsUsed(Btn b) { return _used.Contains(b); }

        private static bool Has(string n, params string[] keys)
        {
            foreach (string k in keys) if (n == k || n.Contains(k)) return true;
            return false;
        }

        private static bool? Classify(string name, out Btn b)
        {
            b = Btn.LB;
            string n = (name ?? "").ToLower().Trim();
            if (n.Length == 0) return null;
            if (Has(n, "left stick button", "left stick click", "left stick press", "l3")) { b = Btn.LS; return true; }
            if (Has(n, "right stick button", "right stick click", "right stick press", "r3")) { b = Btn.RS; return true; }
            if (Has(n, "left trigger", "left shoulder 2", "l2") || n == "lt") { b = Btn.LT; return true; }
            if (Has(n, "right trigger", "right shoulder 2", "r2") || n == "rt") { b = Btn.RT; return true; }
            if (Has(n, "left shoulder", "left bumper", "l1") || n == "lb") { b = Btn.LB; return true; }
            if (Has(n, "right shoulder", "right bumper", "r1") || n == "rb") { b = Btn.RB; return true; }
            if (Has(n, "back", "view", "select", "center 1", "share")) { b = Btn.Back; return true; }
            return null;
        }

        /// <summary>Called regularly until the joystick maps of player 1 could be read.</summary>
        public static void Update()
        {
            if (Known || Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 3f;
            try
            {
                if (!ReInput.isReady) return;
                Player p = ReInput.players.GetPlayer(0);
                if (p == null) return;
                StringBuilder sb = new StringBuilder();
                int maps = 0;
                foreach (ControllerMap m in p.controllers.maps.GetAllMaps(ControllerType.Joystick))
                {
                    maps++;
                    foreach (ActionElementMap a in m.GetElementMaps())
                    {
                        string act = "?";
                        try { InputAction ia = ReInput.mapping.GetAction(a.actionId); if (ia != null) act = ia.name; } catch { }
                        sb.Append(a.elementIdentifierName).Append('=').Append(act).Append(m.enabled ? "" : "(aus)").Append("; ");
                        Btn b;
                        if (a.actionId >= 0 && Classify(a.elementIdentifierName, out b) == true) _used.Add(b);
                    }
                }
                if (maps == 0) return;
                Known = true;
                Log.Write("Controller-Belegung des Spiels: " + sb);
                StringBuilder u = new StringBuilder();
                foreach (Btn b in _used) u.Append(b).Append(' ');
                Log.Write("Vom Spiel belegte Zusatztasten: " + (u.Length > 0 ? u.ToString() : "keine") + ", freie Taste fuer Gegneranzahl: " + AutoCountButton());
            }
            catch (Exception e) { Log.Error("Bindings", e); Known = true; }
        }

        /// <summary>First button the game does not use, for the "enemies left" query; null if unknown or none.</summary>
        public static Btn? AutoCountButton()
        {
            if (!Known) return null;
            foreach (Btn b in new Btn[] { Btn.RB, Btn.LB, Btn.RT, Btn.LT, Btn.Back })
                if (!_used.Contains(b)) return b;
            return null;
        }

        /// <summary>Spoken note which extra buttons the game uses itself.</summary>
        public static string UsageNote()
        {
            if (!Known) return Loc.T("bind_unknown");
            List<string> u = new List<string>();
            foreach (Btn b in new Btn[] { Btn.LB, Btn.RB, Btn.LT, Btn.RT, Btn.Back, Btn.LS, Btn.RS })
                if (_used.Contains(b)) u.Add(Name(b));
            if (u.Count == 0) return Loc.T("bind_none");
            return Loc.T("bind_used", string.Join(", ", u.ToArray()));
        }

        public static string Name(Btn b)
        {
            switch (b)
            {
                case Btn.LB: return "LB / L1";
                case Btn.RB: return "RB / R1";
                case Btn.LT: return "LT / L2";
                case Btn.RT: return "RT / R2";
                case Btn.Back: return Loc.T("btn_back");
                case Btn.LS: return Loc.T("btn_ls");
                default: return Loc.T("btn_rs");
            }
        }

        /// <summary>The button chosen in the mod menu (0 automatic, 1 LB, 2 RB, 3 LT, 4 RT, 5 Back, 6 off).</summary>
        public static Btn? CountButton()
        {
            switch (Settings.EnemyCountButton)
            {
                case 1: return Btn.LB;
                case 2: return Btn.RB;
                case 3: return Btn.LT;
                case 4: return Btn.RT;
                case 5: return Btn.Back;
                case 6: return null;
                default: return AutoCountButton();
            }
        }

        public static bool Pressed(Btn b)
        {
            switch (b)
            {
                case Btn.LB: return ModInput.PadDown(Pad.LB);
                case Btn.RB: return ModInput.PadDown(Pad.RB);
                case Btn.LT: return ModInput.TriggerDown(true);
                case Btn.RT: return ModInput.TriggerDown(false);
                case Btn.Back: return ModInput.PadDown(Pad.Back);
                case Btn.LS: return ModInput.PadDown(Pad.LS);
                default: return ModInput.PadDown(Pad.RS);
            }
        }
    }
}
