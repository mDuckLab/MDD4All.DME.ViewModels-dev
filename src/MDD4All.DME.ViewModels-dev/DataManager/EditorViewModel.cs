using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DME.ViewModels.Editor;
using MDD4All.UI.DataModels.Tree;
using System;
using System.ComponentModel;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class EditorViewModel : ObservableObject, IEditorState
    {
        #region constructor
        public EditorViewModel(DataFileManagerViewModel dataFileManager)
        {
            _dataFileManager = dataFileManager;
            _dataFileManager.PropertyChanged += OnDataFileManagerPropertyChanged;
        }
        #endregion

        #region Properties

        private ObjectTreeViewModel? _treeViewModel;

        private DataFileManagerViewModel _dataFileManager;

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
                    if (TreeViewModel.SelectedNode is ObjectEditorViewModel)
                    {
                        ObjectEditorViewModel objectEditorViewModel = (ObjectEditorViewModel)TreeViewModel.SelectedNode;
                        objectEditorViewModel.EditorState.IsExpanded = true;
                    }
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
            if (e.PropertyName == nameof(DataFileManagerViewModel.DataEditorViewModel))
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

            OnPropertyChanged(nameof(DataEditorViewModel.ActiveObject));
        }

        private void OnTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedNode")
            {
                OnPropertyChanged(nameof(SelectedEditorViewModel));
            }
            else if (e.PropertyName == "HasBeenProcessed")
            {
                OnPropertyChanged(nameof(SelectedEditorViewModel));
            }
        }

        #endregion
    }
}
