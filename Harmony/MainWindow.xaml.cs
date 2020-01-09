using System;
using System.Windows;
using System.Windows.Controls;

namespace Harmony
{

    public partial class MainWindow : Window {
        private bool started = false;

        public static TextBlock debug;
        public MainWindow()
        {
            InitializeComponent();
            debug = Debug;
            Debug.Text = "";
            DisplayManager.SetUp();
        }

        private void OnClickStart(object sender, RoutedEventArgs e) {
            if(!started) { 
                var ip = IPInput.Text;
                var port = PortInput.Text;

                if (!Int32.TryParse(port, out var iport)) return;
                bool isMaster = MasterCheckBox.IsChecked != null && (bool)MasterCheckBox.IsChecked;

                var networkCommunicator = new NetworkCommunicator(ip, iport, isMaster);
                if (NetworkCommunicator.Instance == null) return;
                if (isMaster) {
                    MouseHook.Start();
                }

                started = true;
            }
            else {
                if (NetworkCommunicator.Instance != null) {
                    NetworkCommunicator.Instance.Close();
                    NetworkCommunicator.Instance = null;
                }
                
                started = false;
            }
        }
    }
}
