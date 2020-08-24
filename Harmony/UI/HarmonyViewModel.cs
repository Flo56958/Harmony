using MahApps.Metro;
using System.ComponentModel;
using System.Windows;

namespace Harmony.UI {
    public class HarmonyViewModel : INotifyPropertyChanged {

        public string StartButton_Value {
            get => NotStarted ? "Start" : "Stop"; 
        }

        public bool NotStarted {
            get => _notStarted; set {
                _notStarted = value;
                RaisePropertyChanged("NotStarted");
                RaisePropertyChanged("StartButton_Value");
            }
        }

        public bool IsMaster {
            get => _isMaster; set {
                if (_isMaster == value) return;
                _isMaster = value;
                RaisePropertyChanged("IsMaster");
            }
        }

        public bool Darkmode {
            get => _darkmode; set {
                _darkmode = value;
                ThemeManager.ChangeTheme(Application.Current, ThemeManager.GetInverseTheme(ThemeManager.DetectTheme()));
                RaisePropertyChanged("Darkmode");
            }
        }

        public bool DebugMode {
            get => _debugmode; set {
                _debugmode = value;
                RaisePropertyChanged("DebugMode");
            }
        }

        public string IpAddress {
            get => _ipAddress; set {
                _ipAddress = value;
                RaisePropertyChanged("IpAddress");
            }
        }

        public string Port {
            get => _port; set {
                _port = value;
                RaisePropertyChanged("Port");
            }
        }

        private string _ipAddress = "localhost";
        private string _port = "56958";
        private bool _darkmode;
        private bool _notStarted = true;
        private bool _isMaster;
        private bool _debugmode = true;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
