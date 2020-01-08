using System.Windows;

namespace Harmony
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            var networkCommunicator = new NetworkCommunicator("localhost", 55555);

            MouseHook.Start();
        }
    }
}
