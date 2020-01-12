using System.Collections.Generic;
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
            public uint Action { get; set; }
            public uint MouseData { get; set; }
            public uint Flags { get; set; }
        }

        public class DisplayPacket {
            public List<DisplayManager.Display> screens { get; set; }
        }

        public class KeyboardPacket {
            public Keys key { get; set; }
            public int wParam { get; set; }
        }
    }
}
