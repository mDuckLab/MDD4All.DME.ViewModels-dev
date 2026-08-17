using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DataSerializationViewModel : ObservableObject
    {
        public DataSerializationViewModel(Type dataModelRootType)
        {
            _selectedType = dataModelRootType;
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

        public bool ShowXml { get; set; }

        // Set from the corresponding setting before saving, so the written file can carry
        // its own type and be reopened without picking the matching data model first.
        public bool IncludeTypeInformation { get; set; } = false;

        // Off writes a dictionary keyed by an object as null instead of the Key/Value form only
        // this application understands. Costs the content, keeps the file readable elsewhere.
        public bool WriteComplexDictionaryKeys { get; set; } = true;

        public string ActiveObjectJsonString
        {
            get
            {
                string result = string.Empty;

                if (ActiveObject != null && SelectedType != null)
                {
                    result = DynamicInvoker.SerializeJson(ActiveObject, IncludeTypeInformation,
                                                          WriteComplexDictionaryKeys);
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

        // Turns JSON text into the active object, or reports why it could not.
        // Takes the text, never a file path.
        //
        // verifyRootType means the type is only a guess and has to be held against the file.
        // It is false when the file named its own type - then there is nothing to verify.
        public LoadResult LoadFromJson(string json, bool verifyRootType)
        {
            // Three checks, ordered by how much each one can explain:
            //
            //   syntax        - this is not JSON at all
            //   names         - this is JSON, but not written from this type
            //   deserializing - something went wrong
            //
            // Whichever can give the better answer runs first, and the active object stays
            // untouched until all three have passed. The starting value is a failure, so
            // success has to be reached rather than assumed.
            LoadResult result = LoadResult.DeserializationFailed;

            Newtonsoft.Json.Linq.JToken? rawJson = null;

            // Syntax. The parsed result is also what the name comparison below reads from.
            try
            {
                rawJson = Newtonsoft.Json.Linq.JToken.Parse(json);
            }
            catch (Exception exception)
            {
                result = LoadResult.NotReadableAsJson;
                Console.WriteLine(exception);
            }

            if (rawJson != null)
            {
                bool namesMatch = true;

                // Names. Skipped for a stated type, and for a root that is an array - an array
                // carries no names to compare, which happens when the data model root is a list.
                Newtonsoft.Json.Linq.JObject? rootObject = rawJson as Newtonsoft.Json.Linq.JObject;

                if (verifyRootType && rootObject != null)
                {
                    // SelectedType came in through the constructor, and it is either the type read
                    // from the file's own $type or the model the user currently has selected. Here
                    // it supplies the names that are allowed to appear.
                    List<string> knownNames = new List<string>();

                    foreach (PropertyInfo property in SelectedType!.GetProperties())
                    {
                        knownNames.Add(property.Name);
                    }

                    // The other side: the names the file actually carries. $type and $id are
                    // written by Json.NET itself and belong to no class, so they stay out.
                    List<string> fileNames = new List<string>();

                    foreach (Newtonsoft.Json.Linq.JProperty jsonProperty in rootObject.Properties())
                    {
                        if (!jsonProperty.Name.StartsWith("$"))
                        {
                            fileNames.Add(jsonProperty.Name);
                        }
                    }

                    // Compared in this direction on purpose. A name the type does not know means
                    // the file belongs to something else. A name missing from the file does not,
                    // which is what lets a file written before a property was added still load.
                    foreach (string fileName in fileNames)
                    {
                        if (!knownNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                        {
                            namesMatch = false;
                            break;
                        }
                    }
                }

                if (!namesMatch)
                {
                    result = LoadResult.DoesNotMatchType;
                }
                else
                {
                    // Deserializing. Runs inside the data model's load context, and the text goes
                    // over rather than the parsed result - that one belongs to this context.
                    try
                    {
                        object? deserializedJson = DynamicInvoker.DeserializeJson(json, SelectedType!);

                        // A file containing just "null" parses fine and deserializes to nothing.
                        if (deserializedJson == null)
                        {
                            result = LoadResult.NoObject;
                        }
                        else
                        {
                            ActiveObject = deserializedJson;
                            result = LoadResult.Loaded;
                        }
                    }
                    catch (Exception exception)
                    {
                        // result is still DeserializationFailed, so there is nothing to set here.
                        Console.WriteLine(exception);
                    }
                }
            }

            return result;
        }

        // Counterpart to ActiveObjectXmlString. No equivalent of the root check exists for XML yet,
        // so a mismatched file is only noticed when deserializing fails.
        public LoadResult LoadFromXml(string xml)
        {
            LoadResult result = LoadResult.Loaded;

            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(SelectedType!);

                using (StringReader stringReader = new StringReader(xml))
                {
                    object? deserializedXml = xmlSerializer.Deserialize(stringReader);

                    if (deserializedXml == null)
                    {
                        result = LoadResult.NoObject;
                    }
                    else
                    {
                        ActiveObject = deserializedXml;
                    }
                }
            }
            catch (Exception exception)
            {
                result = LoadResult.DeserializationFailed;
                Console.WriteLine(exception);
            }

            return result;
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