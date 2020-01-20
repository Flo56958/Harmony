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

        private static MainWindow _window;

        public static HarmonyViewModel Model { get; private set; }

        public static SecureString Password;

        public MainWindow() {
            InitializeComponent();
            _window = this;
            Log($"The IP-Address of this machine is { NetworkCommunicator.GetLocalIPAddress() }", false);
            DisplayManager.SetUp();
            Model = (HarmonyViewModel)base.DataContext;
            VersionLabel.Content = "Harmony-Version: " + typeof(MainWindow).Assembly.GetName().Version;
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            if (Model.NotStarted) {
                Password = PasswordInput.SecurePassword;

                new NetworkCommunicator();
                if (NetworkCommunicator.Instance == null) return;
                if (Model.IsMaster) {
                    MouseHook.Start();
                    KeyboardHook.Start();
                }

                Model.NotStarted = false;
            } else {
                if (NetworkCommunicator.Instance != null) {
                    NetworkCommunicator.Instance.Close();
                    NetworkCommunicator.Instance = null;
                }

                if (Model.IsMaster) {
                    MouseHook.Stop();
                    KeyboardHook.Stop();
                }

                DisplayManager.SetUp(); //Reload DisplayManager

                Model.NotStarted = true;
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
                if (!c.Background.Equals(ThemeManager.GetResourceFromAppStyle(this, "MahApps.Brushes.Accent"))) continue;
                var i = int.Parse(((TextBlock)c.Children[0]).Text);
                DisplayManager.Displays[i].Location = new System.Drawing.Point {
                    X = (int)((Canvas.GetLeft(c) + (main.Screen.Width / shrink) / 2) * shrink),
                    Y = (int)((Canvas.GetTop(c) + (main.Screen.Height / shrink) / 2) * shrink)
                };
            }
            NetworkCommunicator.Instance?.SendAsync(new HarmonyPacket {
                Type = HarmonyPacket.PacketType.DisplayPacket,
                Pack = new HarmonyPacket.DisplayPacket {
                    screens = DisplayManager.Displays
                }
            });
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

                if (Model.IsMaster && !dis.OwnDisplay) {
                    canv.MouseLeftButtonDown += (s, eArgs) => {
                        MoveAllScreens((Canvas)s, eArgs.GetPosition(DisplayCanvas));

                    };
                    canv.MouseLeftButtonUp += (s, eArgs) => {
                        var c = (Canvas)s;
                        //TODO: Snap to other Screens
                        var changed = false;
                        do {
                            changed = false;
                            foreach (var obj in DisplayCanvas.Children) {
                                if (!(obj is Canvas)) continue;
                                if (c == obj) continue;
                                var o = (Canvas)obj;
                                if (o.Background.Equals(c.Background)) continue;
                                if (!Intersects(c, o)) continue;
                                changed = true;

                                var lt = new Point(Canvas.GetLeft(c), Canvas.GetTop(c));
                                var lb = new Point(Canvas.GetLeft(c), Canvas.GetTop(c) + c.Height);
                                var rt = new Point(Canvas.GetLeft(c) + c.Width, Canvas.GetTop(c));
                                var rb = new Point(Canvas.GetLeft(c) + c.Width, Canvas.GetTop(c) + c.Height);

                                double dx = 0;
                                double dy = 0;
                                if (IsPointInCanvas(lt, o)) {
                                    var ddx = Canvas.GetLeft(o) - lt.X;
                                    var ddy = Canvas.GetTop(o) - lt.Y;
                                    dx = Math.Max(dx, Math.Abs(ddx));
                                    if (dx == Math.Abs(ddx)) {
                                        dx = ddx;
                                    }
                                    dy = Math.Max(dy, Math.Abs(ddy));
                                    if (dy == Math.Abs(ddy)) {
                                        dy = ddy;
                                    }
                                }

                                if (IsPointInCanvas(lb, o)) {
                                    var ddx = Canvas.GetLeft(o) - lb.X;
                                    var ddy = Canvas.GetTop(o) + o.Height - lb.Y;
                                    dx = Math.Max(dx, Math.Abs(ddx));
                                    if (dx == Math.Abs(ddx)) {
                                        dx = ddx;
                                    }
                                    dy = Math.Max(dy, Math.Abs(ddy));
                                    if (dy == Math.Abs(ddy)) {
                                        dy = ddy;
                                    }
                                }

                                if (IsPointInCanvas(rt, o)) {
                                    var ddx = Canvas.GetLeft(o) + o.Width - rt.X;
                                    var ddy = Canvas.GetTop(o) - rt.Y;
                                    dx = Math.Max(dx, Math.Abs(ddx));
                                    if (dx == Math.Abs(ddx)) {
                                        dx = ddx;
                                    }
                                    dy = Math.Max(dy, Math.Abs(ddy));
                                    if (dy == Math.Abs(ddy)) {
                                        dy = ddy;
                                    }
                                }

                                if (IsPointInCanvas(rb, o)) {
                                    var ddx = Canvas.GetLeft(o) + o.Width - rb.X;
                                    var ddy = Canvas.GetTop(o) + o.Height - rb.Y;
                                    dx = Math.Max(dx, Math.Abs(ddx));
                                    if (dx == Math.Abs(ddx)) {
                                        dx = ddx;
                                    }
                                    dy = Math.Max(dy, Math.Abs(ddy));
                                    if (dy == Math.Abs(ddy)) {
                                        dy = ddy;
                                    }
                                }

                                dx -= Canvas.GetLeft(o);
                                dy -= Canvas.GetTop(o);

                                if (Math.Abs(dx) >= Math.Abs(dy)) {
                                    Canvas.SetLeft(c, Canvas.GetLeft(c) - dx);
                                }
                                else {
                                    Canvas.SetTop(c, Canvas.GetTop(c) - dy);
                                }

                                MoveAllScreens(c, new Point(Canvas.GetLeft(c), Canvas.GetTop(c)));
                            }
                        } while (changed);
                    };
                    canv.MouseMove += (s, eArgs) => {
                        var c = (Canvas)s;
                        if (eArgs.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
                        MoveAllScreens(c, eArgs.GetPosition(DisplayCanvas));
                    };
                }
                DisplayCanvas.Children.Add(canv);
            }
        }

        private static bool IsPointInCanvas(Point p, Canvas c) {
            return p.X > Canvas.GetLeft(c) && p.X < Canvas.GetLeft(c) + c.Width
                                           && p.Y > Canvas.GetTop(c) && p.Y < Canvas.GetTop(c) + c.Height;
        }

        private static bool Intersects(Canvas c1, Canvas c2) {
            var lt = new Point(Canvas.GetLeft(c1), Canvas.GetTop(c1));
            var lb = new Point(Canvas.GetLeft(c1), Canvas.GetTop(c1) + c1.Height);
            var rt = new Point(Canvas.GetLeft(c1) + c1.Width, Canvas.GetTop(c1));
            var rb = new Point(Canvas.GetLeft(c1) + c1.Width, Canvas.GetTop(c1) + c1.Height);
            return IsPointInCanvas(lt, c2) || IsPointInCanvas(lb, c2) || IsPointInCanvas(rt, c2) ||
                   IsPointInCanvas(rb, c2);
            return Canvas.GetLeft(c1) < Canvas.GetLeft(c2) + c2.Width
                   && Canvas.GetLeft(c1) + c1.Width > Canvas.GetLeft(c2)
                   && Canvas.GetTop(c1) > Canvas.GetTop(c2) + c2.Height
                   && Canvas.GetTop(c1) + c1.Height < Canvas.GetTop(c2);
        }

        private void MoveAllScreens(Canvas c, Point p) {
            var deltaLeft = Canvas.GetLeft(c);
            var deltaTop = Canvas.GetTop(c);
            Canvas.SetLeft(c, p.X - c.Width / 2);
            Canvas.SetTop(c, p.Y - c.Height / 2);
            deltaLeft = Canvas.GetLeft(c) - deltaLeft;
            deltaTop = Canvas.GetTop(c) - deltaTop;

            foreach (var can in DisplayCanvas.Children) {
                if (can.Equals(c)) continue;
                if (!(can is Canvas)) continue;
                var canvas = (Canvas)can;
                if (!canvas.Background.Equals(c.Background)) continue;
                Canvas.SetLeft(canvas, Canvas.GetLeft(canvas) + deltaLeft);
                Canvas.SetTop(canvas, Canvas.GetTop(canvas) + deltaTop);
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
    }
}
