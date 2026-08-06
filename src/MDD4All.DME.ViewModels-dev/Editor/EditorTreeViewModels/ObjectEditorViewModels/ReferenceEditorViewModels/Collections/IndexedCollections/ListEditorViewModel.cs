using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MDD4All.DME.ViewModels.Editor
{
    public class ListEditorViewModel : IndexedCollectionEditorViewModel
    {
        #region Constructors and Initialization
        public ListEditorViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, null, title, parent, preAnalyzedResult)
        {
            this.InitializeListData();
            this.CreateTree();
        }

        public ListEditorViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, null, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeListData();
            this.CreateTree();
        }

        public ListEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = null, ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
            : base(tree, access, item, targetType, title, parent, preAnalyzedResult)
        {
            this.InitializeListData();
            this.CreateTree();
        }

        private void InitializeListData()
        {
            this.UnderlyingTypeAnalyzer = TypeAnalyzer.CreateAnalyst(base.TypeAnalyzer.UnderlyingTypes[0]);
        }

        public void CreateTree()
        {
            if (this.ItemAsList != null)
            {
                for (int index = 0; index < this.ItemAsList.Count; index++)
                {
                    object? element = this.ItemAsList[index];
                    ListAccess listAccess = new ListAccess(index);
                    ObjectEditorViewModel? childViewModel = ReferenceEditorViewModel.CreateChildViewModel(this.Tree!,
                                                                                                            listAccess,
                                                                                                            element,
                                                                                                            base.UnderlyingType,
                                                                                                            null,
                                                                                                            this,
                                                                                                            base.UnderlyingTypeAnalyzer);

                    if (childViewModel != null)
                    {
                        this.Children.Add(childViewModel);
                    }
                }
            }
        }

        private void CreateListInstance()
        {
            Type listType = typeof(List<>);
            Type concreteListType = listType.MakeGenericType(this.UnderlyingType);

            object? dynamicList = Activator.CreateInstance(concreteListType);


            this.Item = dynamicList;
            this.Children.Clear();
            UpdateParentReference();
        }
        #endregion

        #region properties
        public IList? ItemAsList
        {
            get
            {
                IList? result = null;

                if (base.Item != null)
                {
                    result = (IList)base.Item;
                }

                return result;
            }
            private set
            {
                Item = value;
            }
        }

        override protected string CollectionTypePrefix
        {
            get
            {
                return "list of";
            }
        }
        #endregion

        #region Comand Execute
        override protected void ExecuteCreateInstance()
        {
            this.CreateListInstance();
            EditorState.IsExpanded = true;
        }

        override protected void ExecuteAddItem()
        {
            if (this.ItemAsList == null)
            {
                this.CreateListInstance();
                EditorState.IsExpanded = true;
            }

            if (this.ItemAsList != null)
            {
                object? newElement = null;

                if (this.UnderlyingType == typeof(string))
                {
                    newElement = string.Empty;
                }
                else
                {
                    newElement = Activator.CreateInstance(this.UnderlyingType);
                }

                if (newElement != null)
                {
                    this.ItemAsList.Add(newElement);
                    int newIndex = this.ItemAsList.Count - 1;

                    // Create the child ViewModel using the base factory
                    ObjectEditorViewModel? childViewModel = ReferenceEditorViewModel.CreateChildViewModel(this.Tree!,
                                                                                                            new ListAccess(newIndex),
                                                                                                            newElement,
                                                                                                            this.UnderlyingType,
                                                                                                            null,
                                                                                                            this,
                                                                                                            UnderlyingTypeAnalyzer);

                    if (childViewModel != null)
                    {
                        childViewModel.EditorState.IsExpanded = true;
                        this.Children.Add(childViewModel);
                        if (this.Tree is ObjectTreeViewModel objectTree)
                        {
                            objectTree.HasBeenProcessed = true;
                        }
                    }

                    this.StateChanged = true;
                }
            }
        }

        protected override void ExecuteDeleteAtIndex(int index)
        {
            if (this.ItemAsList != null && index >= 0 && index < ItemAsList.Count)
            {
                // 1. Remove from the underlying data list
                ItemAsList.RemoveAt(index);

                // 2. Remove from the ViewModel children collection
                if (index < Children.Count)
                {
                    Children.RemoveAt(index);
                }

                // 3. IMPORTANT: Correct the indices of subsequent elements
                // The ReorderIndexChild method handles updating the Access objects.
                this.ReorderIndexChild(index);


                if (this.Tree is ObjectTreeViewModel objectTree)
                {
                    objectTree.HasBeenProcessed = true;
                }
            }
        }
        #endregion
    }
}

