using System;
using System.Reflection;
using System.Runtime.Loader;

namespace MDD4All.DME.ViewModels.DataManager
{
    public static class DynamicInvoker
    {

        public static string SerializeXml(object obj)
        {
            Type type = obj.GetType();
            Assembly assembly = type.Assembly;

            // The proxy has to run inside the object's own AssemblyLoadContext, otherwise
            // its type resolution would not match the object's actual type identity.
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(assembly);

            if (alc != null)
            {
                foreach (Assembly asm in alc.Assemblies)
                {
                    if (asm.GetName().Name == "MDD4All.DME.Proxies")
                    {
                        return InvokeXml(asm, obj);
                    }
                }
            }
            throw new Exception("Helper.dll nicht im gleichen AssemblyLoadContext geladen.");
        }

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

        private static string InvokeJson(Assembly helperAssembly, object obj, bool includeTypeInformation)
        {
            string result = "";

            Type? proxyType = helperAssembly.GetType("MDD4All.DME.Proxies.JsonSerializerProxy");
            if (proxyType != null)
            {
                MethodInfo? method = proxyType.GetMethod("Serialize");

                object? proxy = Activator.CreateInstance(proxyType);
                if (method != null)
                {
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
