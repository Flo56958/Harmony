using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Harmony.Windows;
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

        public static volatile int onSlave;

        public NetworkCommunicator(string address, int port, bool isMaster) {
            if (Instance != null) return;
            _address = address;
            _port = port;
            _communicationThread = isMaster ? new Thread(PackAndSend) : new Thread(ListenAndUnpack);

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

            var keyMap = new Dictionary<Keys, bool>();
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
                if (dis.Screen.Location.X == 0 && dis.Screen.Location.Y == 0) DisplayManager.SlaveMain = dis;
                dis.OwnDisplay = false;
                DisplayManager.AddRight(dis);
            }

            _tx.WriteLine(Crypto.Encrypt(JsonConvert.SerializeObject(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.Displays
                }
            })));
            MainWindow.Log("Finished Handshake with Slave!", false);
            DisplayManager.PrintScreenConfiguration();

            var mouseX = 0;
            var mouseY = 0;
            var mouseMX = 0;
            var mouseMY = 0;

            while (true) {
                var hp = _blockingCollection.Take();
                if (hp.Type == HarmonyPacket.PacketType.MousePacket) {
                    var mp = (HarmonyPacket.MousePacket)hp.Pack;

                    if (onSlave == 0) {
                        var onScreen = DisplayManager.GetDisplayFromPoint(mp.PosX, mp.PosY);
                        if (onScreen == null) continue;
                        mouseX = mp.PosX;
                        mouseY = mp.PosY;
                        if (onScreen.OwnDisplay) {
                            mouseMX = mp.PosX;
                            mouseMY = mp.PosY;
                            continue;
                        }

                        onSlave = 1;
                    }
                    else {
                        mouseX += mp.PosX - mouseMX;
                        mouseY += mp.PosY - mouseMY;
                        var onScreen = DisplayManager.GetDisplayFromPoint(mouseX, mouseY);
                        if (onScreen == null) {
                            mouseX -= mp.PosX - mouseMX;
                            mouseY -= mp.PosY - mouseMY;
                            continue;
                        }

                        if (onScreen.OwnDisplay) {
                            NativeMethods.SetCursorPos(mouseX, mouseY);
                            onSlave = 0;
                        }
                        mp.PosX = mouseX;
                        mp.PosY = mouseY;

                    }
                }

                if (onSlave == 0 && hp.Type != HarmonyPacket.PacketType.DisplayPacket) continue;

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
            Crypto.Init(MainWindow.Password, Convert.FromBase64String(saltPacketReal.Pack));
            MainWindow.Log("Successfully obtained Salt-Packet!", false);

            _tx.WriteLine(Crypto.Encrypt(JsonConvert.SerializeObject(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.Displays
                }
            })));
            MainWindow.Log("Send Display-Packet!", false);

            var displayPacket = Crypto.Decrypt(_rx.ReadLine());
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
                        var mp = ((JObject)packet.Pack).ToObject<HarmonyPacket.MousePacket>();

                        var d = DisplayManager.GetDisplayFromPoint(mp.PosX, mp.PosY);
                        if (d == null) continue;

                        if (d.OwnDisplay) {
                            //TODO: Show Mouse when hidden
                            if (mp.Action == (uint)MouseFlag.Move) { //Move
                                NativeMethods.SetCursorPos(mp.PosX - DisplayManager.SlaveMain.Location.X, mp.PosY - DisplayManager.SlaveMain.Location.Y); //Needs Elevation!!
                            }
                            else {
                                Mouse.SendInput(mp);
                            }

                        }

                        break;

                    case HarmonyPacket.PacketType.KeyBoardPacket:
                        var kp = ((JObject)packet.Pack).ToObject<HarmonyPacket.KeyboardPacket>();
                        Keyboard.SendInput(kp);
                        break;

                    case HarmonyPacket.PacketType.DisplayPacket:
                        var dp = ((JObject)packet.Pack).ToObject<HarmonyPacket.DisplayPacket>();
                        DisplayManager.SetUp(dp.screens);
                        break;

                }
            }
        }

        public static string GetLocalIPAddress() {
            string localIP;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint?.Address.ToString();
            }

            return localIP;
        }
    }
}