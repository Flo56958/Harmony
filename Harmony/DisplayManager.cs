using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Harmony {
    public class DisplayManager {

        public static List<Display> displays { get; set; }

        public static Display slaveMain { get; set; }

        //SetUp-Function for Slave and Master
        public static void SetUp() {
            var screens = Screen.AllScreens;
            displays = new List<Display>();

            foreach (var scr in screens) {
                displays.Add(new Display()
                {
                    Screen = scr.Bounds, 
                    OwnDisplay = true,
                    Location = scr.Bounds.Location
                });
            }

            displays.Sort((d1, d2) => d1.Location.X - d2.Location.X);
            PrintScreenConfiguration();
        }

        //Additional SetUp-Function for Slave
        public static void SetUp(List<Display> displ) {
            displays = new List<Display>();
            foreach (var d in displ) {
                d.OwnDisplay = !d.OwnDisplay;
                if (d.OwnDisplay && d.Screen.Location.X == 0 && d.Screen.Location.Y == 0) slaveMain = d;
                displays.Add(d);
            }
        }

        public static void AddRight(Display display) {
            var left = displays[displays.Count - 1];
            display.Location = new Point(left.Location.X + left.Screen.Width, left.Location.Y);
            displays.Add(display);
        }

        public static bool IsPointInHarmonySpace(int x, int y) {
            return GetDisplayFromPoint(x, y) != null;
        }

        public static Display GetDisplayFromPoint(int x, int y) {
            return displays.FirstOrDefault(dis => x >= dis.Location.X && x < dis.Location.X + dis.Screen.Width && y >= dis.Location.Y && y < dis.Location.Y + dis.Screen.Height);
        }

        public static void PrintScreenConfiguration() {
            var count = 0;
            MainWindow.Log("Current Screen-Configuration:", false);
            foreach (var dis in displays) {
                var b = (dis.OwnDisplay) ? "own" : "foreign";
                MainWindow.Log($"Screen {count++},{b}: X:{dis.Screen.Size.Width}, Y:{dis.Screen.Size.Height}, Pos: {dis.Location}", false);
            }
        }

        public class Display {
            public Rectangle Screen { get; set; }
            public bool OwnDisplay { get; set; }
            public Point Location { get; set; } //Location in Harmony-Space
        }
    }
}
