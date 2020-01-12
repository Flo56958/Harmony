using System;
using System.Runtime.InteropServices;

namespace Harmony.Windows {
    internal struct INPUT {
        public InputType Type;
        public MOUSEKEYBOARDINPUT Data;
    }

    internal enum InputType : uint {
        Mouse = 0,
        Keyboard = 1
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MOUSEKEYBOARDINPUT {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBOARDINPUT Keyboard;
    }

    internal struct MOUSEINPUT {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    internal struct KEYBOARDINPUT {
        public ushort KeyCode;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
