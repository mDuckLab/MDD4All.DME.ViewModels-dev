using System;
using System.Reflection;
using System.Runtime.Loader;

namespace MDD4All.DME.ViewModels.DataManager
{
    // Serializing an object whose class the application does not know.
    //
    // The user picks a data model DLL at runtime and it is loaded into a DataModelLoadContext of its
    // own, so that two models can use different versions of the same dependency without clashing.
    // Everything the model needs is loaded in there with it - including a second copy of our own
    // MDD4All.DME.Proxies:
    //
    //      default context                  DataModelLoadContext, one per model
    //      ---------------                  ----------------------------------
    //      MDD4All.DME.Proxies.dll          SomeDataModel.dll          the model itself
    //      Newtonsoft.Json.dll              MDD4All.DME.Proxies.dll    a second copy
    //                                       Newtonsoft.Json.dll        a second copy
    //
    // Reading the model's types from the left side is fine. Type and PropertyInfo work across the
    // boundary, and the whole editor is built on that. Turning a *name* into a type is not: the
    // runtime looks for it in the context the searching code is running in. Json.NET does exactly
    // that with the "$type" it writes into a file, so it finds SomeDataModel only while running on
    // the right.
    //
    // The serializer therefore has to run on the right - and .NET offers no way to say "run this
    // call over there". A context is chosen by loading an assembly into it, never per call. Writing
    //
    //      new JsonSerializerProxy().Deserialize(json, targetType)
    //
    // would always reach the left copy. This project has no reference to MDD4All.DME.Proxies on
    // purpose, so that line cannot even be written here.
    //
    // What is left is to reach the right-hand copy by name, which is what every method below does:
    //
    //      1. from the type at hand, find its load context and the proxy copy inside it
    //      2. build an instance of the proxy class out of that copy
    //      3. call the wanted method on it by name
    //
    // Only string, bool and Type may cross - those mean the same thing on both sides, anything else
    // would be a different type over there. And steps 2 and 3 are unchecked: rename a method in the
    // proxy and this still compiles, then quietly returns nothing.
    public static class DynamicInvoker
    {

        // Writes the object as XML. There is no counterpart for reading.
        public static string SerializeXml(object obj)
        {
            Assembly proxies = FindProxiesAssembly(obj.GetType());

            string result = InvokeXml(proxies, obj);

            return result;
        }

        // Writes the object as JSON. The two settings travel as bare bool because a
        // JsonSerializerSettings assembled here would be the wrong type on the other side.
        public static string SerializeJson(object obj, bool includeTypeInformation, bool writeComplexDictionaryKeys)
        {
            Assembly proxies = FindProxiesAssembly(obj.GetType());

            string result = InvokeJson(proxies, obj, includeTypeInformation, writeComplexDictionaryKeys);

            return result;
        }

        // Builds the object graph back up from JSON. Takes the target type rather than an object,
        // because there is no instance yet whose context could be looked up.
        public static object? DeserializeJson(string json, Type targetType)
        {
            Assembly proxies = FindProxiesAssembly(targetType);

            object? result = InvokeJsonDeserialize(proxies, json, targetType);

            return result;
        }

        // Step 1 for all three: the copy of the proxy assembly living in the same context as the
        // given type. Missing it is not something to carry on from - working with the wrong copy
        // would produce data that cannot be read back.
        private static Assembly FindProxiesAssembly(Type type)
        {
            Assembly? result = null;

            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(type.Assembly);

            if (alc != null)
            {
                foreach (Assembly asm in alc.Assemblies)
                {
                    if (asm.GetName().Name == "MDD4All.DME.Proxies")
                    {
                        result = asm;
                        break;
                    }
                }
            }

            if (result == null)
            {
                throw new Exception("MDD4All.DME.Proxies ist nicht im gleichen AssemblyLoadContext geladen.");
            }

            return result;
        }

        // Steps 2 and 3 for JsonSerializerProxy.Deserialize.
        private static object? InvokeJsonDeserialize(Assembly helperAssembly, string json, Type targetType)
        {
            object? result = null;

            Type? proxyType = helperAssembly.GetType("MDD4All.DME.Proxies.JsonSerializerProxy");

            if (proxyType != null)
            {
                MethodInfo? method = proxyType.GetMethod("Deserialize");

                object? proxy = Activator.CreateInstance(proxyType);

                if (method != null)
                {
                    result = method.Invoke(proxy, new object[] { json, targetType });
                }
            }

            return result;
        }

        // Steps 2 and 3 for XmlSerializerProxy.Serialize.
        private static string InvokeXml(Assembly helperAssembly, object obj)
        {
            string result = "";

            Type? proxyType = helperAssembly.GetType("MDD4All.DME.Proxies.XmlSerializerProxy");

            if (proxyType != null)
            {
                MethodInfo? method = proxyType.GetMethod("Serialize");

                object? proxy = Activator.CreateInstance(proxyType);

                if (method != null)
                {
                    string? xml = (string?)method.Invoke(proxy, new[] { obj });

                    if (xml != null)
                    {
                        result = xml;
                    }
                }
            }

            return result;
        }

        // Steps 2 and 3 for JsonSerializerProxy.Serialize.
        private static string InvokeJson(Assembly helperAssembly, object obj, bool includeTypeInformation,
                                         bool writeComplexDictionaryKeys)
        {
            string result = "";

            Type? proxyType = helperAssembly.GetType("MDD4All.DME.Proxies.JsonSerializerProxy");

            if (proxyType != null)
            {
                MethodInfo? method = proxyType.GetMethod("Serialize");

                object? proxy = Activator.CreateInstance(proxyType);

                if (method != null)
                {
                    string? json = (string?)method.Invoke(proxy, new object[] { obj, includeTypeInformation,
                                                                                writeComplexDictionaryKeys });

                    if (json != null)
                    {
                        result = json;
                    }
                }
            }

            return result;
        }

    }
}
