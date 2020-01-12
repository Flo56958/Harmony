using System;
using System.Windows;
using Harmony.Windows;

namespace Harmony
{
    public partial class MainWindow : Window {
        private bool _started = false;

        private static MainWindow _window;

        public static string Password;

        public MainWindow()
        {
            InitializeComponent();
            _window = this;
            DisplayManager.SetUp();
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            if(!_started) { 
                var ip = IPInput.Text;
                var port = PortInput.Text;

                if (!int.TryParse(port, out var iport)) return;
                var isMaster = MasterCheckBox.IsChecked != null && (bool)MasterCheckBox.IsChecked;
                Password = PasswordInput.Text;

                var networkCommunicator = new NetworkCommunicator(ip, iport, isMaster);
                if (NetworkCommunicator.Instance == null) return;
                if (isMaster) {
                    MouseHook.Start();
                    KeyboardHook.Start();
                }

                StartButton.Content = "Stop";

                _started = true;
            }
            else {
                if (NetworkCommunicator.Instance != null) {
                    NetworkCommunicator.Instance.Close();
                    NetworkCommunicator.Instance = null;
                }
                
                _started = false;
                StartButton.Content = "Start";
            }
        }

        public static void Log(string message, bool error) {
            var err = error ? "[ERROR] " : "[INFO] "; 
            _window.Dispatcher?.Invoke(() => {
                _window.Debug.AppendText("\n" + err + message);
            });
        }
    }
}
