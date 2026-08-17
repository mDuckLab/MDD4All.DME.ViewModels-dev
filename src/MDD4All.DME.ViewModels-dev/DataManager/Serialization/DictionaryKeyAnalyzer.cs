using MDD4All.Reflection;
using System.Reflection;

namespace MDD4All.DME.ViewModels.DataManager
{
    // JSON only allows strings as property names, so a dictionary keyed by an object cannot be
    // written the plain way. This finds those places in a data model before anything is written,
    // so a save that would drop them can name them instead of just warning about "some data".
    //
    // Deliberately not part of MDD4All.DME.Proxies: that assembly is loaded into the data model's
    // own context because its code has to run there. This code does not - it only reads types.
    public class DictionaryKeyAnalyzer
    {
        public string[] FindDictionariesWithComplexKey(Type? rootType)
        {
            List<string> found = new List<string>();

            if (rootType != null)
            {
                CollectComplexKeyDictionaries(rootType, "", new List<Type>(), found);
            }

            return found.ToArray();
        }

        // Types can reference each other, so each one is descended into only once. A dictionary
        // reachable by two paths is therefore reported under the first path found.
        private void CollectComplexKeyDictionaries(Type type, string path, List<Type> visited, List<string> found)
        {
            if (!visited.Contains(type))
            {
                visited.Add(type);

                foreach (PropertyInfo property in type.GetProperties())
                {
                    string propertyPath = property.Name;

                    if (path != "")
                    {
                        propertyPath = path + "." + property.Name;
                    }

                    if (IsDictionaryWithComplexKey(property.PropertyType))
                    {
                        found.Add(propertyPath);
                    }

                    foreach (Type nested in TypesInside(property.PropertyType))
                    {
                        CollectComplexKeyDictionaries(nested, propertyPath, visited, found);
                    }
                }
            }
        }

        // Word for word what DictionaryJsonConverter.CanConvert asks, down to the same two calls,
        // so a property is reported here exactly when the converter would take it.
        private bool IsDictionaryWithComplexKey(Type type)
        {
            bool result = false;

            TypeAnalyzer analyst = TypeAnalyzer.CreateAnalyst(type);

            if (analyst.TypeCategory == TypeCategory.IDictionary)
            {
                result = !TypeAnalyzer.IsSimpleDataType(analyst.UnderlyingTypes[0]);
            }

            return result;
        }

        // A dictionary's values, a list's elements and a plain object can each hold a dictionary
        // further down.
        private List<Type> TypesInside(Type type)
        {
            List<Type> result = new List<Type>();

            if (type.IsArray)
            {
                Type? elementType = type.GetElementType();

                if (elementType != null)
                {
                    result.Add(elementType);
                }
            }
            else if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                {
                    result.Add(argument);
                }
            }
            else if (type.IsClass && type != typeof(string))
            {
                result.Add(type);
            }

            return result;
        }
    }
}
