using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Harmony.UI;
using Harmony.Windows;
using MahApps.Metro.Controls;

namespace Harmony {
    public partial class MainWindow : MetroWindow {
        private bool _started = false;

        private static MainWindow _window;

        private static HarmonyViewModel _model;

        public static SecureString Password;

        private bool isMaster;

        public MainWindow()
        {
            InitializeComponent();
            _window = this;
            Log($"The IP-Address of this machine is { NetworkCommunicator.GetLocalIPAddress() }", false);
            DisplayManager.SetUp();
            _model = (HarmonyViewModel)base.DataContext;
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            isMaster = MasterCheckBox.IsChecked != null && (bool)MasterCheckBox.IsChecked;
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
            var shrink = 10;
            foreach(var obj in DisplayCanvas.Children) {
                if(typeof(Canvas).IsInstanceOfType(obj)) {
                    var c = (Canvas)obj;
                    if (c.Background.Equals(Brushes.IndianRed)) {
                        int i = int.Parse(((TextBlock) c.Children[0]).Text);
                        DisplayManager.Displays[i].Location = new System.Drawing.Point {
                            X = (int) (Canvas.GetLeft(c) - c.Width / 2) * shrink,
                            Y = (int)(Canvas.GetTop(c) - c.Height / 2) * shrink
                        };
                    }
                }
            }
            NetworkCommunicator.Instance?.SendAsync(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.Displays
                }
            });
        }

        private void OnClickUpdate(object sender, RoutedEventArgs e) {
            var shrink = 10; //(int) (1 / (Math.Min(1920 / this.ActualWidth, 1080 / this.ActualHeight)) * 3);
            DisplayCanvas.Children.Clear();

            var displays = DisplayManager.Displays;

            foreach (var dis in displays) {
                var canv = new Canvas() {
                    Width = dis.Screen.Width / shrink,
                    Height = dis.Screen.Height / shrink,
                    Background = dis.OwnDisplay ? Brushes.CornflowerBlue : Brushes.IndianRed,
                    Opacity = 100
                };

                var disNo = new TextBlock() {
                    Width = canv.Width,
                    Height = 25,
                    Text = displays.IndexOf(dis).ToString(),
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center
                };

                var disSize = new TextBlock() {
                    Width = canv.Width,
                    Height = 25,
                    Text = $"{dis.Screen.Width} x {dis.Screen.Height}",
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(disNo, canv.Width / 2 - disNo.Width / 2);
                Canvas.SetTop(disNo, canv.Height / 2 - 10 - disNo.Height / 2);
                Canvas.SetLeft(disSize, canv.Width / 2 - disSize.Width / 2);
                Canvas.SetTop(disSize, canv.Height / 2 + 10 - disSize.Height / 2);

                canv.Children.Add(disNo);
                canv.Children.Add(disSize);

                Canvas.SetLeft(canv, dis.Location.X / shrink - canv.Width / 2);
                Canvas.SetTop(canv, dis.Location.Y / shrink - canv.Height / 2);

                if (isMaster && !dis.OwnDisplay) {
                    canv.MouseLeftButtonDown += (s, eArgs) => {
                        var c = ((Canvas)s);
                        c.Opacity = 20;
                        Canvas.SetLeft(c, eArgs.GetPosition(DisplayCanvas).X - c.Width / 2);
                        Canvas.SetTop(c, eArgs.GetPosition(DisplayCanvas).Y - c.Height / 2);
                    };
                    canv.MouseLeftButtonUp += (s, eArgs) => {
                        ((Canvas)s).Opacity = 100;
                        //TODO: Snap to other Screens
                    };
                    canv.MouseMove += (s, eArgs) => {
                        var c = ((Canvas)s);
                        if (eArgs.LeftButton == System.Windows.Input.MouseButtonState.Pressed && c.Opacity == 20) {
                            Canvas.SetLeft(c, eArgs.GetPosition(DisplayCanvas).X - c.Width / 2);
                            Canvas.SetTop(c, eArgs.GetPosition(DisplayCanvas).Y - c.Height / 2);
                        }
                    };
                }
                DisplayCanvas.Children.Add(canv);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            MouseHook.Stop();
            KeyboardHook.Stop();
            NetworkCommunicator.Instance?.Close();
        }
    }
}
