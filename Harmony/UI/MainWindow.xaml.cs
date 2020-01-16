using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
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

        public MainWindow() {
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
            var shrink = 10.0;
            var main = DisplayManager.GetDisplayFromPoint(0, 0);
            foreach (var obj in DisplayCanvas.Children) {
                if (!(obj is Canvas)) continue;
                var c = (Canvas)obj;
                if (!c.Background.Equals(Brushes.IndianRed)) continue;
                var i = int.Parse(((TextBlock) c.Children[0]).Text);
                DisplayManager.Displays[i].Location = new System.Drawing.Point {
                    X = (int) ((Canvas.GetLeft(c) + (main.Screen.Width / shrink) / 2) * shrink),
                    Y = (int) ((Canvas.GetTop(c) + (main.Screen.Height / shrink) / 2) * shrink)
                };
            }
            NetworkCommunicator.Instance?.SendAsync(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.Displays
                }
            });
        }

        private void OnClickUpdate(object sender, RoutedEventArgs e) {
            _updateDisplayCanvas();
        }

        public static void updateDisplayCanvas() {
            _window.Dispatcher?.InvokeAsync(_window._updateDisplayCanvas);
        }

        private void _updateDisplayCanvas() {
            var shrink = 10.0;
            DisplayCanvas.Children.Clear();

            var displays = DisplayManager.Displays;

            var main = DisplayManager.GetDisplayFromPoint(0, 0);

            foreach (var dis in displays) {
                var canv = new Canvas() {
                    Width = dis.Screen.Width / shrink,
                    Height = dis.Screen.Height / shrink,
                    MaxWidth = dis.Screen.Width / shrink,
                    MaxHeight = dis.Screen.Height / shrink,
                    Background = dis.OwnDisplay ? Brushes.CornflowerBlue : Brushes.IndianRed,
                    Opacity = 100
                };

                Debug.WriteLine($"{canv.Width} + {canv.Height}");

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

                Canvas.SetLeft(canv, dis.Location.X / shrink - (main.Screen.Width / shrink) / 2);
                Canvas.SetTop(canv, dis.Location.Y / shrink - (main.Screen.Height / shrink) / 2);

                if (isMaster && !dis.OwnDisplay) {
                    canv.MouseLeftButtonDown += (s, eArgs) => {
                        var c = ((Canvas)s);
                        c.Opacity = 20;
                        var deltaLeft = Canvas.GetLeft(c);
                        var deltaTop = Canvas.GetTop(c);
                        Canvas.SetLeft(c, eArgs.GetPosition(DisplayCanvas).X - c.Width / 2);
                        Canvas.SetTop(c, eArgs.GetPosition(DisplayCanvas).Y - c.Height / 2);
                        deltaLeft = Canvas.GetLeft(c) - deltaLeft;
                        deltaTop = Canvas.GetTop(c) - deltaTop;

                        foreach (var can in DisplayCanvas.Children) {
                            if (can.Equals(c)) continue;
                            if (!(can is Canvas)) continue;
                            var canvas = (Canvas)can;
                            if (!canvas.Background.Equals(canv.Background)) continue;
                            Canvas.SetLeft(canvas, Canvas.GetLeft(canvas) + deltaLeft);
                            Canvas.SetTop(canvas, Canvas.GetTop(canvas) + deltaTop);
                        }
                    };
                    canv.MouseLeftButtonUp += (s, eArgs) => {
                        ((Canvas)s).Opacity = 100;
                        //TODO: Snap to other Screens
                    };
                    canv.MouseMove += (s, eArgs) => {
                        var c = ((Canvas)s);
                        if (eArgs.LeftButton != System.Windows.Input.MouseButtonState.Pressed ||
                            !(Math.Abs(c.Opacity - 20) < 0.1)) return;
                        var deltaLeft = Canvas.GetLeft(c);
                        var deltaTop = Canvas.GetTop(c);
                        Canvas.SetLeft(c, eArgs.GetPosition(DisplayCanvas).X - c.Width / 2);
                        Canvas.SetTop(c, eArgs.GetPosition(DisplayCanvas).Y - c.Height / 2);
                        deltaLeft = Canvas.GetLeft(c) - deltaLeft;
                        deltaTop = Canvas.GetTop(c) - deltaTop;

                        foreach (var can in DisplayCanvas.Children) {
                            if (can.Equals(c)) continue;
                            if (!(can is Canvas)) continue;
                            var canvas = (Canvas)can;
                            if (!canvas.Background.Equals(canv.Background)) continue;
                            Canvas.SetLeft(canvas, Canvas.GetLeft(canvas) + deltaLeft);
                            Canvas.SetTop(canvas, Canvas.GetTop(canvas) + deltaTop);
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
