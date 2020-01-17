using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace Harmony {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        public App() {
            nIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            nIcon.Visible = true;
            nIcon.MouseDown += (s, e) => {
                if (e.Button != MouseButtons.Left) return;
                MainWindow.Show();
                MainWindow.Visibility = Visibility.Visible;
                MainWindow.WindowState = WindowState.Normal;
            };
        }
    }
}
