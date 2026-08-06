using CommunityToolkit.Mvvm.Input;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // RelayCommand comes from CommunityToolkit.Mvvm.Input -> RelayCommand(Action (Functionpointer) , bool (execute possible))s
            this.CreateInstanceCommand = new RelayCommand(ExecuteCreatItem);
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
                    catch (Exception ex)
                    {
                    }
                }

                this.SortChildrenByPrimitiveState();
            }
        }
        #endregion

        #region properties
        public override string BadgeText
        {
            get
            {
                return "ComplexObject";
            }
        }
        #endregion

        #region UI Prioritization
        public ICommand CreateInstanceCommand { get; private set; } = null!;
        #endregion

        #region Sorting Children
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

        #region Comand Execute
        private void ExecuteCreatItem()
        {
            if (this.Type != null)
            {
                object? newInstance = Activator.CreateInstance(this.Type);
                this.Item = newInstance;

                this.Children.Clear();

                if (this.Access is PropertyAccess propertyAccess)
                {
                    ComplexObjectEditorViewModel parentViewModel = (ComplexObjectEditorViewModel)this.Parent!;

                    propertyAccess.PropertyInfo.SetValue(parentViewModel.Item, newInstance);
                    parentViewModel.StateChanged = true;
                }
                else if (this.Access is ListAccess listAccess)
                {
                    ListEditorViewModel parentViewModel = (ListEditorViewModel)this.Parent!;
                    System.Collections.IList? list = parentViewModel.ItemAsList;

                    if (list != null)
                    {
                        list[listAccess.Index] = newInstance;
                        parentViewModel.StateChanged = true;
                    }
                }

                this.CreateTree();

                EditorState.IsExpanded = true;
            }
        }
        #endregion
    }
}