using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Localization.Contracts;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Resources;

namespace MDD4All.DME.ViewModels.Editor
{
    public class ObjectTreeViewModel : ObservableObject, ITree
    {
        public ObjectTreeViewModel(object? item, Type? targetType = null,
                                   ILanguageSetter? languageSetter = null)
        {
            LanguageSetter = languageSetter;

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

        // Held rather than read once: the tree outlives a language switch, the culture must not.
        public ILanguageSetter? LanguageSetter { get; }

        // One resource manager per resource type, thrown away with the document. Static would
        // outlive the assembly the type came from, and those are loaded and dropped at runtime.
        private readonly Dictionary<Type, ResourceManager> _resourceManagers
            = new Dictionary<Type, ResourceManager>();

        // The nodes ask here instead of resolving themselves. DisplayAttribute.GetName() would
        // read the value through the generated resource class, and that one resolves against
        // CultureInfo.CurrentUICulture - the value this host cannot reach. Handing the picked
        // culture over is the only way it arrives.
        //
        // A key the resource file does not know falls back to the key itself. GetName() throws
        // in that case; a label reading DisplayName_FirstName says what is missing.
        public string? ResolveDisplayName(DisplayAttribute displayAttribute)
        {
            string? result = displayAttribute.Name;

            if (displayAttribute.ResourceType != null && !string.IsNullOrEmpty(result) &&
                LanguageSetter != null)
            {
                string? text = GetResourceManager(displayAttribute.ResourceType)
                                   .GetString(result, LanguageSetter.CurrentCulture);

                if (text != null)
                {
                    result = text;
                }
            }

            return result;
        }

        private ResourceManager GetResourceManager(Type resourceType)
        {
            ResourceManager? result;

            if (!_resourceManagers.TryGetValue(resourceType, out result))
            {
                // The generated class builds its own manager from exactly this name.
                result = new ResourceManager(resourceType);
                _resourceManagers.Add(resourceType, result);
            }

            return result;
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