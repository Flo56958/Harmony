using System.Drawing;
using System.Windows.Forms;

namespace Harmony {
    class DisplayManager {

        private static Rectangle[] resolutions;

        public static void SetUp() {
            var screens = Screen.AllScreens;
            resolutions = new Rectangle[screens.Length];

            for(var i = 0; i < screens.Length; i++) {
                resolutions[i] = screens[i].Bounds;
                MainWindow.debug.Text += $"\nScreen {i}: X:{screens[i].Bounds.Size.Width}, Y:{screens[i].Bounds.Size.Height}, Pos: {resolutions[i].Location}";
            }
        }
    }
}
