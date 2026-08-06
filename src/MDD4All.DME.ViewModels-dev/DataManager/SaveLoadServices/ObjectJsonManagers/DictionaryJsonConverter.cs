using Newtonsoft.Json;
using System.Collections;
using System;

namespace MDD4All.DME.ViewModels.DataManager
{
    public class DictionaryJsonConverter : JsonConverter
    {
        // This method is called internally by the Newtonsoft.Json library during serialization and deserialization.
        // It serves as an automated "gatekeeper" to determine if this converter is responsible for the current object type.
        // Because we registered this converter in the 'SerializerSettings', the framework performs this check automatically
        // for every object it encounters, without requiring an explicit call in our business logic.
        public override bool CanConvert(Type objectType)
        {
            // We return true if the type implements the IDictionary interface.
            return typeof(IDictionary).IsAssignableFrom(objectType);
        }

        // Converts the C# IDictionary into a JSON structure.
        // It branches into two different formats depending on whether the dictionary key 
        // is a simple type (like string/int) or a complex object.
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // First, we ensure the value is actually a dictionary.
            if (value is IDictionary dictionary)
            {
                // We retrieve the generic type arguments (Key and Value types) of the dictionary.
                Type[] genericArguments = value.GetType().GetGenericArguments();

                if (genericArguments.Length > 0)
                {
                    Type keyType = genericArguments[0];

                    // A 'simple key' can be easily converted to a string and used as a JSON property name.
                    // Example: "PersonID": 123
                    bool isSimpleKey = keyType == typeof(string) ||
                                       keyType.IsPrimitive ||
                                       keyType == typeof(Guid);

                    if (isSimpleKey)
                    {
                        // FORMAT 1: Standard JSON Object
                        // Used when keys are simple strings or numbers.
                        // Result: { "Key1": "Value1", "Key2": "Value2" }
                        writer.WriteStartObject();
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            // JSON property names must be strings, so we call ToString().
                            writer.WritePropertyName(entry.Key?.ToString() ?? string.Empty);
                            serializer.Serialize(writer, entry.Value);
                        }
                        writer.WriteEndObject();
                    }
                    else
                    {
                        // FORMAT 2: Array of Key-Value Pairs
                        // Used when keys are complex objects (e.g., a 'Company' object as a key).
                        // JSON properties cannot be objects, so we wrap them in a list of objects.
                        // Result: [ { "Key": {...}, "Value": {...} }, ... ]
                        writer.WriteStartArray();
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            writer.WriteStartObject();

                            writer.WritePropertyName("Key");
                            serializer.Serialize(writer, entry.Key);

                            writer.WritePropertyName("Value");
                            serializer.Serialize(writer, entry.Value);

                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();
                    }
                }
                else
                {
                    // Fallback for non-generic dictionaries where types cannot be determined.
                    writer.WriteNull();
                }
            }
            else
            {
                // If the value is null or not a dictionary, we write a null value to the JSON.
                writer.WriteNull();
            }
        }

        // Reconstructs the C# IDictionary from the JSON data.
        // This method acts like a detective, checking whether the JSON contains a 
        // standard object or our custom array-based format for complex keys.
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // Creates a new, empty instance of the specific dictionary type (e.g., Dictionary<string, int>).
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(objectType)!;

            // We retrieve the generic type definitions for the Key and the Value.
            Type[] genericArguments = objectType.GetGenericArguments();
            Type keyType = genericArguments[0];
            Type valueType = genericArguments[1];

            // CASE 1: The data is stored as an Array.
            // This happens when 'WriteJson' used the safe format for complex keys.
            // Example in JSON: [ { "Key": {"Id": 1}, "Value": "Admin" } ]
            if (reader.TokenType == JsonToken.StartArray)
            {
                // We load the entire array into a temporary JArray object for processing.
                Newtonsoft.Json.Linq.JArray temporaryArray = Newtonsoft.Json.Linq.JArray.Load(reader);

                foreach (Newtonsoft.Json.Linq.JToken item in temporaryArray)
                {
                    // We manually extract the "Key" and "Value" and convert them back to their original C# types.
                    object? key = item["Key"]?.ToObject(keyType, serializer);
                    object? value = item["Value"]?.ToObject(valueType, serializer);

                    if (key != null)
                    {
                        dictionary.Add(key, value);
                    }
                }
            }
            // CASE 2: The data is stored as a standard JSON Object.
            // This happens when the keys were simple types (strings or numbers).
            // Example in JSON: { "123": "UserAccount" }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                // Since the format matches the standard JSON expectation, 
                // we let the default serializer fill the dictionary for us.
                serializer.Populate(reader, dictionary);
            }

            return dictionary;
        }
    }
}
