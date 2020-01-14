using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Harmony {
    /// <summary>
    /// Interaction logic for DisplayManagerWindow.xaml
    /// </summary>
    public partial class DisplayManagerWindow : Window {
        public DisplayManagerWindow() {
            InitializeComponent();
            DisplayCanvas.BeginInit();
        }

        private void OnClickSave(object sender, RoutedEventArgs e) {

        }

        private void OnClickUpdate(object sender, RoutedEventArgs e) {
            Debug.WriteLine("Click");
            var shrink = 20;
            DisplayCanvas.IsEnabled = true;
            DisplayCanvas.Visibility = Visibility.Visible;
            DisplayCanvas.Children.Clear();
            var width = DisplayCanvas.Width;
            var height = DisplayCanvas.Height;

            var displays = DisplayManager.Displays;

            foreach(DisplayManager.Display dis in displays) {
                Rectangle rect = new Rectangle();
                rect.Width = dis.Screen.X / shrink;
                rect.Height = dis.Screen.Y / shrink;

                rect.StrokeThickness = 2;

                rect.Stroke = new SolidColorBrush(Color.FromArgb(0, 0, 255, 0));
                rect.Fill = new SolidColorBrush(dis.OwnDisplay ? Color.FromArgb(0, 255, 0, 0) : Color.FromArgb(255, 0, 0, 0));
                Canvas.SetLeft(rect, 0);//dis.Location.X / shrink + width);
                Canvas.SetTop(rect, 0);//dis.Location.Y / shrink + height);
                DisplayCanvas.Children.Add(rect);
            }

            DisplayCanvas.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
    }
}
