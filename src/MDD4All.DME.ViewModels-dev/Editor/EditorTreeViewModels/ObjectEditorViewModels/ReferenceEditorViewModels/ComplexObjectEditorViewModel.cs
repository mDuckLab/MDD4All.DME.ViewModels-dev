using CommunityToolkit.Mvvm.Input;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace MDD4All.DME.ViewModels.Editor
{
    public class ComplexObjectEditorViewModel : ReferenceEditorViewModel
    {
        #region Constructors and Initialization
        public ComplexObjectEditorViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, title, parent, preAnalyzedResult)
        {
            this.InitializeCommands();
            this.CreateTree();
        }

        public ComplexObjectEditorViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeCommands();
            this.CreateTree();
        }

        public ComplexObjectEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeCommands();
            this.CreateTree();
        }

        private void InitializeCommands()
        {
            // RelayCommand comes from CommunityToolkit.Mvvm.Input -> RelayCommand(Action (Functionpointer) , bool (execute possible))
            this.CreateInstanceCommand = new RelayCommand(ExecuteCreateInstance);
        }

        public void CreateTree()
        {
            if (this.Item != null)
            {
                PropertyInfo[] properties = this.Type!.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        object? rawValue = null;

                        if (this.Item != null)
                        {
                            rawValue = property.GetValue(Item);
                        }
                        Type propertyType = property.PropertyType;

                        PropertyAccess propertyAccess = new PropertyAccess(property);

                        ObjectEditorViewModel? childViewModel = ReferenceEditorViewModel.CreateChildViewModel(this.Tree!,
                                                                                                                propertyAccess,
                                                                                                                rawValue,
                                                                                                                propertyType,
                                                                                                                null,
                                                                                                                this);

                        if (childViewModel != null)
                        {
                            this.Children.Add(childViewModel);
                        }
                    }
                    catch
                    {
                    }
                }

                this.SortChildrenByPrimitiveState();
            }
        }

        // Primitive rows first, complex cards after - the order the editor renders in.
        private void SortChildrenByPrimitiveState()
        {
            List<ITreeNode> sortedList = this.Children.OrderBy(child => child is PrimitivePropertyViewModel ? 0 : 1)
                .ToList();

            this.Children.Clear();

            foreach (ITreeNode sortedNode in sortedList)
            {
                this.Children.Add(sortedNode);
            }
        }
        #endregion

        #region Commands
        public ICommand CreateInstanceCommand { get; private set; } = null!;

        private void ExecuteCreateInstance()
        {
            if (this.Type != null)
            {
                object? newInstance = Activator.CreateInstance(this.Type);
                this.Item = newInstance;

                this.Children.Clear();

                UpdateParentReference();

                this.CreateTree();

                EditorState.IsExpanded = true;
            }
        }
        #endregion
    }
}
