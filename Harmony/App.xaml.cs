using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace Harmony {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        NotifyIcon nIcon = new NotifyIcon();
        public App() {
            nIcon.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            nIcon.Visible = true;
            nIcon.MouseDown += (s, e) => {
                if (e.Button != MouseButtons.Left) return;
                if (MainWindow == null) return;
                MainWindow.Show();
                MainWindow.Visibility = Visibility.Visible;
                MainWindow.WindowState = WindowState.Normal;
            };

            nIcon.ContextMenuStrip = new ContextMenuStrip();

            var button = new ToolStripButton("Close", null, onCloseButtonClick);
            nIcon.ContextMenuStrip.Items.Add(button);
        }

        private void onCloseButtonClick(object? sender, EventArgs e)
        {
            nIcon.Dispose();
            Environment.Exit(0);
        }

        protected override void OnExit(ExitEventArgs e) {
            nIcon.Dispose();
            base.OnExit(e);
        }
    }
}
