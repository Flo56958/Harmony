using System.Security;
using System.Windows;
using Harmony.Windows;

namespace Harmony
{
    public partial class MainWindow : Window {
        private bool _started = false;

        private static MainWindow _window;

        public static SecureString Password;

        public MainWindow()
        {
            InitializeComponent();
            _window = this;
            Log($"The IP-Address of this machine is { NetworkCommunicator.GetLocalIPAddress() }", false);
            DisplayManager.SetUp();
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            var isMaster = MasterCheckBox.IsChecked != null && (bool)MasterCheckBox.IsChecked;
            if (!_started) { 
                var ip = IPInput.Text;
                var port = PortInput.Text;

                if (!int.TryParse(port, out var iport)) return;
                Password = PasswordInput.SecurePassword;

                new NetworkCommunicator(ip, iport, isMaster);
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

                if (isMaster) {
                    MouseHook.Stop();
                    KeyboardHook.Stop();
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
