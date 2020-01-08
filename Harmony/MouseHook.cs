using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Harmony {
    public static class MouseHook {
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
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                  GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private const int WH_MOUSE_LL = 14;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode < 0) return CallNextHookEx(_hookId, nCode, wParam, lParam);
            var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            var package = new MousePackage
            {
                PosX = hookStruct.pt.X, PosY = hookStruct.pt.Y, MouseData = hookStruct.mouseData, Action = (MouseActionType)wParam
            };

            NetworkCommunicator.instance.SendAsync(package);
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {
            public int X;
            public int Y;

            public POINT(int x, int y) {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p) {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p) {
                return new POINT(p.X, p.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT //Für passives Abfragen der Mausbewegung
        {
            public readonly POINT pt;
            public readonly uint mouseData;
            private readonly uint flags;
            private readonly uint time;
            private readonly IntPtr dwExtraInfo;
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
