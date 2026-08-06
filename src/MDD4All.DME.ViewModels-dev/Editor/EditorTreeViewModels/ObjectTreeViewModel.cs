using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MDD4All.DME.ViewModels.Editor
{
    public class ObjectTreeViewModel : ObservableObject, ITree
    {
        public ObjectTreeViewModel(object? item, Type? targetType = null)
        {
            this.TreeRootNodes = new ObservableCollection<ITreeNode>();

            Access access = new RootNodeAccess();

            ObjectEditorViewModel? root = ReferenceEditorViewModel.CreateChildViewModel(this,
                                                                                        access,
                                                                                        item,
                                                                                        targetType,
                                                                                        null,
                                                                                        null!
                                                                                        );

            if (root != null)
            {
                root.IsExpanded = true;
                root.Tree = this;
                this.TreeRootNodes.Add(root);
                SelectedNode = root;
            }
        }

        private ITreeNode? _selectedNode;

        public ITreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;

                    if (_selectedNode != null)
                    {
                        UpdateBreadcrumbPath(_selectedNode);
                    }
                    else
                    {
                        SelectedNodeParentList = null;
                    }

                    OnPropertyChanged(nameof(SelectedNode));
                }
            }
        }

        private void UpdateBreadcrumbPath(ITreeNode node)
        {
            List<ITreeNode> path = new List<ITreeNode>();
            ITreeNode? current = node;

            while (current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            SelectedNodeParentList = path;
        }

        private List<ITreeNode>? _selectedNodeParentList;

        public List<ITreeNode>? SelectedNodeParentList
        {
            get
            {
                return _selectedNodeParentList;
            }
            private set
            {
                if (_selectedNodeParentList != value)
                {
                    _selectedNodeParentList = value;
                    OnPropertyChanged(nameof(SelectedNodeParentList));
                }
            }
        }

        public bool HasBeenProcessed
        {
            set 
            {
                OnPropertyChanged("HasBeenProcessed");
            }
        }

        public ObservableCollection<ITreeNode> TreeRootNodes { get; }
    }
}