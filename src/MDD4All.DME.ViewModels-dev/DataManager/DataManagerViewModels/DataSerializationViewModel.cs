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




        // Counterpart to ActiveObjectJsonString - takes content, never a path, so reading
        // the file and parsing it stay separate concerns.
        public void LoadFromJson(string json)
        {
            object? deserializedJson = DynamicInvoker.DeserializeJson(json, SelectedType!);

            if (deserializedJson != null)
            {
                ActiveObject = deserializedJson;
            }
        }

        // Counterpart to ActiveObjectXmlString.
        public void LoadFromXml(string xml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(SelectedType!);

            using (StringReader stringReader = new StringReader(xml))
            {
                ActiveObject = xmlSerializer.Deserialize(stringReader);
            }
        }

        // Checks whether a file plausibly belongs to the given type, for the case where the type was
        // guessed rather than read from the file. Only the top level is compared, because that is
        // where a file identifies itself - if the root does not match, nothing below it can either.
        // Unknown names are rejected, missing ones are not, so a file written before a property was
        // added still loads.
        public static bool RootPropertiesMatch(string jsonContent, Type targetType)
        {
            bool result = true;

            try
            {
                Newtonsoft.Json.Linq.JObject rawJson = Newtonsoft.Json.Linq.JObject.Parse(jsonContent);

                List<string> knownNames = new List<string>();

                foreach (PropertyInfo property in targetType.GetProperties())
                {
                    knownNames.Add(property.Name);
                }

                foreach (Newtonsoft.Json.Linq.JProperty jsonProperty in rawJson.Properties())
                {
                    // Written by Json.NET itself ($type, $id), not part of the type.
                    if (jsonProperty.Name.StartsWith("$"))
                    {
                        continue;
                    }

                    if (!knownNames.Contains(jsonProperty.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        result = false;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                // Unreadable content is not this method's problem - deserializing will report it.
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