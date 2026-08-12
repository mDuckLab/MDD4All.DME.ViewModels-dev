using System;
using System.Reflection;
using System.Runtime.Loader;

namespace MDD4All.DME.ViewModels.DataManager
{
    // The proxy assembly is loaded once per AssemblyLoadContext, so its classes exist several times at
    // runtime. A normal call would always reach the application's copy - the wrong one - which is why
    // the right copy has to be looked up at runtime.
    public static class DynamicInvoker
    {

        public static string SerializeXml(object obj)
        {
            Type type = obj.GetType();
            Assembly assembly = type.Assembly;

            // The object's own type decides which context has to do the work.
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(assembly);

            if (alc != null)
            {
                // DataModelLoadContext loads the proxy assembly into itself, so the right copy is in here.
                foreach (Assembly asm in alc.Assemblies)
                {
                    if (asm.GetName().Name == "MDD4All.DME.Proxies")
                    {
                        return InvokeXml(asm, obj);
                    }
                }
            }
            // Nothing to fall back to - serializing in the wrong context would silently produce
            // data that cannot be read back.
            throw new Exception("Helper.dll nicht im gleichen AssemblyLoadContext geladen.");
        }

        // The type information flag is passed as a primitive because a JsonSerializerSettings built
        // here would be a different type over there - Newtonsoft is loaded separately per context.
        public static string SerializeJson(object obj, bool includeTypeInformation)
        {
            Type type = obj.GetType();
            Assembly assembly = type.Assembly;

            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(assembly);

            if (alc != null)
            {
                foreach (Assembly asm in alc.Assemblies)
                {
                    if (asm.GetName().Name == "MDD4All.DME.Proxies")
                    {
                        return InvokeJson(asm, obj, includeTypeInformation);
                    }
                }
            }
            throw new Exception("Helper.dll nicht im gleichen AssemblyLoadContext geladen.");
        }

        // Takes the target type instead of an object, because when deserializing there is no
        // instance yet - the type is what tells us which load context the proxy has to run in.
        public static object? DeserializeJson(string json, Type targetType)
        {
            Assembly assembly = targetType.Assembly;

            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(assembly);

            if (alc != null)
            {
                foreach (Assembly asm in alc.Assemblies)
                {
                    if (asm.GetName().Name == "MDD4All.DME.Proxies")
                    {
                        return InvokeJsonDeserialize(asm, json, targetType);
                    }
                }
            }
            throw new Exception("Helper.dll nicht im gleichen AssemblyLoadContext geladen.");
        }

        // Type and method are addressed by name because they only exist in the other context -
        // there is nothing here the compiler could bind against.
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

        // Only exists for writing - XmlSerializerProxy has no Deserialize counterpart, so reading XML
        // still bypasses the context switch entirely.
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

        // Does by hand what "new JsonSerializerProxy().Serialize(obj, flag)" would do, because that
        // class has no compile-time name here. Everything is addressed by string, so a rename over
        // there does not break the build - it just returns an empty string.
        private static string InvokeJson(Assembly helperAssembly, object obj, bool includeTypeInformation)
        {
            string result = "";

            // Every context has its own copy of the proxy class, so the type is taken from this copy.
            Type? proxyType = helperAssembly.GetType("MDD4All.DME.Proxies.JsonSerializerProxy");
            if (proxyType != null)
            {
                // Found by name only - nothing checks at build time that Serialize still exists.
                MethodInfo? method = proxyType.GetMethod("Serialize");

                // new would need a type known at build time, so the instance is built from the Type.
                object? proxy = Activator.CreateInstance(proxyType);
                if (method != null)
                {
                    // Invoking it this way runs Serialize inside the model's context, where its types are known.
                    string? json = (string?)method.Invoke(proxy, new object[] { obj, includeTypeInformation });
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
