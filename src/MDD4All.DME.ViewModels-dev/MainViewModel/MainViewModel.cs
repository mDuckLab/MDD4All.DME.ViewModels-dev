using CommunityToolkit.Mvvm.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    // The shell. The editor is the only screen on this branch, so all that is left to decide is
    // whether the settings dialog is open.
    public class MainViewModel : ObservableObject
    {
        private OverlayState _activeOverlay = OverlayState.None;

        public OverlayState ActiveOverlay
        {
            get
            {
                return _activeOverlay;
            }

            set
            {
                _activeOverlay = value;
                OnPropertyChanged(nameof(ActiveOverlay));
            }
        }

        public void OpenSettings()
        {
            ActiveOverlay = OverlayState.Settings;
        }
    }
}
