using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using Harmony.Windows;
using Newtonsoft.Json.Linq;

namespace Harmony {
    public static class NetworkCommunicator {
        private static BlockingCollection<HarmonyPacket> _toSend;
        private static BlockingCollection<HarmonyPacket> _toProcess;

        private static ConcurrentBag<Thread> _threads;

        //Only for Server
        private static ConcurrentDictionary<EndPoint, TcpClient> _connections;

        public static volatile int OnSlave;
        public static volatile byte[] _salt;

        public static void Init() {
            _toSend = new BlockingCollection<HarmonyPacket>();
            _toProcess = new BlockingCollection<HarmonyPacket>();
            _connections = new ConcurrentDictionary<EndPoint, TcpClient>();
            _threads = new ConcurrentBag<Thread>();
            var thread = MainWindow.Model.IsServer ? new Thread(ServerAccept) : new Thread(ClientConnect);
            thread.Start();
            _threads.Add(thread);
        }



        public static void SendAsync(HarmonyPacket p) {
            _toSend?.Add(p);
        }

        public static void Close() {
            if (_threads != null) {
                foreach (var thread in _threads) {
                    thread?.Abort();
                }
            }

            if (_connections != null) {
                foreach (var (_, value) in _connections) {
                    value.Close();
                    value.Dispose();
                }
            }

            _toSend?.Dispose();
            _toProcess?.Dispose();
        }

        private static void ServerAccept() {
            _salt = Crypto.Init(MainWindow.Password);
            var serverReceive = new Thread(ServerProcessReceived);
            serverReceive.Start();
            _threads.Add(serverReceive);

            if (!int.TryParse(MainWindow.Model.Port, out var port)) return;
            var tcpIn = new TcpListener(IPAddress.Any, port);
            tcpIn.Start();

            var connectedClients = 0;
            while (connectedClients < 1) {
                var tcpInClient = tcpIn.AcceptTcpClient();
                var endpoint = tcpInClient.Client.RemoteEndPoint;
                if (!_connections.TryAdd(endpoint, tcpInClient)) {
                    tcpInClient.Close();
                    tcpInClient.Dispose();
                    continue;
                }

                var send = new Thread(ServerClientHandler);
                send.Start(endpoint);
                _threads.Add(send);

                connectedClients++;
            }
            tcpIn.Stop();
        }

        //This is the Method that handles the Client->Server Communication on the Server
        private static void ServerReceive(object obj) {
            if (!_connections.TryGetValue((EndPoint) obj, out var client)) return;
            var stream = client.GetStream();

            while (stream.CanRead) {
                var hp = HarmonyPacket.ReadPacket(stream);
                _toProcess.Add(hp);
            }
        }

        private static void ServerProcessReceived() {
            while (true) {
                var hp = _toProcess.Take();

                switch (hp.Type) {
                    case HarmonyPacket.PacketType.MouseMovePacket:
                        break;
                    case HarmonyPacket.PacketType.MousePacket:
                        break;
                    case HarmonyPacket.PacketType.KeyBoardPacket:
                        if (OnSlave == 0) {
                            Keyboard.SendInput(hp.Pack);
                        }
                        else {
                            SendAsync(hp);
                        }
                        break;
                    case HarmonyPacket.PacketType.DisplayPacket:
                        break;
                    case HarmonyPacket.PacketType.SaltPacket:
                        break;
                    case HarmonyPacket.PacketType.MediaControl:
                        switch (((HarmonyPacket.MediaControlPacket) hp.Pack).Action) {
                            case HarmonyPacket.MediaControlPacket.MediaAction.PlayPause:
                                MediaControl.PlayPause();
                                break;
                            case HarmonyPacket.MediaControlPacket.MediaAction.Stop:
                                MediaControl.Stop();
                                break;
                            case HarmonyPacket.MediaControlPacket.MediaAction.SkipPrevious:
                                MediaControl.SkipPrevious();
                                break;
                            case HarmonyPacket.MediaControlPacket.MediaAction.SkipForward:
                                MediaControl.SkipForward();
                                break;
                            case HarmonyPacket.MediaControlPacket.MediaAction.Reload:
                                MediaControl.Reload();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void ServerClientHandler(object obj) {
            if (!_connections.TryGetValue((EndPoint) obj, out var client)) return;
            MainWindow.Log($"Connected to {obj} as Server!", false);
            var stream = client.GetStream();
            {
                var saltPacket = HarmonyPacket.Encode(new HarmonyPacket()
                {
                    Type = HarmonyPacket.PacketType.SaltPacket,
                    Pack = _salt
                });

                stream.Write(saltPacket, 0, saltPacket.Length);
                stream.Flush();
                MainWindow.Log("Send Salt-Packet!", false);
            }

            var keyMap = new Dictionary<Keys, bool>();
            foreach (Keys key in Enum.GetValues(typeof(Keys))) {
                keyMap[key] = false;
            }

            {
                var displayPacket = HarmonyPacket.ReadPacket(stream);
                if (displayPacket.Type != HarmonyPacket.PacketType.DisplayPacket) {
                    MainWindow.Log(
                        $"Received unexpected Packet-Type! Expected: {HarmonyPacket.PacketType.SaltPacket}; Actual: {displayPacket.Type}",
                        true);
                    client.Close();
                    _ = _connections.TryRemove((EndPoint) obj, out _);
                    return;
                }

                foreach (var d in ((HarmonyPacket.DisplayPacket) displayPacket.Pack).screens) {
                    if (d.Screen.Location.X == 0 && d.Screen.Location.Y == 0) DisplayManager.SlaveMain = d;
                    d.OwnDisplay = false;
                    DisplayManager.AddRight(d);
                }
            }

            {
                var returnPacket = HarmonyPacket.Encode(new HarmonyPacket
                {
                    Type = HarmonyPacket.PacketType.DisplayPacket,
                    Pack = new HarmonyPacket.DisplayPacket
                    {
                        screens = DisplayManager.Displays
                    }
                });
                stream.Write(returnPacket, 0, returnPacket.Length);
                stream.Flush();
                MainWindow.Log($"Finished Handshake with Client {obj}!", false);
            }

            var receive = new Thread(ServerReceive);
            receive.Start(obj);
            _threads.Add(receive);

            var mouseX = 0;
            var mouseY = 0;
            while (stream.CanWrite) {
                var hp = _toSend.Take();
                if (hp.Type == HarmonyPacket.PacketType.MouseMovePacket) {
                    var mp = (HarmonyPacket.MouseMovePacket) hp.Pack;

                    if (OnSlave == 0) {
                        var onScreen = DisplayManager.GetDisplayFromPoint(mp.PosX, mp.PosY);
                        if (onScreen == null) continue;
                        mouseX = mp.PosX;
                        mouseY = mp.PosY;
                        if (onScreen.OwnDisplay) {
                            continue;
                        }

                        OnSlave = 1;
                    }
                    else {
                        var pos = NativeMethods.GetCursorPosition();
                        mouseX += mp.PosX - pos.X;
                        mouseY += mp.PosY - pos.Y;
                        var onScreen = DisplayManager.GetDisplayFromPoint(mouseX, mouseY);
                        if (onScreen == null) {
                            mouseX -= mp.PosX - pos.X;
                            mouseY -= mp.PosY - pos.Y;
                            continue;
                        }

                        if (onScreen.OwnDisplay) {
                            NativeMethods.SetCursorPos(mouseX, mouseY);
                            OnSlave = 0;
                        }

                        mp.PosX = mouseX;
                        mp.PosY = mouseY;
                    }
                }

                if (OnSlave == 0 && hp.Type != HarmonyPacket.PacketType.DisplayPacket) continue;
                var packet = HarmonyPacket.Encode(hp);
                stream.Write(packet, 0, packet.Length);
                stream.Flush();
            }
        }

        private static void ClientConnect() {
            if (!int.TryParse(MainWindow.Model.Port, out var port)) return;
            var tcpClient = new TcpClient(MainWindow.Model.IpAddress, port);

            var stream = tcpClient.GetStream();
            MainWindow.Log($"Connected to {tcpClient.Client.RemoteEndPoint} as Client!", false);

            var saltPacket = HarmonyPacket.ReadPacket(stream);
            if (saltPacket.Type != HarmonyPacket.PacketType.SaltPacket) {
                MainWindow.Log($"Received unexpected Packet-Type! Expected: {HarmonyPacket.PacketType.SaltPacket}; Actual: {saltPacket.Type}", true);
                tcpClient.Close();
                return;
            }

            _salt = saltPacket.Pack;
            Crypto.Init(MainWindow.Password, _salt);
            MainWindow.Log("Successfully obtained Salt-Packet!", false);

            if (!_connections.TryAdd(tcpClient.Client.RemoteEndPoint, tcpClient)) return;
            var sendthread = new Thread(ClientSend);
            _threads.Add(sendthread);
            sendthread.Start(tcpClient.Client.RemoteEndPoint);

            {
                var displayPacket = HarmonyPacket.Encode(new HarmonyPacket
                {
                    Type = HarmonyPacket.PacketType.DisplayPacket,
                    Pack = new HarmonyPacket.DisplayPacket
                    {
                        screens = DisplayManager.Displays
                    }
                });
                stream.Write(displayPacket, 0, displayPacket.Length);
                stream.Flush();
                MainWindow.Log("Send Display-Packet!", false);
            }

            {
                var displayPacket = HarmonyPacket.ReadPacket(stream);
                if (displayPacket.Type != HarmonyPacket.PacketType.DisplayPacket) {
                    MainWindow.Log($"Received unexpected Packet-Type! Expected: {HarmonyPacket.PacketType.DisplayPacket}; Actual: {displayPacket.Type}", true);
                    stream.Close();
                    tcpClient.Close();
                    return;
                }
                DisplayManager.SetUp(((HarmonyPacket.DisplayPacket) displayPacket.Pack).screens);
                MainWindow.Log("Finished Handshake with Server!", false);
            }


            while (stream.CanRead) {
                var packet = HarmonyPacket.ReadPacket(stream);

                switch (packet.Type) {
                    case HarmonyPacket.PacketType.MousePacket:
                        Mouse.SendInput(packet.Pack);
                        break;

                    case HarmonyPacket.PacketType.KeyBoardPacket:
                        var kp = (HarmonyPacket.KeyboardPacket) packet.Pack;
                        Keyboard.SendInput(kp);
                        break;

                    case HarmonyPacket.PacketType.DisplayPacket:
                        var dp = ((JObject)packet.Pack).ToObject<HarmonyPacket.DisplayPacket>();
                        DisplayManager.SetUp(dp.screens);
                        break;
                    case HarmonyPacket.PacketType.MouseMovePacket:
                        var mmp = (HarmonyPacket.MouseMovePacket) packet.Pack;
                        NativeMethods.SetCursorPos(mmp.PosX - DisplayManager.SlaveMain.Location.X, mmp.PosY - DisplayManager.SlaveMain.Location.Y);
                        break;
                    case HarmonyPacket.PacketType.SaltPacket:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void ClientSend(object obj) {
            if (!_connections.TryGetValue((EndPoint) obj, out var client)) return;
            var stream = client.GetStream();

            while (stream.CanWrite) {
                var hp = _toSend.Take();
                stream.Write(HarmonyPacket.Encode(hp));
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