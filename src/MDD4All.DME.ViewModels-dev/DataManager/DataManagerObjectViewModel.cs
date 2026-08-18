using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MDD4All.DME.ViewModels.DataManager
{
    // The object currently being edited, and the type it was built from.
    //
    // Built once at startup and never replaced - only its content changes when another file is
    // opened. That is what lets every reader subscribe a single time and never again.
    //
    // Filled by DataManagerFileViewModel alone. It knows this class, this class knows nobody.
    public class DataManagerObjectViewModel : ObservableObject
    {
        private Type? _rootType;

        // The type the open file was read as - not necessarily the selected data model, because
        // a file naming its own type brings its own.
        public Type? RootType
        {
            get
            {
                return _rootType;
            }
        }

        private object? _rootObject;

        // The root of the object graph. Everything below it is the editor tree's business.
        public object? RootObject
        {
            get
            {
                return _rootObject;
            }
        }

        public bool HasContent
        {
            get
            {
                bool result = (_rootObject != null);

                return result;
            }
        }

        // Both values belong to the same file, so they are set together. Separate property setters
        // would send the first notification while the second value is still the old one, and a
        // reader calling back in between would see a mismatched pair.
        public void SetObject(Type? rootType, object? rootObject)
        {
            _rootType = rootType;
            _rootObject = rootObject;

            this.OnPropertyChanged(nameof(RootType));
            this.OnPropertyChanged(nameof(RootObject));
            this.OnPropertyChanged(nameof(HasContent));
        }
    }
}
