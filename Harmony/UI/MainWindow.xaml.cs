﻿using System;
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
            }
            else {
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
                if (int.TryParse(((TextBlock)c.Children[0]).Text, out int i)) {
                    DisplayManager.Displays[i].Location = new System.Drawing.Point {
                        X = (int)((Canvas.GetLeft(c) + (main.Screen.Width / shrink) / 2) * shrink),
                        Y = (int)((Canvas.GetTop(c) + (main.Screen.Height / shrink) / 2) * shrink)
                    };
                }
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
            if (!Model.IsMaster) return;

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
                        var changed = false;
                        do {
                            changed = false;
                            foreach (var obj in DisplayCanvas.Children) {
                                if (!(obj is Canvas)) continue;
                                if (c == obj) continue;
                                var o = (Canvas)obj;
                                if (o.Background.Equals(c.Background)) continue;
                                if (!Intersects(c, o, out IntersectsEnum direc)) continue;
                                changed = true;

                                switch (direc) {
                                    case IntersectsEnum.TOP:
                                        MoveAllScreens(c, new Point(Canvas.GetLeft(c), Canvas.GetTop(c) - 1));
                                        break;
                                    case IntersectsEnum.LEFT:
                                        MoveAllScreens(c, new Point(Canvas.GetLeft(c) - 1, Canvas.GetTop(c)));
                                        break;
                                    case IntersectsEnum.BOT:
                                        MoveAllScreens(c, new Point(Canvas.GetLeft(c), Canvas.GetTop(c) + 1));
                                        break;
                                    case IntersectsEnum.RIGHT:
                                        MoveAllScreens(c, new Point(Canvas.GetLeft(c) + 1, Canvas.GetTop(c)));
                                        break;
                                    case IntersectsEnum.NONE:
                                        break;
                                }
                            }
                            System.Windows.Forms.Application.DoEvents();
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

        private enum IntersectsEnum {
            TOP,
            LEFT,
            BOT,
            RIGHT,
            NONE
        }

        private static bool Intersects(Canvas c1, Canvas c2, out IntersectsEnum direction) {
            direction = IntersectsEnum.NONE;
            double w = 0.5 * (c1.Width + c2.Width);
            double h = 0.5 * (c1.Height + c2.Height);
            double dx = (Canvas.GetLeft(c1) + c1.Width) / 2 - (Canvas.GetLeft(c2) + c2.Width) / 2;
            double dy = (Canvas.GetTop(c1) + c1.Height) / 2 - (Canvas.GetTop(c2) + c2.Height) / 2;

            if (Math.Abs(dx) <= w && Math.Abs(dy) <= h) {
                double wy = w * dy;
                double hx = h * dx;

                if (wy > hx) {
                    if (wy > -hx) {
                        direction = IntersectsEnum.TOP;
                    }
                    else {
                        direction = IntersectsEnum.LEFT;
                    }
                }
                else {
                    if (wy > -hx) {
                        direction = IntersectsEnum.RIGHT;

                    }
                    else {
                        direction = IntersectsEnum.BOT;
                    }
                }
                return true;
            }
            return false;
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
