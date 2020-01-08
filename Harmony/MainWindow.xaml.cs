using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Harmony
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly StreamWriter sw;
        public MainWindow()
        {
            InitializeComponent();
            var t = new Timer();
            t.Interval = 1000;

            t.Elapsed += T_Elapsed;

            var client = new TcpClient("localhost", 55555);

            sw = new StreamWriter(client.GetStream());

            t.Start();

            MouseHook.Start();
            MouseHook.MouseAction += new EventHandler(MouseClickEvent);
        }

        private void MouseClickEvent(object sender, EventArgs e) {
            sw.WriteLine(JsonConvert.SerializeObject(e));
            sw.Flush();
        }


        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            POINT P;
            GetCursorPos(out P);
            sw.WriteLine(JsonConvert.SerializeObject(P));
            sw.Flush();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);
    }
}
