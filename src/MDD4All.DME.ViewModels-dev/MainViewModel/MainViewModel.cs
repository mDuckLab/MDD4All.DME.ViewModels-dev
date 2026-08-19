using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Localization.Contracts;
using System;

namespace MDD4All.DME.ViewModels.DataManager
{
    // The shell: which overlay is on screen, and nothing else. The editor is the only screen on
    // this branch, so there is no view state left to switch between.
    public class MainViewModel : ObservableObject
    {
        #region constructor
        public MainViewModel(ILanguageSetter languageSetter)
        {
            _languageSetter = languageSetter;
            _languageSetter.CultureChanged += OnCultureChanged;
        }
        #endregion

        #region Properties

        private ILanguageSetter _languageSetter;

        // At most one overlay is ever open at once (modals block everything else).
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

        #endregion

        #region INavigation

        public void OpenSettings()
        {
            ActiveOverlay = OverlayState.Settings;
        }

        #endregion

        #region Event Handlers

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            ActiveOverlay = OverlayState.CultureChange;
        }

        #endregion
    }
}
