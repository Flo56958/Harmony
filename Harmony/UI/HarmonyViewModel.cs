using MahApps.Metro;
using System.ComponentModel;
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

        private bool darkmode = false;
        private bool notStarted = true;
        private bool isMaster = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
