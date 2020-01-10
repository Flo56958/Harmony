using System;
using System.Windows.Forms;

namespace Harmony {
    public class HarmonyPacket {

        public PacketType Type { get; set; }
        public dynamic Pack { get; set; }

        public enum PacketType {
            MousePacket,
            KeyBoardPacket,
            DisplayPacket,
            SaltPacket
        }

        public class MousePacket {
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int wParam { get; set; }
            public uint MouseData { get; set; }
            public uint Flags { get; set; }
            public uint Time { get; set; }
            public int DwExtraInfo { get; set; }
        }

        public class DisplayPacket {
            public System.Drawing.Rectangle[] screens { get; set; }
        }

        public class KeyboardPacket {
            public Keys key { get; set; }
            public int wParam { get; set; }
            public int pressedKeys { get; set; } //first bit ctrl; second bit alt; third bit shift; forth bit windows
        }

        public enum MouseActionType {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }
    }
}
