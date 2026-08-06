using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DME.ViewModels.Editor;
using MDD4All.Localization.Contracts;
using System;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class MainViewModel : ObservableObject, INavigation
    {
        #region constructor
        public MainViewModel(ILanguageSetter languageSetter, DataFileManagerViewModel dataFileManager)
        {
            _languageSetter = languageSetter;
            _languageSetter.CultureChanged += OnCultureChanged;

            _dataFileManager = dataFileManager;
            _dataFileManager.PropertyChanged += OnDataFileManagerPropertyChanged;
        }
        #endregion

        #region Properties

        private ILanguageSetter _languageSetter;

        private DataFileManagerViewModel _dataFileManager;

        private EViewState _viewState = EViewState.ShowStartPage;

        public EViewState ViewState
        {
            get
            {
                return _viewState;
            }

            set
            {
                _viewState = value;
                OnPropertyChanged(nameof(ViewState));
            }
        }

        // At most one overlay is ever open at once (modals block everything else).
        private EOverlayState _activeOverlay = EOverlayState.None;

        public EOverlayState ActiveOverlay
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
            ActiveOverlay = EOverlayState.Settings;
        }

        public void ShowStartPage()
        {
            // TODO save changes
            ViewState = EViewState.ShowStartPage;
        }

        #endregion

        #region Event Handlers

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            ActiveOverlay = EOverlayState.CultureChange;
        }

        private void OnDataFileManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataFileManagerViewModel.DataEditorViewModel))
            {
                ViewState = EViewState.ShowEditor;
            }
            else if (e.PropertyName == nameof(DataFileManagerViewModel.AssemblyTreeViewModel))
            {
                ActiveOverlay = _dataFileManager.AssemblyTreeViewModel != null
                    ? EOverlayState.TypeSelection
                    : EOverlayState.None;
            }
        }

        #endregion
    }
}
