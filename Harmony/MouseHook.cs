using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Harmony {
    public static class MouseHook {

        private const int WH_MOUSE_LL = 14;
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static void Start() {
            _hookId = SetHook(Proc);
        }

        public static void Stop() {
            UnhookWindowsHookEx(_hookId);
        }

        private static readonly LowLevelMouseProc Proc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelMouseProc proc) {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule) {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode < 0) return CallNextHookEx(_hookId, nCode, wParam, lParam);
            var hookStruct = (Msllhookstruct) Marshal.PtrToStructure(lParam, typeof(Msllhookstruct));

            NetworkCommunicator.Instance?.SendAsync(new HarmonyPacket() { 
                Type = HarmonyPacket.PacketType.MousePacket, 
                Pack = new HarmonyPacket.MousePacket {
                    PosX = hookStruct.x,
                    PosY = hookStruct.y,
                    MouseData = hookStruct.mouseData,
                    wParam = wParam.ToInt32(),
                    Flags = hookStruct.flags,
                    DwExtraInfo = hookStruct.dwExtraInfo.ToInt32(),
                    Time = hookStruct.time
                }
            });
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Msllhookstruct {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public struct MouseInput {
            public uint DwType;
            public Msllhookstruct Mstruct;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}