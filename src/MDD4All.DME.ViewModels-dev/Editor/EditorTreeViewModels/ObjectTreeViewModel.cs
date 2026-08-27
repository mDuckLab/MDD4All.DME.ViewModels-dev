using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DME.ViewModels.Editor.Settings;
using MDD4All.DME.ViewModels.Localization;
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
        public ObjectTreeViewModel(object? item, Type? targetType = null,
                                   AnnotationTextProvider? annotationTexts = null,
                                   EditorAppearanceSettingsViewModel? appearanceSettings = null)
        {
            AnnotationTexts = annotationTexts;
            _appearanceSettings = appearanceSettings;

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
                root.EditorState.IsExpanded = true;
                root.Tree = this;
                this.TreeRootNodes.Add(root);
                SelectedNode = root;
            }
        }

        // Reads the [Display] labels of the loaded data model. Held, not built here - the tree
        // is how a node reaches it, not the place that does the work.
        public AnnotationTextProvider? AnnotationTexts { get; }

        private readonly EditorAppearanceSettingsViewModel? _appearanceSettings;

        // Whether the nodes should prefer the annotated label over the property name. No
        // settings at hand means yes - that is what the annotations are there for.
        public bool ShowAnnotationNames
        {
            get
            {
                bool result = true;

                if (_appearanceSettings != null)
                {
                    result = _appearanceSettings.ShowAnnotationNames;
                }

                return result;
            }
        }

        private ITreeNode? _selectedNode;

        public ITreeNode? SelectedNode
        {
            get
            {
                return _selectedNode;
            }
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

        // How many levels the tree actually has, counting the root as 1.
        //
        // Walked on every read rather than remembered. Create, Add and Delete all change it, and
        // Create changes it by an unknown amount - it builds the whole subtree of the new object
        // in one go. A stored value would be stale after any of them, and the tree is in memory
        // anyway, so walking it costs nothing worth saving.
        public int MaxDepth
        {
            get
            {
                int result = 0;

                foreach (ITreeNode rootNode in this.TreeRootNodes)
                {
                    int depthOfBranch = MeasureDepth(rootNode);

                    if (depthOfBranch > result)
                    {
                        result = depthOfBranch;
                    }
                }

                return result;
            }
        }

        // A leaf is one level. Everything hangs in Children, including a dictionary entry's key
        // and value editors, so there is no case to treat separately here.
        private int MeasureDepth(ITreeNode node)
        {
            int deepestChild = 0;

            foreach (ITreeNode child in node.Children)
            {
                int depthOfChild = MeasureDepth(child);

                if (depthOfChild > deepestChild)
                {
                    deepestChild = depthOfChild;
                }
            }

            int result = deepestChild + 1;

            return result;
        }

        public void RaiseTreeChanged()
        {
            OnPropertyChanged("TreeChanged");
        }

        public ObservableCollection<ITreeNode> TreeRootNodes { get; }
    }
}