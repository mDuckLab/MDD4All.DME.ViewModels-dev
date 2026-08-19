using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    // The shell: which overlay is on screen. The editor is the only screen on this branch, so
    // there is no view state left to switch between.
    public class MainViewModel : ObservableObject
    {
        public MainViewModel(DataManagerFileViewModel dataFileManager)
        {
            _dataFileManager = dataFileManager;
            _dataFileManager.PropertyChanged += OnDataFileManagerPropertyChanged;
        }

        private DataManagerFileViewModel _dataFileManager;

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

        // Losing edits has to be answered, not mentioned in passing, so it takes the same
        // central overlay slot the settings dialog uses.
        private void OnDataFileManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerFileViewModel.ShowUnsavedChangesWarning))
            {
                if (_dataFileManager.ShowUnsavedChangesWarning)
                {
                    ActiveOverlay = OverlayState.UnsavedChanges;
                }
                else
                {
                    ActiveOverlay = OverlayState.None;
                }
            }
        }
    }
}
