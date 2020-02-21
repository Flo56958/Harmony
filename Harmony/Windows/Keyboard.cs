using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace Harmony.Windows {

    [Flags]
    internal enum KeyboardFlag : uint {
        ExtendedKey = 0x0001,
        KeyUp = 0x0002,
        Unicode = 0x0004,
        ScanCode = 0x0008
    }

    public static class Keyboard {
        public static bool IsExtendedKey(Keys keyCode) {
            return keyCode == Keys.Menu ||
                   keyCode == Keys.LMenu ||
                   keyCode == Keys.RMenu ||
                   keyCode == Keys.Control ||
                   keyCode == Keys.ControlKey ||
                   keyCode == Keys.LControlKey ||
                   keyCode == Keys.RControlKey ||
                   keyCode == Keys.Insert ||
                   keyCode == Keys.Delete ||
                   keyCode == Keys.Home ||
                   keyCode == Keys.End ||
                   keyCode == Keys.Prior ||
                   keyCode == Keys.Next ||
                   keyCode == Keys.Right ||
                   keyCode == Keys.Up ||
                   keyCode == Keys.Left ||
                   keyCode == Keys.Down ||
                   keyCode == Keys.NumLock ||
                   keyCode == Keys.Cancel ||
                   keyCode == Keys.Snapshot ||
                   keyCode == Keys.Divide;
        }

        public static bool SendInput([NotNull] HarmonyPacket.KeyboardPacket kp) {
            INPUT input;
            if (kp.wParam == 256) { //Down
                input = new INPUT {
                    Type = InputType.Keyboard,
                    Data = new MOUSEKEYBOARDINPUT() {
                        Keyboard = new KEYBOARDINPUT() {
                            KeyCode = (ushort)kp.key,
                            Scan = (ushort)(NativeMethods.MapVirtualKey((uint)kp.key, 0) & 0xFFU),
                            Flags = IsExtendedKey(kp.key) ? (uint)KeyboardFlag.ExtendedKey : 0
                        }
                    }
                };
            }
            else { //Up: should be 257
                input = new INPUT {
                    Type = InputType.Keyboard,
                    Data = new MOUSEKEYBOARDINPUT() {
                        Keyboard = new KEYBOARDINPUT() {
                            KeyCode = (ushort)kp.key,
                            Scan = (ushort)(NativeMethods.MapVirtualKey((uint)kp.key, 0) & 0xFFU),
                            Flags = (uint)(IsExtendedKey(kp.key) ? KeyboardFlag.KeyUp | KeyboardFlag.ExtendedKey : KeyboardFlag.KeyUp)
                        }
                    }
                };
            }
            return NativeMethods.SendInput(1, new INPUT[] { input }, Marshal.SizeOf(input)) == 0;
        }
    }

    public static class KeyboardHook {

        private static NativeMethods.HookProc _proc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;

        public static void Start() {
            _hookId = SetHook(_proc);
        }

        public static void Stop() {
            NativeMethods.UnhookWindowsHookEx(_hookId);
        }

        private static IntPtr SetHook(NativeMethods.HookProc proc) {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule) {
                return NativeMethods.SetWindowsHookEx(13, proc, NativeMethods.GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            NetworkCommunicator.SendAsync(new HarmonyPacket() {
                Type = HarmonyPacket.PacketType.KeyBoardPacket,
                Pack = new HarmonyPacket.KeyboardPacket() {
                    wParam = wParam.ToInt32(),
                    key = (Keys)Marshal.ReadInt32(lParam),
                }
            });

            return (IntPtr)NetworkCommunicator.OnSlave + (int)NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
