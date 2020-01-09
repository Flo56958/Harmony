using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Harmony {
    public class NetworkCommunicator {
        public static NetworkCommunicator Instance;
        private StreamWriter _tx;
        private StreamReader _rx;
        private readonly BlockingCollection<(HarmonyPacket.PacketType, object)> _blockingCollection;

        private readonly string address;
        private readonly int port;

        private readonly Thread _communicationThread;

        public NetworkCommunicator(string address, int port, bool isMaster) {
            if (Instance != null) return;
            this.address = address;
            this.port = port;
            _communicationThread = isMaster ? new Thread(new ThreadStart(PackAndSend)) : new Thread(new ThreadStart(ListenAndUnpack));

            _blockingCollection = new BlockingCollection<(HarmonyPacket.PacketType, object)>();
            _communicationThread.Start();
            Instance = this;
            MainWindow.debug.Text += "\nSuccessfully connected!";
        }

        public void SendAsync((HarmonyPacket.PacketType, object) p) {
            _blockingCollection.Add(p);
        }

        public void Close() {
            _communicationThread.Abort();
            if (_rx == null) {
                _tx.Close();
            }
            else {
                _rx.Close();
            }
        }

        private void PackAndSend() {
            var tcpOut = new TcpClient(address, port);

            _tx = new StreamWriter(tcpOut.GetStream()) { AutoFlush = true };
            Debug.WriteLine("Connected!");
            while (true) {
                var o = _blockingCollection.Take();
                HarmonyPacket hp = new HarmonyPacket() {Type = o.Item1};
                switch (o.Item1) {
                    case HarmonyPacket.PacketType.MousePacket:
                        hp.PacketStr = JsonConvert.SerializeObject((HarmonyPacket.MousePacket) o.Item2);
                        break;
                }
                _tx.WriteLine(JsonConvert.SerializeObject(hp));
            }
        }

        private void ListenAndUnpack() {
            var tcpIn = new TcpListener(IPAddress.Any, port);
            tcpIn.Start();
            var tcpInClient = tcpIn.AcceptTcpClient();

            _rx = new StreamReader(tcpInClient.GetStream(), Encoding.UTF8);
            while (true) {
                var line = _rx.ReadLine();
                Console.WriteLine(line);

                var packet = JsonConvert.DeserializeObject<HarmonyPacket>(line);
                if (packet == null) continue;

                switch (packet.Type) {
                    case HarmonyPacket.PacketType.MousePacket:
                        var mp = JsonConvert.DeserializeObject<HarmonyPacket.MousePacket>(packet.PacketStr);

                        var input = new MouseHook.MouseInput()
                        {
                            DwType = 1, Mstruct = new MouseHook.Msllhookstruct()
                            {
                                pt = new Point(mp.PosX, mp.PosY), time = mp.Time, flags = mp.Flags, mouseData = mp.MouseData, dwExtraInfo = mp.DwExtraInfo
                            }
                        };

                        if (mp.Action == 0x0200) {
                            SetCursorPos(mp.PosX, mp.PosY); //Needs Elevation!!
                            //mouse_event(0x8000, mp.PosX, mp.PosY, (int)mp.MouseData, mp.DwExtraInfo);
                        } else {
                            //mouse_event(mp.Action, 0, 0, (int)mp.MouseData, mp.DwExtraInfo);
                        }
                        //SendInput(0, input, Marshal.SizeOf(input));
                        break;
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint cInputs, MouseHook.MouseInput input, int size); //Does not work (currently)

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo); //Does not work (currently)

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

    }
}