using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;

namespace Harmony {
    public class NetworkCommunicator {
        public static NetworkCommunicator instance;
        private readonly StreamWriter sender;
        private readonly BlockingCollection<MousePackage> blockingCollection;
        private readonly Thread packer;

        public NetworkCommunicator(string address, int port) {
            instance = this;
            var client = new TcpClient(address, port);

            sender = new StreamWriter(client.GetStream()) { AutoFlush = true };

            blockingCollection = new BlockingCollection<MousePackage>();

            packer = new Thread(new ThreadStart(PackAndSend));
            packer.Start();
        }

        public void SendAsync(MousePackage o) {
            blockingCollection.Add(o);
        }

        public void close() {
            packer.Abort();
            sender.Close();
        }

        private void PackAndSend() {
            while (true) {
                var mp = blockingCollection.Take();
                sender.WriteLine(JsonConvert.SerializeObject(mp));
            }
        }
    }

    public struct MousePackage {
        public int PosX;
        public int PosY;
        public MouseActionType Action;
        public uint MouseData;
    }

    public enum MouseActionType {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEWHEEL = 0x020A,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205
    }
}