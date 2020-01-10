using System;
using System.Windows;
using System.Windows.Controls;

namespace Harmony
{

    public partial class MainWindow : Window {
        private bool started = false;

        private static MainWindow window;

        public static string Password;

        public MainWindow()
        {
            InitializeComponent();
            window = this;
            DisplayManager.SetUp();
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            if(!started) { 
                var ip = IPInput.Text;
                var port = PortInput.Text;

                if (!Int32.TryParse(port, out var iport)) return;
                bool isMaster = MasterCheckBox.IsChecked != null && (bool)MasterCheckBox.IsChecked;
                Password = PasswordInput.Text;

                var networkCommunicator = new NetworkCommunicator(ip, iport, isMaster);
                if (NetworkCommunicator.Instance == null) return;
                if (isMaster) {
                    MouseHook.Start();
                    KeyboardHook.Start();
                }

                StartButton.Content = "Stop";

                started = true;
            }
            else {
                if (NetworkCommunicator.Instance != null) {
                    NetworkCommunicator.Instance.Close();
                    NetworkCommunicator.Instance = null;
                }
                
                started = false;
                StartButton.Content = "Start";
            }
        }

        public static void Log(string message, bool error) {
            var err = error ? "[ERROR] " : "[INFO] "; 
            window.Dispatcher.Invoke(() => {
                window.Debug.AppendText("\n" + err + message);
            });
        }

      
    }
}
