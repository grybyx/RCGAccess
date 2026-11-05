using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RCGAccess
{
    public enum Pad
    {
        Up = 0x0001, Down = 0x0002, Left = 0x0004, Right = 0x0008,
        Start = 0x0010, Back = 0x0020, LS = 0x0040, RS = 0x0080,
        LB = 0x0100, RB = 0x0200, A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000
    }

    public enum StickDir { None, Up, Down, Left, Right }

    /// <summary>Keyboard (Unity) and XInput controller reading with edge detection.</summary>
    public static class ModInput
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct XGamepad
        {
            public ushort Buttons;
            public byte LT, RT;
            public short LX, LY, RX, RY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XState
        {
            public uint Packet;
            public XGamepad Pad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] private static extern int XGet14(int i, out XState s);
        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")] private static extern int XGet13(int i, out XState s);
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")] private static extern int XGet910(int i, out XState s);

        private static int _dll = 0; // 0 = 1_4, 1 = 1_3, 2 = 9_1_0, 3 = none
        private static ushort _btn, _prevBtn;
        private static float _lx, _ly, _rx, _ry, _lt, _rt, _ltPrev, _rtPrev;
        private static StickDir _rStick = StickDir.None, _rStickPrev = StickDir.None;
        private static StickDir _lStick = StickDir.None, _lStickPrev = StickDir.None;
        private static bool _ctrlDown;
        private static bool _ctrlOther;
        private static bool _ctrlAlone;
        private static int _padIndex = -1;
        private static float _nextScan;
        private static float _repeatTimer;
        private static int _repeatKey;
        public static bool PadConnected { get { return _padIndex >= 0; } }

        private static int GetState(int i, out XState s)
        {
            s = new XState();
            try
            {
                switch (_dll)
                {
                    case 0: return XGet14(i, out s);
                    case 1: return XGet13(i, out s);
                    case 2: return XGet910(i, out s);
                    default: return -1;
                }
            }
            catch (DllNotFoundException) { _dll++; return -1; }
            catch (EntryPointNotFoundException) { _dll++; return -1; }
        }

        public static void Poll()
        {
            _prevBtn = _btn;
            _ltPrev = _lt; _rtPrev = _rt;
            _rStickPrev = _rStick;
            _lStickPrev = _lStick;
            _btn = 0;
            XState s;
            if (_padIndex < 0)
            {
                if (Time.realtimeSinceStartup >= _nextScan)
                {
                    _nextScan = Time.realtimeSinceStartup + 2f;
                    for (int i = 0; i < 4; i++)
                        if (GetState(i, out s) == 0) { _padIndex = i; Log.Write("Controller gefunden (XInput " + i + ")."); break; }
                }
            }
            if (_padIndex >= 0)
            {
                if (GetState(_padIndex, out s) == 0)
                {
                    _btn = s.Pad.Buttons;
                    _lx = Norm(s.Pad.LX); _ly = Norm(s.Pad.LY); _rx = Norm(s.Pad.RX); _ry = Norm(s.Pad.RY);
                    _lt = s.Pad.LT / 255f; _rt = s.Pad.RT / 255f;
                }
                else { _padIndex = -1; _lx = _ly = _rx = _ry = 0; _lt = _rt = 0; }
            }
            _rStick = Dir(_rx, _ry, _rStickPrev);
            _lStick = Dir(_lx, _ly, _lStickPrev);

            // Ctrl alone: pressed and released without another key in between
            _ctrlAlone = false;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && !_ctrlDown) { _ctrlDown = true; _ctrlOther = false; }
            else if (ctrl && _ctrlDown)
            {
                if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.LeftControl) && !Input.GetKeyDown(KeyCode.RightControl)) _ctrlOther = true;
            }
            else if (!ctrl && _ctrlDown)
            {
                _ctrlDown = false;
                if (!_ctrlOther) _ctrlAlone = true;
            }
        }

        private static float Norm(short v) { return Mathf.Clamp(v / 32767f, -1f, 1f); }

        private static StickDir Dir(float x, float y, StickDir prev)
        {
            float on = 0.65f, off = 0.35f;
            if (prev != StickDir.None)
            {
                // hysteresis: keep the direction until the stick is nearly centred
                float m = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                if (m > off) return prev;
            }
            if (y > on && Mathf.Abs(y) >= Mathf.Abs(x)) return StickDir.Up;
            if (y < -on && Mathf.Abs(y) >= Mathf.Abs(x)) return StickDir.Down;
            if (x > on) return StickDir.Right;
            if (x < -on) return StickDir.Left;
            return StickDir.None;
        }

        /// <summary>True on the frame Ctrl was released without any other key (stops speech).</summary>
        public static bool CtrlAlone() { return _ctrlAlone; }
        public static bool CtrlNow() { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); }
        public static bool Shift() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
        public static bool Alt() { return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt); }

        public static bool Key(KeyCode k) { return Input.GetKeyDown(k); }
        public static bool KeyPlain(KeyCode k) { return Input.GetKeyDown(k) && !Shift() && !CtrlNow() && !Alt(); }
        public static bool KeyShift(KeyCode k) { return Input.GetKeyDown(k) && Shift() && !CtrlNow(); }
        public static bool KeyCtrl(KeyCode k) { return Input.GetKeyDown(k) && CtrlNow() && !Shift(); }

        public static bool PadDown(Pad b) { return (_btn & (ushort)b) != 0 && (_prevBtn & (ushort)b) == 0; }
        public static bool PadHeld(Pad b) { return (_btn & (ushort)b) != 0; }
        public static bool AnyPadButton() { return _btn != 0 || _lt > 0.5f || _rt > 0.5f; }
        public static bool TriggerDown(bool left)
        {
            return left ? (_lt > 0.6f && _ltPrev <= 0.6f) : (_rt > 0.6f && _rtPrev <= 0.6f);
        }
        public static float LeftX { get { return _lx; } }
        public static float LeftY { get { return _ly; } }

        public static StickDir RightStickFlick() { return _rStick != _rStickPrev ? _rStick : StickDir.None; }
        public static StickDir LeftStickFlick() { return _lStick != _lStickPrev ? _lStick : StickDir.None; }

        /// <summary>Menu navigation with auto repeat: -1 up / +1 down / -2 left / +2 right.</summary>
        public static int MenuMove()
        {
            int k = 0;
            if (Input.GetKey(KeyCode.UpArrow) || PadHeld(Pad.Up) || _lStick == StickDir.Up) k = -1;
            else if (Input.GetKey(KeyCode.DownArrow) || PadHeld(Pad.Down) || _lStick == StickDir.Down) k = 1;
            else if (Input.GetKey(KeyCode.LeftArrow) || PadHeld(Pad.Left) || _lStick == StickDir.Left) k = -2;
            else if (Input.GetKey(KeyCode.RightArrow) || PadHeld(Pad.Right) || _lStick == StickDir.Right) k = 2;
            if (k == 0) { _repeatKey = 0; return 0; }
            if (k != _repeatKey) { _repeatKey = k; _repeatTimer = Time.realtimeSinceStartup + 0.45f; return k; }
            if (Time.realtimeSinceStartup >= _repeatTimer) { _repeatTimer = Time.realtimeSinceStartup + 0.12f; return k; }
            return 0;
        }
    }
}
