using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.Reflection;
using MDD4All.ObjectGraph.Access;
using MDD4All.UI.DataModels.Tree;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace MDD4All.DME.ViewModels.Editor
{
    public abstract class ObjectEditorViewModel : ObservableObject, ITreeNode
    {
        #region Constructors and Initialization
        public ObjectEditorViewModel(ITree tree, Access access, object? item, Type? targetType, string? title = "",
                                        ITreeNode? parent = null, TypeAnalyzer? preAnalyzedResult = null)
        {
            this.Access = access;
            this.Item = item;

            this.Tree = tree;
            this.Parent = parent;
            this.Children = new ObservableCollection<ITreeNode>();

            EditorState = new EditorState(this);

            /* * NOTE: The following TypeAnalyzer initialization logic is technically redundant when 
             * this ViewModel is created via ReferenceEditorViewModel.CreateChildViewModel, 
             * as the factory already ensures a valid TypeAnalyzer is provided. 
   
             * It is kept here as a safety measure (fallback) to guarantee that the TypeAnalyzer 
             * is always correctly initialized, even if the constructor is called directly 
             * or the factory's pre-analysis result is missing.
            */

            if (preAnalyzedResult != null)
            {
                this.TypeAnalyzer = new TypeAnalyzer(preAnalyzedResult);
            }
            else
            {
                TypeAnalyzer = new TypeAnalyzer();
                //type Analyzer is set new when Type is setted 
                if (this.Item != null)
                {
                    this.TypeAnalyzer.Analyze(this.Item);
                }
                else if (targetType != null)
                {
                    this.TypeAnalyzer.Analyze(targetType);
                }
                else
                {
                    this.TypeAnalyzer.Analyze(typeof(object));
                }
            }

            if (!string.IsNullOrEmpty(title))
            {
                Title = title;
            }
        }

        public ObjectEditorViewModel(ITree tree, Access access, TypeAnalyzer preAnalyzedResult, string? title = null, ITreeNode? parent = null)
            : this(tree, access, null, null, title, parent, preAnalyzedResult) { }

        public ObjectEditorViewModel(ITree tree, Access access, object item, TypeAnalyzer preAnalyzedResult, string? title = null, ITreeNode? parent = null)
            : this(tree, access, item, null, title, parent, preAnalyzedResult) { }

        public ObjectEditorViewModel(ITree tree, Access access, object item, string? title = null, ITreeNode? parent = null)
            : this(tree, access, item, null, title, parent, null) { }

        public ObjectEditorViewModel(ITree tree, Access access, Type targetType, string? title = null, ITreeNode? parent = null)
            : this(tree, access, null, targetType, title, parent, null) { }
        #endregion

        #region properties

        public EditorState EditorState { get; private set; }






        private object? _item;

        public virtual object? Item
        {
            get
            {
                return _item;
            }
            set
            {
                if (_item != value)
                {
                    bool wasNullBefore = (_item == null);

                    _item = value;

                    bool isNullNow = (_item == null);

                    OnPropertyChanged(nameof(Item));

                    if (wasNullBefore != isNullNow)
                    {
                        OnPropertyChanged(nameof(IsNull));
                    }
                }
            }
        }

        public Type Type
        {
            get
            {
                Type result = typeof(object);
                if (TypeAnalyzer != null && TypeAnalyzer.AnalyzeType != null)
                {
                    result = TypeAnalyzer.AnalyzeType;
                }
                return result;
            }
            protected set
            {
                if (value != null)
                {
                    TypeAnalyzer.Analyze(value);
                }
                else
                {
                    TypeAnalyzer.Analyze(typeof(object));
                }
            }
        }

        public TypeCategory TypeCategory
        {
            get
            {
                return TypeAnalyzer.TypeCategory;
            }
        }

        public TypeAnalyzer TypeAnalyzer { get; set; } = null!;

        public Access Access { get; set; } = null!;

        public bool IsReadOnly
        {
            get
            {
                return !Access.CanWrite;
            }
        }

        // Short, technical type label ("List"/"Array"/"Dictionary"/"Object") for
        // readers who want to know the underlying .NET shape at a glance - as
        // opposed to ReferenceEditorViewModel.BadgeText's friendlier, more
        // descriptive phrasing ("list of whole numbers").
        public string? TypeBadgeText
        {
            get
            {
                string? result = null;

                switch (TypeCategory)
                {
                    case TypeCategory.IList:
                        result = "List";
                        break;

                    case TypeCategory.Array:
                        result = "Array";
                        break;

                    case TypeCategory.IDictionary:
                        result = "Dictionary";
                        break;

                    case TypeCategory.None:
                        result = "Object";
                        break;
                }

                return result;
            }
        }

        // Single-letter symbol for the Explorer tree, mirroring TypeBadgeText's
        // categories in a more compact form ("s" simple, "c" collection, "O" object).
        public string TypeSymbol
        {
            get
            {
                string result = "";

                switch (TypeCategory)
                {
                    case TypeCategory.Simple:
                    case TypeCategory.SimpleNullable:
                        result = "s";
                        break;

                    case TypeCategory.IList:
                    case TypeCategory.Array:
                    case TypeCategory.IDictionary:
                        result = "c";
                        break;

                    case TypeCategory.None:
                        result = "O";
                        break;
                }

                return result;
            }
        }

        private string? _title;

        public string Title
        {
            get
            {
                string result = string.Empty;
                if (!string.IsNullOrEmpty(_title))
                {
                    result = _title;
                }
                else
                {
                    result = DefaultTitle;
                }
                return result;
            }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        protected virtual string DefaultTitle
        {
            get
            {
                string result = "";

                if (Access is PropertyAccess propertyAccess)
                {
                    try
                    {
                        PropertyInfo propertyInfo = propertyAccess.PropertyInfo;


                        foreach (object attr in propertyInfo.GetCustomAttributes(false))
                        {
                            Type type = attr.GetType();

                            if (type.Name == "DisplayAttribute")
                            {
                                MethodInfo? method = type.GetMethod("GetName");
                                if (method != null)
                                {
                                    object? value = method.Invoke(attr, null);

                                    if (value != null)
                                    {
                                        string? stringValue = value.ToString();

                                        if (stringValue != null)
                                        {
                                            result = stringValue;
                                        }
                                    }
                                }

                            }
                        }
                    }
                    catch
                    {
                        result = string.Empty;
                    }


                    if (result == "")
                    {
                        result = propertyAccess.PropertyInfo.Name;
                    }
                }
                else
                {
                    if (Type != null)
                    {
                        try
                        {

                            foreach (object attr in Type.GetCustomAttributes(false))
                            {
                                Type type = attr.GetType();

                                if (type.Name == "DisplayAttribute")
                                {
                                    MethodInfo? method = type.GetMethod("GetName");
                                    if (method != null)
                                    {
                                        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture;

                                        ;

                                        object? value = method.Invoke(attr, null);

                                        if (value != null)
                                        {
                                            string? stringValue = value.ToString();

                                            if (stringValue != null)
                                            {
                                                result = stringValue;
                                            }
                                        }
                                    }

                                }
                            }
                        }
                        catch
                        {
                            result = string.Empty;
                        }
                    }

                    if (result == string.Empty)
                    {
                        result = Type?.Name ?? "Object";
                    }
                }

                return result;
            }
        }

        public string? DataTypeAnnotation
        {
            get
            {
                string? result = null;

                if (Access is PropertyAccess propertyAccess)
                {
                    try
                    {
                        PropertyInfo propertyInfo = propertyAccess.PropertyInfo;


                        foreach (object attr in propertyInfo.GetCustomAttributes(false))
                        {
                            Type type = attr.GetType();

                            if (type.Name == "DataTypeAttribute")
                            {
                                MethodInfo? method = type.GetMethod("GetDataTypeName");
                                if (method != null)
                                {
                                    object? value = method.Invoke(attr, null);

                                    if (value != null)
                                    {
                                        string? stringValue = value.ToString();

                                        if (stringValue != null)
                                        {
                                            result = stringValue;
                                        }
                                    }
                                }

                            }
                        }
                    }
                    catch
                    {
                        result = string.Empty;
                    }
                }
                return result;
            }
        }

        public bool IsNull
        {
            get
            {
                bool result = false;
                if (this.Item == null)
                {
                    result = true;
                }
                return result;
            }
        }

        public ITreeNode? Parent { get; set; }

        public ObservableCollection<ITreeNode> Children { get; set; } = null!;

        public bool StateChanged
        {
            set
            {
                if (value == true)
                {
                    OnPropertyChanged(nameof(StateChanged));
                }
            }
        }

        public ITree? Tree { get; set; }

        public int Index
        {
            get
            {
                int result = 0;

                if (Parent != null)
                {
                    int counter = 0;
                    foreach (ITreeNode child in Parent.Children)
                    {
                        if (child == this)
                        {
                            result = counter;
                            break;
                        }
                        counter++;
                    }

                }

                return result;
            }
        }

        public bool HasChildNodes
        {
            get
            {
                return Children.Count > 0;
            }
        }

        public bool IsExpanded { get; set; }

        public bool IsSelected
        {
            get
            {
                bool result = false;
                if (Tree != null)
                {
                    result = Tree.SelectedNode == this;
                }
                return result;
            }
            set
            {
            }
        }

        public bool IsLoading
        {
            get
            {
                return false;
            }
        }

        public string Level
        {
            get
            {
                string result = "";

                result = "" + (Index + 1);

                ITreeNode item = this;

                while (item.Parent != null)
                {
                    result = (item.Parent.Index + 1) + "." + result;
                    item = item.Parent;
                }

                return result;
            }
        }

        public bool IsDisabled { get; set; } = false;

        public event EventHandler? TreeStateChanged;

        public string DragDropOperationInformation { get; set; } = string.Empty;
        #endregion
    }
}
