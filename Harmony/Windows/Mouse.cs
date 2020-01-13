using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Harmony.Windows {
    [Flags]
    public enum MouseFlag : uint {
        Move = 0x0001,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        XDown = 0x0080,
        XUp = 0x0100,
        VerticalWheel = 0x0800,
        HorizontalWheel = 0x1000,
        VirtualDesk = 0x4000,
        Absolute = 0x8000,
    }

    public static class Mouse {                                                                                                                                                               

        public static uint MapWParamToFlags(int wParam) {
            switch (wParam) {
                case 0x200:
                    return (uint)MouseFlag.Move;
                case 0x201:
                    return (uint)MouseFlag.LeftDown;
                case 0x202:
                    return (uint)MouseFlag.LeftUp;
                case 0x204:
                    return (uint)MouseFlag.RightDown;
                case 0x205:
                    return (uint)MouseFlag.RightUp;
                case 0x207:
                    return (uint)MouseFlag.MiddleDown;
                case 0x208:
                    return (uint)MouseFlag.MiddleUp;
                case 0x20A:
                    return (uint)MouseFlag.VerticalWheel;
                case 0x20B:
                    return (uint)MouseFlag.XDown;
                case 0x20C:
                    return (uint)MouseFlag.XUp;
                case 0x20E:
                    return (uint)MouseFlag.HorizontalWheel;
            }

            return 0;
        }

        public static bool SendInput(HarmonyPacket.MousePacket mp) {
            INPUT input;
            if (mp.Action == 0) return false;
            else if (mp.Action == (uint)MouseFlag.VerticalWheel || mp.Action == (uint)MouseFlag.HorizontalWheel
                || mp.Action == (uint)MouseFlag.XDown || mp.Action == (uint)MouseFlag.XUp) {
                input = new INPUT() {
                    Type = InputType.Mouse,
                    Data = new MOUSEKEYBOARDINPUT() {
                        Mouse = new MOUSEINPUT() {
                            Flags = mp.Flags | mp.Action,
                            MouseData = (uint)(short)(mp.MouseData >> 16),
                        }
                    }
                };
            }
            else {
                input = new INPUT() {
                    Type = InputType.Mouse,
                    Data = new MOUSEKEYBOARDINPUT() {
                        Mouse = new MOUSEINPUT() {
                            Flags = mp.Flags | mp.Action,
                            MouseData = mp.MouseData,
                        }
                    }
                };
            }
            return NativeMethods.SendInput(1, new INPUT[] { input }, Marshal.SizeOf(input)) == 0;
        }
    }

    public static class MouseHook {

        private static readonly NativeMethods.HookProc Proc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;

        public static void Start() {
            _hookId = SetHook(Proc);
        }

        public static void Stop() {
            NativeMethods.UnhookWindowsHookEx(_hookId);
        }

        private static IntPtr SetHook(NativeMethods.HookProc proc) {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule) {
                return NativeMethods.SetWindowsHookEx(14, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode < 0) return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            var hookStruct = (MOUSEINPUT)Marshal.PtrToStructure(lParam, typeof(MOUSEINPUT));

            NetworkCommunicator.Instance?.SendAsync(new HarmonyPacket() {
                Type = HarmonyPacket.PacketType.MousePacket,
                Pack = new HarmonyPacket.MousePacket {
                    PosX = hookStruct.X,
                    PosY = hookStruct.Y,
                    MouseData = hookStruct.MouseData,
                    Action = Mouse.MapWParamToFlags(wParam.ToInt32()),
                    Flags = hookStruct.Flags,
                }
            });
            return (IntPtr)NetworkCommunicator.onSlave + (int)NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
