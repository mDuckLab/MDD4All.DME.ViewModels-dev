using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DME.ViewModels.Editor;
using MDD4All.UI.DataModels.Tree;
using System;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class EditorViewModel : ObservableObject
    {
        #region constructor
        public EditorViewModel(DataManagerViewModel dataFileManager)
        {
            _dataFileManager = dataFileManager;
            _dataFileManager.PropertyChanged += OnDataFileManagerPropertyChanged;

            // As a lazily-constructed singleton, this can be built after a data file
            // was already loaded (e.g. the switch to the Editor screen itself triggers
            // this construction) - sync with the current state instead of only
            // waiting for the next change notification, which would otherwise be missed.
            RebuildTree();
        }
        #endregion

        #region Properties

        private ObjectTreeViewModel? _treeViewModel;

        private DataManagerViewModel _dataFileManager;

        public ObjectTreeViewModel? TreeViewModel
        {
            get
            {
                return _treeViewModel;
            }

            private set
            {
                if (_treeViewModel != value)
                {
                    _treeViewModel = value;
                    OnPropertyChanged(nameof(TreeViewModel));
                    OnPropertyChanged(nameof(SelectedEditorViewModel));
                }
            }
        }

        public ITreeNode? SelectedEditorViewModel
        {
            get
            {
                ITreeNode? result = null;
                if (TreeViewModel != null)
                {
                    result = TreeViewModel.SelectedNode;
                }
                return result;
            }
        }

        private bool _showRawData = false;

        public bool ShowRawData
        {
            get
            {
                return _showRawData;
            }

            set
            {
                _showRawData = value;
                OnPropertyChanged(nameof(ShowRawData));
            }
        }

        #endregion

        #region Event Handlers
        private void OnDataFileManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerViewModel.DataEditorViewModel))
            {
                RebuildTree();
            }
        }

        private void RebuildTree()
        {
            if (this.TreeViewModel != null)
            {
                this.TreeViewModel.PropertyChanged -= this.OnTreePropertyChanged;
            }

            object? activeObject = _dataFileManager.DataEditorViewModel?.ActiveObject;
            Type? selectedType = _dataFileManager.DataEditorViewModel?.SelectedType;

            if (activeObject != null || selectedType != null)
            {
                ObjectTreeViewModel newTree = new ObjectTreeViewModel(activeObject, selectedType);
                newTree.PropertyChanged += this.OnTreePropertyChanged;
                TreeViewModel = newTree;
            }
            else
            {
                TreeViewModel = null;
            }
        }

        private void OnTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedNode")
            {
                if (TreeViewModel?.SelectedNode is ObjectEditorViewModel selectedNode)
                {
                    selectedNode.EditorState.IsExpanded = true;
                }

                OnPropertyChanged(nameof(SelectedEditorViewModel));
            }
            else if (e.PropertyName == "TreeChanged")
            {
                OnPropertyChanged(nameof(SelectedEditorViewModel));
            }
        }

        #endregion
    }
}
