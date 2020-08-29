using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Harmony.UI;
using Harmony.Windows;
using MahApps.Metro;

namespace Harmony {
    public partial class MainWindow {

        public static MainWindow Window { get; private set; }

        public static HarmonyViewModel Model { get; private set; }

        public static SecureString Password;

        public MainWindow() {
            InitializeComponent();
            Window = this;
            Log($"The IP-Address of this machine is { NetworkCommunicator.GetLocalIPAddress() }", false);
            DisplayManager.SetUp();
            Model = (HarmonyViewModel)base.DataContext;
            VersionLabel.Content = "Harmony-Version: " + typeof(MainWindow).Assembly.GetName().Version;
            MediaControl.Reload();
            MediaControl.UpdateMediaProperties();
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            if (Model.NotStarted) {
                Password = PasswordInput.SecurePassword;

                NetworkCommunicator.Init();
                if (Model.IsServer) {
                    MouseHook.Start();
                    KeyboardHook.Start();
                }

                Model.NotStarted = false;
            }
            else {
                NetworkCommunicator.Close();

                if (Model.IsServer) {
                    MouseHook.Stop();
                    KeyboardHook.Stop();
                }

                DisplayManager.SetUp(); //Reload DisplayManager

                Model.NotStarted = true;
            }
        }

        public static void Log(string message, bool error) {
            var err = error ? "[ERROR] " : "[INFO] ";
            Window.Dispatcher?.Invoke(() => {
                Window.DebugTextBox.AppendText(err + message + "\n");
            });
        }

        private void OnClickSave(object sender, RoutedEventArgs e) {
            const double shrink = 10.0;
            var main = DisplayManager.GetDisplayFromPoint(0, 0);
            foreach (var obj in DisplayCanvas.Children) {
                if (!(obj is Canvas)) continue;
                var c = (Canvas)obj;
                if (!c.Background.Equals(ThemeManager.GetResourceFromAppStyle(this, "MahApps.Brushes.Accent"))) continue;
                if (int.TryParse(((TextBlock)c.Children[0]).Text, out int i)) {
                    DisplayManager.Displays[i].Location = new System.Drawing.Point {
                        X = (int)((Canvas.GetLeft(c) + (main.Screen.Width / shrink) / 2) * shrink),
                        Y = (int)((Canvas.GetTop(c) + (main.Screen.Height / shrink) / 2) * shrink)
                    };
                }
            }
            NetworkCommunicator.SendAsync(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.Displays
                }
            });
        }

        public static void updateDisplayCanvas() {
            Window.Dispatcher?.InvokeAsync(Window._updateDisplayCanvas);
        }

        private void _updateDisplayCanvas() {
            if (!Model.IsServer) return;

            const double shrink = 10.0;
            DisplayCanvas.Children.Clear();

            var displays = DisplayManager.Displays;

            var main = DisplayManager.GetDisplayFromPoint(0, 0);

            foreach (var dis in displays) {
                var canv = new Canvas() {
                    Width = dis.Screen.Width / shrink,
                    Height = dis.Screen.Height / shrink,
                    MaxWidth = dis.Screen.Width / shrink,
                    MaxHeight = dis.Screen.Height / shrink,
                    Background = (Brush)(dis.OwnDisplay ? ThemeManager.GetResourceFromAppStyle(this, "MahApps.Brushes.Accent2") : ThemeManager.GetResourceFromAppStyle(this, "MahApps.Brushes.Accent")),
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

                Canvas.SetLeft(canv, dis.Location.X / shrink - (main.Screen.Width / shrink) / 2);
                Canvas.SetTop(canv, dis.Location.Y / shrink - (main.Screen.Height / shrink) / 2);

                if (Model.IsServer && !dis.OwnDisplay) {
                    canv.MouseLeftButtonDown += (s, eArgs) => {
                        MoveAllScreens((Canvas)s, eArgs.GetPosition(DisplayCanvas), false);
                    };
                    canv.MouseLeftButtonUp += (s, eArgs) => {
                        var c = (Canvas)s;
                        bool changed;
                        do {
                            changed = false;
                            foreach (var obj in DisplayCanvas.Children) {
                                if (!(obj is Canvas)) continue;
                                var o = (Canvas)obj;
                                if (o.Background.Equals(c.Background)) continue;

                                var xOverlap = Math.Round(Math.Max(0, Math.Min(Canvas.GetLeft(c) + c.Width, Canvas.GetLeft(o) + o.Width) - Math.Max(Canvas.GetLeft(c), Canvas.GetLeft(o))));
                                var yOverlap = Math.Round(Math.Max(0, Math.Min(Canvas.GetTop(c) + c.Height, Canvas.GetTop(o) + o.Height) - Math.Max(Canvas.GetTop(c), Canvas.GetTop(o))));

                                if (xOverlap == 0 || yOverlap == 0) continue;
                                changed = true;

                                MoveAllScreens(c,
                                    xOverlap < yOverlap ? new Point(-xOverlap, 0) : new Point(0, -yOverlap), true);
                                //break;
                            }
                            System.Windows.Forms.Application.DoEvents();
                        } while (changed);
                    };
                    canv.MouseMove += (s, eArgs) => {
                        var c = (Canvas)s;
                        if (eArgs.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
                        MoveAllScreens(c, Point.Add(eArgs.GetPosition(DisplayCanvas), new Vector(- c.Width / 2, - c.Height / 2)), false);
                    };
                }
                DisplayCanvas.Children.Add(canv);
            }
        }

        private void MoveAllScreens(Canvas c, Point p, bool delta) {
            var dx = Canvas.GetLeft(c) - p.X;
            var dy = Canvas.GetTop(c) - p.Y;
            foreach (var can in DisplayCanvas.Children) {
                if (!(can is Canvas)) continue;
                var canvas = (Canvas)can;
                if (!canvas.Background.Equals(c.Background)) continue;
                Canvas.SetLeft(canvas, (delta) ? Canvas.GetLeft(canvas) + p.X : (c == canvas) ? p.X : Canvas.GetLeft(canvas) - dx);
                Canvas.SetTop(canvas, (delta) ? Canvas.GetTop(canvas) + p.Y : (c == canvas) ? p.Y : Canvas.GetTop(canvas) - dy);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            MouseHook.Stop();
            KeyboardHook.Stop();
            NetworkCommunicator.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void TabItem_Selected(object sender, RoutedEventArgs e) {
            _updateDisplayCanvas();
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e) {
            switch (this.WindowState) {
                case WindowState.Maximized:
                    break;
                case WindowState.Minimized:
                    this.Hide();
                    break;
                case WindowState.Normal:

                    break;
            }
        }

        private void Media_Stop_Click(object sender, RoutedEventArgs e) {
            MediaControl.Stop();
        }

        private void Media_PlayPause_Click(object sender, RoutedEventArgs e) {
            MediaControl.PlayPause();
        }

        private void Media_SkipPrevious_Click(object sender, RoutedEventArgs e) {
            MediaControl.SkipPrevious();
        }

        private void Media_SkipForward_Click(object sender, RoutedEventArgs e) {
            MediaControl.SkipForward();
        }

        private void Media_Reload_OnClick(object sender, RoutedEventArgs e) {
            MediaControl.Reload();
        }
    }
}
