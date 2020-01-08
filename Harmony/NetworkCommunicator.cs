using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Harmony {

    public class NetworkCommunicator {
        private StreamWriter sender;
        
        public NetworkCommunicator(String address, int port) {
            var client = new TcpClient(address, port);

            sender = new StreamWriter(client.GetStream());
        }

        public void SendAsync(Object o) {
            sender.WriteLine(JsonConvert.SerializeObject(o));
        }
    }
}
