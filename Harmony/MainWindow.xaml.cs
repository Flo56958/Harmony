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
            var shrink = 5; //(int) (1 / (Math.Min(1920 / this.ActualWidth, 1080 / this.ActualHeight)) * 3);
            DisplayCanvas.Children.Clear();

            var displays = DisplayManager.Displays;

            foreach (var dis in displays) {
                var recti = new Rectangle {
                    Width = dis.Screen.Width / shrink,
                    Height = dis.Screen.Height / shrink,

                    StrokeThickness = 1,

                    Stroke = Brushes.Black,
                    Fill = dis.OwnDisplay ? Brushes.CornflowerBlue : Brushes.IndianRed
                };

                Canvas.SetLeft(recti, dis.Location.X / shrink);
                Canvas.SetTop(recti, dis.Location.Y / shrink);
                DisplayCanvas.Children.Add(recti);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            MouseHook.Stop();
            KeyboardHook.Stop();
            NetworkCommunicator.Instance?.Close();
        }

        private bool _canvasClicked = false;
        private void DisplayCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Debug.WriteLine(e.GetPosition(DisplayCanvas).ToString());
        }
    }
}
