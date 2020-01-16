using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harmony.UI {
    class HarmonyViewModel : INotifyPropertyChanged {
        #region Construction
        /// Constructs the default instance of a SongViewModel
        public HarmonyViewModel() {
            StartButton_Value = "Start";
            notStarted = true;
            MasterCheckBox_Label = "is Master";
            isMaster = false;
        }
        #endregion

        #region Members
        public string StartButton_Value;
        public bool notStarted;
        public string MasterCheckBox_Label;
        public bool isMaster;
        #endregion

        #region Properties

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        private void RaisePropertyChanged(string propertyName) {
            // take a copy to prevent thread issues
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
