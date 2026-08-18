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
        public EditorViewModel(DataManagerObjectViewModel dataManagerObject)
        {
            _dataManagerObject = dataManagerObject;
            _dataManagerObject.PropertyChanged += OnDataManagerObjectPropertyChanged;

            // As a lazily-constructed singleton, this can be built after a data file
            // was already loaded (e.g. the switch to the Editor screen itself triggers
            // this construction) - sync with the current state instead of only
            // waiting for the next change notification, which would otherwise be missed.
            RebuildTree();
        }
        #endregion

        #region Properties

        private ObjectTreeViewModel? _treeViewModel;

        private readonly DataManagerObjectViewModel _dataManagerObject;

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
        private void OnDataManagerObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataManagerObjectViewModel.RootObject))
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

            object? rootObject = _dataManagerObject.RootObject;
            Type? rootType = _dataManagerObject.RootType;

            if (rootObject != null || rootType != null)
            {
                ObjectTreeViewModel newTree = new ObjectTreeViewModel(rootObject, rootType);
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
