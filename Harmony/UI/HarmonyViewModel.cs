using MahApps.Metro;
using System.ComponentModel;
using System.Security;
using System.Windows;

namespace Harmony.UI {
    public class HarmonyViewModel : INotifyPropertyChanged {

        public string StartButton_Value {
            get => NotStarted ? "Start" : "Stop"; 
        }

        public bool NotStarted {
            get => notStarted; set {
                notStarted = value;
                RaisePropertyChanged("NotStarted");
                RaisePropertyChanged("StartButton_Value");
            }
        }

        public bool IsMaster {
            get => isMaster; set {
                if (isMaster == value) return;
                isMaster = value;
                RaisePropertyChanged("IsMaster");
            }
        }

        public bool Darkmode {
            get => darkmode; set {
                darkmode = value;
                ThemeManager.ChangeTheme(Application.Current, ThemeManager.GetInverseTheme(ThemeManager.DetectTheme()));
                RaisePropertyChanged("Darkmode");
            }
        }

        public string IpAddress {
            get => ipAddress; set {
                ipAddress = value;
                RaisePropertyChanged("IpAddress");
            }
        }

        public string Port {
            get => port; set {
                port = value;
                RaisePropertyChanged("Port");
            }
        }

        private string ipAddress = "localhost";
        private string port = "56958";
        private bool darkmode = false;
        private bool notStarted = true;
        private bool isMaster = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
