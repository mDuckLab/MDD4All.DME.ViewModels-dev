using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.FileAccess.Contracts;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml.Serialization;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataSerializationViewModel : ObservableObject
    {
        private IFileSaver _fileSaver;

        public DataSerializationViewModel(string fileName,
                                   Type dataModelRootType,
                                   IFileSaver fileSaver)
        {
            _fileName = fileName;
            _selectedType = dataModelRootType;
            _fileSaver = fileSaver;

            SerializerSettings = new JsonSerializerSettings
            {
                // Includes the full C# type name in the JSON (as $type). 
                // This is vital for deserializing inherited classes correctly.
                TypeNameHandling = TypeNameHandling.Auto,
                // Forces the reader to look for metadata (like $type or $id) at the beginning.
                //MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                //// Ensures that the same object isn't saved twice; instead, it uses references ($id/$ref).
                //PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                //// Prevents the serializer from crashing if objects point to each other in a circle.
                //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // Explicitly writes 'null' into the JSON file instead of skipping the property.
                // Also needed on the way back in: without an explicit "Property": null in the
                // file, deserialization leaves whatever the target type's constructor already
                // set (e.g. PersonRepository's constructor seeds PersonArray with test data),
                // so a deleted/nulled property would silently reappear after reload.
                NullValueHandling = NullValueHandling.Include,
                // Ensures a "fresh start" by replacing existing collections and objects instead of 
                // appending new data to them. This prevents data pollution and duplicate entries.
                // Example: If a list currently has 3 items and you load a file containing 2 items, 
                // 'Replace' ensures the list has exactly 2 items. Without this, the list would 
                // incorrectly grow to 5 items due to default 'Append' behavior.
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                //// Allows the use of private or internal constructors when creating objects from JSON.
                //ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                //// Uses a simplified assembly name in the $type metadata for better compatibility.
                //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                // Formats the resulting JSON string with indentation and line breaks for human readability.
                Formatting = Formatting.Indented,
                // Adds a custom converter to handle Dictionary structures correctly during conversion.
                // This converter handles the transformation of IDictionary objects.
                // It solves the problem that standard JSON only allows strings as keys, 
                // whereas C# dictionaries can use complex objects as keys.
                //Converters = new List<JsonConverter> { new DictionaryJsonConverter() }
            };
        }


        private object? _activeObject;

        public object? ActiveObject
        {
            get
            {
                return _activeObject;
            }
            set
            {
                if (_activeObject != value)
                {
                    _activeObject = value;
                    OnPropertyChanged(nameof(ActiveObject));
                }
            }
        }

        private Type? _selectedType;

        public Type? SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    OnPropertyChanged(nameof(SelectedType));
                }
            }
        }

        private string _fileName = "";

        public string FileName
        {
            get 
            { 
                return _fileName; 
            }
            set
            { 
                _fileName = value; 
            }
        }


        public JsonSerializerSettings SerializerSettings { get; private set; }

        public bool ShowXml { get; set; }

        // Set from the corresponding setting before saving, so the written file can carry
        // its own type and be reopened without picking the matching data model first.
        public bool IncludeTypeInformation { get; set; } = false;

        public string ActiveObjectJsonString
        {
            get
            {
                string result = string.Empty;

                if (ActiveObject != null && SelectedType != null)
                {
                    result = DynamicInvoker.SerializeJson(ActiveObject, IncludeTypeInformation);
                }

                return result;
            }
        }

        public string ActiveObjectXmlString
        {
            get
            {
                string result = string.Empty;

                if (ActiveObject != null && SelectedType != null)
                {
                    try
                    {
                        result = DynamicInvoker.SerializeXml(ActiveObject);
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception);
                    }
                }

                return result;
            }
        }

        public void CreateNewInstance()
        {
            if (SelectedType != null)
            {
                // Assigning to the property triggers OnPropertyChanged
                ActiveObject = Activator.CreateInstance(SelectedType);
            }
        }




        public void LoadFromFile()
        {
            if (FileName.ToLower().EndsWith("json"))
            {
                try
                {
                    string json = File.ReadAllText(FileName);

                    object? deserializedJson = DynamicInvoker.DeserializeJson(json, SelectedType!);

                    if (deserializedJson != null)
                    {
                        ActiveObject = deserializedJson;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
            else if(FileName.ToLower().EndsWith("xml"))
            {
                try
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(SelectedType);
                    
                    FileStream fileStream = new FileStream(FileName, FileMode.Open);
                    // Call the Deserialize method and cast to the object type.
                    ActiveObject = xmlSerializer.Deserialize(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
                catch(Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        // Reads only the $type metadata Json.NET wrote into the file, without deserializing the rest.
        // The result is assembly-qualified ("Namespace.Type, AssemblyName") - callers that need the
        // plain type name have to strip the assembly part themselves.
        public static string? ReadTypeNameFromJson(string jsonContent)
        {
            string? result = null;

            try
            {
                Newtonsoft.Json.Linq.JObject rawJson = Newtonsoft.Json.Linq.JObject.Parse(jsonContent);
                Newtonsoft.Json.Linq.JToken? typeToken = rawJson["$type"];

                if (typeToken != null)
                {
                    result = typeToken.ToString();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return result;
        }

    }
}