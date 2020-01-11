using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Harmony {
    public class NetworkCommunicator {
        public static NetworkCommunicator Instance;
        private StreamWriter _tx;
        private StreamReader _rx;
        private readonly BlockingCollection<HarmonyPacket> _blockingCollection;

        private readonly string _address;
        private readonly int _port;

        private readonly Thread _communicationThread;

        public NetworkCommunicator(string address, int port, bool isMaster) {
            if (Instance != null) return;
            this._address = address;
            this._port = port;
            _communicationThread = isMaster ? new Thread(new ThreadStart(PackAndSend)) : new Thread(new ThreadStart(ListenAndUnpack));

            _blockingCollection = new BlockingCollection<HarmonyPacket>();
            _communicationThread.Start();
            Instance = this;
        }

        public void SendAsync(HarmonyPacket p) {
            _blockingCollection.Add(p);
        }

        public void Close() {
            _communicationThread.Abort();
            _tx?.Close();
            _rx?.Close();
        }

        private void PackAndSend() {
            var tcpOut = new TcpClient(_address, _port);

            _tx = new StreamWriter(tcpOut.GetStream()) { AutoFlush = true };
            _rx = new StreamReader(tcpOut.GetStream(), Encoding.UTF8);

            MainWindow.Log($"Connected to {tcpOut.Client.RemoteEndPoint} as Master!", false);

            var salt = Crypto.Init(MainWindow.Password);
            var saltPacket = new HarmonyPacket {
                Type = HarmonyPacket.PacketType.SaltPacket,
                Pack = Convert.ToBase64String(salt)
            };

            _tx.WriteLine(JsonConvert.SerializeObject(saltPacket));
            MainWindow.Log("Send Salt-Packet!", false);

            var keyMap = new System.Collections.Generic.Dictionary<Keys, bool>();
            foreach (Keys key in Enum.GetValues(typeof(Keys))) {
                keyMap[key] = false;
            }

            var displayPacket = _rx.ReadLine();
            if (displayPacket == null) {
                MainWindow.Log("Did not receive Displays from Slave!", true);
                tcpOut.Close();
                return;
            }
            displayPacket = Crypto.Decrypt(displayPacket);

            var displayPacketReal = JsonConvert.DeserializeObject<HarmonyPacket>(displayPacket);
            if (displayPacketReal.Type != HarmonyPacket.PacketType.DisplayPacket) {
                MainWindow.Log($"Received unexpected Packet-Type! Expected: {HarmonyPacket.PacketType.SaltPacket.ToString()}; Actual: {displayPacketReal.Type.ToString()}", true);
                tcpOut.Close();
                return;
            }

            var displays = ((JObject)displayPacketReal.Pack).ToObject<HarmonyPacket.DisplayPacket>();
            foreach (var dis in displays.screens) {
                DisplayManager.AddRight(dis);
            }

            _tx.WriteLine(Crypto.Encrypt(JsonConvert.SerializeObject(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.displays
                }
            })));
            MainWindow.Log("Finished Handshake with Slave!", false);
            DisplayManager.PrintScreenConfiguration();

            while (true) {
                var hp = _blockingCollection.Take();
                switch (hp.Type) {
                    case HarmonyPacket.PacketType.KeyBoardPacket:
                        var kp = (HarmonyPacket.KeyboardPacket) hp.Pack;
                        if (kp.wParam == (int) KeyboardHook.KeyEvent.WM_KEYDOWN) {
                            keyMap[kp.key] = true;
                        } else if (kp.wParam == (int) KeyboardHook.KeyEvent.WM_KEYUP) {
                            keyMap[kp.key] = false;
                        }

                        if (keyMap[Keys.Control] || keyMap[Keys.ControlKey] || keyMap[Keys.LControlKey] || keyMap[Keys.RControlKey]) {
                            kp.pressedKeys |= 1;
                        }

                        if (keyMap[Keys.Alt] || keyMap[Keys.LMenu] || keyMap[Keys.RMenu]) {
                            kp.pressedKeys |= 2;
                        }

                        if (keyMap[Keys.Shift] || keyMap[Keys.ShiftKey] || keyMap[Keys.LShiftKey] ||
                            keyMap[Keys.RShiftKey]) {
                            kp.pressedKeys |= 4;
                        }

                        if (keyMap[Keys.LWin] || keyMap[Keys.RWin]) {
                            kp.pressedKeys |= 8;
                        }
                        break;
                }

                _tx.WriteLine(Crypto.Encrypt(JsonConvert.SerializeObject(hp)));
            }
        }

        private void ListenAndUnpack() {
            var tcpIn = new TcpListener(IPAddress.Any, _port);
            tcpIn.Start();
            var tcpInClient = tcpIn.AcceptTcpClient();

            _tx = new StreamWriter(tcpInClient.GetStream()) { AutoFlush = true };
            _rx = new StreamReader(tcpInClient.GetStream(), Encoding.UTF8);
            MainWindow.Log($"Connected to {tcpInClient.Client.RemoteEndPoint} as Slave!", false);

            var saltPacket = _rx.ReadLine();
            if (saltPacket == null) {
                MainWindow.Log("Did not receive Salt!", true);
                tcpInClient.Close();
                tcpIn.Stop();
                return;
            }

            var saltPacketReal = JsonConvert.DeserializeObject<HarmonyPacket>(saltPacket);
            if (saltPacketReal.Type != HarmonyPacket.PacketType.SaltPacket) {
                MainWindow.Log($"Received unexpected Packet-Type! Expected: {HarmonyPacket.PacketType.SaltPacket.ToString()}; Actual: {saltPacketReal.Type.ToString()}", true);
                tcpInClient.Close();
                tcpIn.Stop();
                return;
            }
            Crypto.Init(Convert.FromBase64String(saltPacketReal.Pack), MainWindow.Password);
            MainWindow.Log("Successfully obtained Salt-Packet!", false);

            _tx.WriteLine(Crypto.Encrypt(JsonConvert.SerializeObject(new HarmonyPacket
            {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket
                {
                    screens = DisplayManager.displays
                }
            })));
            MainWindow.Log("Send Display-Packet!", false);

            var displayPacket= Crypto.Decrypt(_rx.ReadLine());
            if (displayPacket == null) {
                MainWindow.Log("Did not receive Display-Packet!", true);
                tcpInClient.Close();
                tcpIn.Stop();
                return;
            }

            var displayPacketReal = JsonConvert.DeserializeObject<HarmonyPacket>(displayPacket);
            if (displayPacketReal.Type != HarmonyPacket.PacketType.DisplayPacket) {
                MainWindow.Log($"Received unexpected Packet-Type! Expected: {HarmonyPacket.PacketType.DisplayPacket.ToString()}; Actual: {displayPacketReal.Type.ToString()}", true);
                tcpInClient.Close();
                tcpIn.Stop();
                return;
            }
            DisplayManager.SetUp(((JObject)displayPacketReal.Pack).ToObject<HarmonyPacket.DisplayPacket>().screens);
            MainWindow.Log("Finished Handshake with Master!", false);
            DisplayManager.PrintScreenConfiguration();

            while (!_rx.EndOfStream) {
                var line = _rx.ReadLine();
                if (line == null) continue;
                line = Crypto.Decrypt(line);

                var packet = JsonConvert.DeserializeObject<HarmonyPacket>(line);
                if (packet == null) continue;

                switch (packet.Type) {
                    case HarmonyPacket.PacketType.MousePacket:
                        var mp = ((JObject) packet.Pack).ToObject<HarmonyPacket.MousePacket>();

                        //var input = new MouseHook.MouseInput() {
                        //    DwType = 1,
                        //    Mstruct = new MouseHook.Msllhookstruct()
                        //    {
                        //        x = mp.PosX,
                        //        y = mp.PosY,
                        //        flags = mp.Flags,
                        //        mouseData = mp.MouseData,
                        //        //dwExtraInfo = (IntPtr) mp.DwExtraInfo
                        //    }
                        //};

                        //var inputArr = new MouseHook.MouseInput[]
                        //{
                        //    input
                        //};

                        if (mp.wParam == 0x0200) { //Move
                            SetCursorPos(mp.PosX, mp.PosY); //Needs Elevation!!
                            //mouse_event(0x8000, mp.PosX, mp.PosY, (int)mp.MouseData, mp.DwExtraInfo);
                        } else {
                            //mouse_event(mp.wParam, 0, 0, (int)mp.MouseData, mp.DwExtraInfo);
                        }
                        //SendInput(1, inputArr, Marshal.SizeOf(input));
                        break;

                    case HarmonyPacket.PacketType.KeyBoardPacket:
                        //TODO: Keyboard stuff
                        var kp = ((Newtonsoft.Json.Linq.JObject)packet.Pack).ToObject<HarmonyPacket.KeyboardPacket>();

                        //var kinput = new KeyboardHook.KeyboardInput()
                        //{
                        //    DwType = 0,
                        //    Mstruct = new KeyboardHook.KEYBDINPUT()
                        //    {
                        //        dwFlags = (uint) kp.wParam,
                        //        wVk = (ushort) kp.key,
                        //        wScan = 0,

                        //    }
                        //};
                        //var kinputArr = new KeyboardHook.KeyboardInput[]
                        //{
                        //    kinput
                        //};
                        //SendInput(1, kinputArr, Marshal.SizeOf(kinput));

                        if (kp.wParam == (int)KeyboardHook.KeyEvent.WM_KEYDOWN) {

                            if (Keys.Control == kp.key || Keys.ControlKey == kp.key || Keys.LControlKey == kp.key || Keys.RControlKey == kp.key
                                || Keys.Alt == kp.key || Keys.LMenu == kp.key || Keys.RMenu == kp.key
                                || Keys.Shift == kp.key || Keys.ShiftKey == kp.key || Keys.LShiftKey == kp.key || Keys.RShiftKey == kp.key
                                || Keys.LWin == kp.key || Keys.RWin == kp.key) {
                                break;
                            }

                            var k = kp.key.ToString().ToUpper();
                            switch (kp.key) { //TODO: Add special characters
                                case Keys.Back:
                                    k = "BACKSPACE";
                                    break;
                                case Keys.Space:
                                    k = " ";
                                    break;
                                case Keys.Return:
                                    k = "ENTER";
                                    break;
                            }

                            if (k.StartsWith("OEM")) break;
                            var key = "";
                            if (kp.key.ToString().Length > 1 && k.Length > 1) {
                                key += "{" + k + "}";
                            } else {
                                key = k.ToLower();
                            }
                            if ((kp.pressedKeys & 4) != 0) {
                                key = "+" + key;
                            }
                            if ((kp.pressedKeys & 2) != 0) {
                                key = "%" + key;
                            }
                            if ((kp.pressedKeys & 1) != 0) {
                                key = "^" + key;
                            }
                            SendKeys.SendWait(key);
                        }
                        break;

                    case HarmonyPacket.PacketType.DisplayPacket:
                        //TODO: Display stuff
                        break;
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint cInputs, MouseHook.MouseInput[] input, int size); //Does not work (currently)

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint cInputs, KeyboardHook.KeyboardInput[] input, int size); //Does not work (currently)

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo); //Does not work (currently)

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

    }
}