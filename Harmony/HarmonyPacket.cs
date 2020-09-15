using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Harmony {
    public class HarmonyPacket {

        public PacketType Type { get; set; }
        public dynamic Pack { get; set; }

        public enum PacketType : byte {
            MouseMovePacket = 0,
            MousePacket = 1,
            KeyBoardPacket = 2,
            DisplayPacket = 3,
            SaltPacket = 4,
            MediaControl = 5
        }

        public class MouseMovePacket {
            public int PosX { get; set; }
            public int PosY { get; set; }
        }

        public class MousePacket {
            public uint Action { get; set; }
            public uint MouseData { get; set; }
            public uint Flags { get; set; }
        }

        public class DisplayPacket {
            public List<DisplayManager.Display> screens { get; set; }
        }

        public class KeyboardPacket {
            public Keys Key { get; set; }
            public int wParam { get; set; }
        }

        public class MediaControlPacket {
            public enum MediaAction : byte {
                PlayPause = 0,
                Stop = 1,
                SkipPrevious = 2,
                SkipForward = 3,
                Reload = 4
            }

            public MediaAction Action { get; set; }
        }

        public class MediaDataPacket
        {
            public int TrackNumber { get; set; }
            public string Artist { get; set; }
            public string Title { get; set; }
            public string Album { get; set; }
            public BitmapImage Thumbnail { get; set; }
        }

        internal static byte[] Encode(HarmonyPacket hp) {
            var bytes = new List<byte>();
            var type = new [] { (byte) hp.Type };
            var encrypt = true;
            switch (hp.Type) {
                case PacketType.MouseMovePacket:
                    var mmp = (MouseMovePacket) hp.Pack;
                    var PosX = BitConverter.GetBytes(mmp.PosX);
                    var PosY = BitConverter.GetBytes(mmp.PosY);
                    bytes.AddRange(PosX);
                    bytes.AddRange(PosY);
                    break;
                case PacketType.MousePacket:
                    var mp = (MousePacket) hp.Pack;
                    var Action = BitConverter.GetBytes(mp.Action);
                    var MouseData = BitConverter.GetBytes(mp.MouseData);
                    var Flags = BitConverter.GetBytes(mp.Flags);
                    bytes.AddRange(Action);
                    bytes.AddRange(MouseData);
                    bytes.AddRange(Flags);
                    break;
                case PacketType.KeyBoardPacket:
                    var kp = (KeyboardPacket)hp.Pack;
                    var Key = BitConverter.GetBytes((int) kp.Key);
                    var wParam = BitConverter.GetBytes(kp.wParam);
                    bytes.AddRange(Key);
                    bytes.AddRange(wParam);
                    break;
                case PacketType.DisplayPacket:
                    var dp = (DisplayPacket) hp.Pack;
                    var screens = JsonConvert.SerializeObject(dp.screens);
                    foreach (var c in screens) {
                        bytes.AddRange(BitConverter.GetBytes(c));
                    }
                    break;
                case PacketType.SaltPacket:
                    bytes.AddRange((byte[]) hp.Pack);
                    encrypt = false;
                    break;
                case PacketType.MediaControl:
                    bytes.Add((byte) ((MediaControlPacket) hp.Pack).Action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var arr = bytes.ToArray();
            if (encrypt) {
                arr = Crypto.Encrypt(arr);
            }
            var length = BitConverter.GetBytes(arr.Length + 1);

            return length.Concat(type).Concat(arr).ToArray();
        }

        private static dynamic Decode(byte[] input, out PacketType type) {
            type = (PacketType) input[0];
            if (type != PacketType.SaltPacket)
                input = Crypto.Decrypt(input.Skip(1).ToArray());
            switch (type) {
                case PacketType.MouseMovePacket:
                    return new MouseMovePacket()
                    {
                        PosX = BitConverter.ToInt32(input, 0),
                        PosY = BitConverter.ToInt32(input, 4)
                    };
                case PacketType.MousePacket:
                    return new MousePacket()
                    {
                        Action = BitConverter.ToUInt32(input, 0),
                        MouseData = BitConverter.ToUInt32(input, 4),
                        Flags = BitConverter.ToUInt32(input, 8)
                    };
                case PacketType.KeyBoardPacket:
                    return new KeyboardPacket()
                    {
                        Key = (Keys) BitConverter.ToInt32(input, 0),
                        wParam = BitConverter.ToInt32(input, 4)
                    };
                case PacketType.DisplayPacket:
                    var sb = new StringBuilder();
                    for (var i = 0; i < input.Length; i += 2) {
                        sb.Append(BitConverter.ToChar(input, i));
                    }
                    return new DisplayPacket()
                    {
                        screens = JsonConvert.DeserializeObject<List<DisplayManager.Display>>(sb.ToString())
                    };
                case PacketType.SaltPacket:
                    return input.Skip(1).ToArray();
                case PacketType.MediaControl:
                    return new MediaControlPacket()
                    {
                        Action = (MediaControlPacket.MediaAction) input[0]
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static HarmonyPacket ReadPacket(NetworkStream stream) {
            var length = new byte[4];
            stream.Read(length, 0, 4);

            var len = BitConverter.ToInt32(length, 0);
            var packet = new byte[len];
            stream.Read(packet, 0, len);

            var pack = Decode(packet, out var type);
            return new HarmonyPacket()
            {
                Type = type,
                Pack = pack
            };
        }
    }
}
