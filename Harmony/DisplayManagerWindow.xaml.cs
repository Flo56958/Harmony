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
            var shrink = 20;
            DisplayCanvas.Children.Clear();
            var width = DisplayCanvas.Width / 2;
            var height = DisplayCanvas.Height / 2;

            var displays = DisplayManager.Displays;

            foreach (DisplayManager.Display dis in displays) {
                Debug.WriteLine(dis.ToString());
                Rectangle recti = new Rectangle {
                    Width = dis.Screen.Width / shrink,
                    Height = dis.Screen.Height / shrink,

                    StrokeThickness = 2,

                    Stroke = Brushes.Black,
                    Fill = dis.OwnDisplay ? Brushes.CadetBlue : Brushes.Red
                };
                Canvas.SetLeft(recti, dis.Location.X / shrink + width);
                Canvas.SetTop(recti, dis.Location.Y / shrink + height);
                var i = DisplayCanvas.Children.Add(recti);
                Debug.WriteLine(i);
            }
        }
    }
}
