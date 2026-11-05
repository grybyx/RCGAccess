using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RCG;
using TMPro;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Mod keys during the game (keyboard F keys, controller sticks).</summary>
    public static class Hotkeys
    {
        private static float _rsDown;
        private static bool _rsHoldDone;
        public static void Update()
        {
            if (ModInput.Key(KeyCode.F11)) { ModMenu.Open(false); return; }
            if (ModInput.KeyPlain(KeyCode.F9)) Speech.Repeat();
            if (ModInput.KeyPlain(KeyCode.F10)) Speech.Stop();
            if (ModInput.KeyPlain(KeyCode.F7)) ChangeRate(1);
            if (ModInput.KeyShift(KeyCode.F7)) ChangeRate(-1);

            if (ModInput.KeyPlain(KeyCode.F5)) Speech.Say(Combat.StatusText(), true);
            if (ModInput.KeyShift(KeyCode.F5)) Navigator.DescribeRoom(true);
            if (ModInput.KeyCtrl(KeyCode.F5)) Speech.Say(Combat.EnemyList(), true);
            if (ModInput.KeyShift(KeyCode.F3)) Speech.Say(Navigator.QuestText(), true);

            if (ModInput.KeyPlain(KeyCode.F6)) Navigator.SayTarget(false);
            if (ModInput.KeyShift(KeyCode.F6)) ToneTest();

            if (ModInput.KeyPlain(KeyCode.F8)) Navigator.ToggleBeacon();
            if (Input.GetKeyDown(KeyCode.F8) && ModInput.Shift() && !ModInput.CtrlNow()) Navigator.Cycle(1);
            if (Input.GetKeyDown(KeyCode.F8) && ModInput.Shift() && ModInput.CtrlNow()) Navigator.Cycle(-1);
            if (ModInput.KeyCtrl(KeyCode.F8)) Navigator.ToggleWalk();
            if (ModInput.KeyCtrl(KeyCode.F12)) SceneProtocol();

            if (!Settings.PadKeys || !ModInput.PadConnected) return;
            bool gameplay = !GameInfo.Quiet;
            if (!gameplay) return;
            Bindings.Btn? cb = Bindings.CountButton();
            if (cb.HasValue && Bindings.Pressed(cb.Value)) Speech.Say(Combat.EnemyCountText(), true);
            // right stick: short press = guide me there, hold = beacon on/off (the left stick press is the game's taunt)
            if (ModInput.PadDown(Pad.RS)) { _rsDown = Time.realtimeSinceStartup; _rsHoldDone = false; }
            if (ModInput.PadHeld(Pad.RS) && !_rsHoldDone && Time.realtimeSinceStartup - _rsDown >= 0.6f) { _rsHoldDone = true; Navigator.ToggleBeacon(); }
            if (!ModInput.PadHeld(Pad.RS) && _rsDown > 0f)
            {
                if (!_rsHoldDone) Navigator.ToggleWalk();
                _rsDown = 0f;
            }
            switch (ModInput.RightStickFlick())
            {
                case StickDir.Up: Navigator.Cycle(-1); break;
                case StickDir.Down: Navigator.Cycle(1); break;
                case StickDir.Left: Navigator.DescribeRoom(true); break;
                case StickDir.Right: Speech.Say(Combat.StatusText(), true); break;
            }
        }

        private static void ChangeRate(int d)
        {
            Settings.Rate = Mathf.Clamp(Settings.Rate + d, -5, 10);
            Speech.Config("rate", Settings.Rate.ToString());
            Speech.Say(Loc.T("rate", Settings.Rate), true);
            Settings.Save();
        }

        private static void ToneTest()
        {
            Speech.Say(Loc.T("tonetest"), true);
            float d = 2600;
            Speech.Tone(330, 70, 0.8f, 0.7f, 1, 330, d);
            Speech.Tone(330, 70, 0.8f, 0.7f, 1, 330, d + 500);
            Speech.Tone(495, 70, -0.8f, 0.7f, 1, 495, d + 1000);
            Speech.RawTone(240, 55, -0.6f, Settings.KnockVolume / 100f, 0, 170, d + 1800);
            Speech.RawTone(240, 55, -0.6f, Settings.KnockVolume / 100f, 0, 170, d + 2050);
            Speech.RawTone(240, 55, -0.6f, 0.3f * Settings.KnockVolume / 100f, 0, 170, d + 2300);
            Speech.Tone(900, 35, 0.6f, 0.7f, 2, 1500, d + 3000);
            Speech.Tone(900, 35, 0.6f, 0.7f, 2, 1500, d + 3060);
            Speech.Tone(180, 140, 0, 0.8f, 2, 120, d + 3700);
            Speech.Tone(500, 220, 0, 0.7f, 0, 900, d + 4300);
            Speech.Tone(300, 260, -0.7f, 0.8f, 0, 150, d + 5000);
            Speech.Tone(90, 160, 0.9f, 0.8f, 3, 90, d + 5700);
            Speech.Tone(660, 60, 0, 0.5f, 1, 990, d + 6300);
            Speech.Tone(1318, 50, 0, 0.45f, 0, 1318, d + 6900);
            Speech.Tone(1760, 70, 0, 0.45f, 0, 1760, d + 6955);
        }

        /// <summary>Ctrl+F12: writes all objects of interest of the current room for development.</summary>
        private static void SceneProtocol()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Szene ").Append(GameInfo.SceneName).Append(" ").Append(DateTime.Now.ToString("HH:mm:ss")).Append("\r\n");
                RCG.Player p = GameInfo.Player;
                Vector3 pp = p != null ? p.transform.position : Vector3.zero;
                sb.Append("Spieler ").Append(pp.ToString("F2")).Append(" GameState ").Append(GameState.CurrentState).Append("\r\n");
                foreach (Door d in UnityEngine.Object.FindObjectsOfType<Door>())
                    sb.Append("Tuer ").Append(GameInfo.Path(d.transform)).Append(" pos ").Append(d.transform.position.ToString("F2"))
                      .Append(" nach ").Append(d.DoorData.ToScene).Append("/").Append(d.DoorData.ToDoor).Append(" Richtung ").Append(d.DoorData.DoorEnterDirection)
                      .Append(" aktiv ").Append(d.isActiveAndEnabled).Append("\r\n");
                foreach (InteractEntity e in UnityEngine.Object.FindObjectsOfType<InteractEntity>())
                    sb.Append("Interaktion ").Append(e.GetType().Name).Append(" ").Append(GameInfo.Path(e.transform)).Append(" pos ").Append(e.transform.position.ToString("F2")).Append("\r\n");
                foreach (EnemyEnitity e in Combat.Enemies())
                    sb.Append("Gegner ").Append(e.GetType().Name).Append(" ").Append(e.ClassName).Append(" pos ").Append(e.transform.position.ToString("F2"))
                      .Append(" HP ").Append(e.Stamina).Append("/").Append(e.StartingStamina).Append(" Zustand ").Append(e.Fsm != null ? e.Fsm.GetCurrentState() : "").Append("\r\n");
                foreach (Pit pit in UnityEngine.Object.FindObjectsOfType<Pit>())
                    sb.Append("Grube ").Append(GameInfo.Path(pit.transform)).Append(" pos ").Append(pit.transform.position.ToString("F2")).Append("\r\n");
                foreach (Climable c in UnityEngine.Object.FindObjectsOfType<Climable>())
                    sb.Append("Kletter ").Append(GameInfo.Path(c.transform)).Append(" pos ").Append(c.transform.position.ToString("F2")).Append("\r\n");
                foreach (TMP_Text t in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
                    sb.Append("Text ").Append(GameInfo.Visible(t) ? "sichtbar " : "verdeckt ").Append(GameInfo.Path(t.transform)).Append(" = ").Append((t.text ?? "").Replace("\n", " ")).Append("\r\n");
                string f = Path.Combine(Mod.ProtoDir, "szene_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                File.WriteAllText(f, sb.ToString(), new UTF8Encoding(false));
                Geometry.Protocol("Strg+F12", false);
                Speech.Say(Loc.T("protocol_saved"), true);
            }
            catch (Exception e) { Log.Error("SceneProtocol", e); }
        }
    }
}
