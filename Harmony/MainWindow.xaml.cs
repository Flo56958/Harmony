using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Harmony.Windows;
using MahApps.Metro.Controls;

namespace Harmony
{
    public partial class MainWindow : MetroWindow {
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

                DisplayManager.SetUp(); //Reload DisplayManager

                _started = false;
                StartButton.Content = "Start";
            }
        }

        public static void Log(string message, bool error) {
            var err = error ? "[ERROR] " : "[INFO] "; 
            _window.Dispatcher?.Invoke(() => {
                _window.DebugTextBox.AppendText(err + message + "\n");
            });
        }

        private void OnClickSave(object sender, RoutedEventArgs e) {

        }

        private void OnClickUpdate(object sender, RoutedEventArgs e) {
            var shrink = 20;
            DisplayCanvas.Children.Clear();
            var width = DisplayCanvas.Width / 2;
            var height = DisplayCanvas.Height / 2;

            var displays = DisplayManager.Displays;

            foreach (DisplayManager.Display dis in displays) {
                Rectangle recti = new Rectangle {
                    Width = dis.Screen.Width / shrink,
                    Height = dis.Screen.Height / shrink,

                    StrokeThickness = 2,

                    Stroke = Brushes.Black,
                    Fill = dis.OwnDisplay ? Brushes.CadetBlue : Brushes.Red
                };
                Canvas.SetLeft(recti, dis.Location.X / shrink);// + width);
                Canvas.SetTop(recti, dis.Location.Y / shrink);// + height);
                var i = DisplayCanvas.Children.Add(recti);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            MouseHook.Stop();
            KeyboardHook.Stop();
            NetworkCommunicator.Instance?.Close();
        }

        private void DisplayCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Debug.WriteLine(e.GetPosition(DisplayCanvas).ToString());
        }
    }
}
