using System;

namespace Harmony {
    public class HarmonyPacket {

        public PacketType Type { get; set; }
        public string Packet { get; set; }

        public enum PacketType {
            MousePacket,
            KeyBoardPacket
        }

        public struct MousePacket {
            public int PosX;
            public int PosY;
            public int Action;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr DwExtraInfo;
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
