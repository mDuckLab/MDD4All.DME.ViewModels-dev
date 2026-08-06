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

            // richtigen LoadContext holen
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(assembly);

            if (alc != null)
            {
                // Helper-Assembly laden (falls noch nicht geladen)
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

        public static string SerializeJson(object obj)
        {
            Type type = obj.GetType();
            Assembly assembly = type.Assembly;

            // richtigen LoadContext holen
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(assembly);

            if (alc != null)
            {
                // Helper-Assembly laden (falls noch nicht geladen)
                foreach (Assembly asm in alc.Assemblies)
                {
                    if (asm.GetName().Name == "MDD4All.DME.Proxies")
                    {
                        return InvokeJson(asm, obj);
                    }
                }
            }
            throw new Exception("Helper.dll nicht im gleichen AssemblyLoadContext geladen.");
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

        private static string InvokeJson(Assembly helperAssembly, object obj)
        {
            string result = "";

            Type? proxyType = helperAssembly.GetType("MDD4All.DME.Proxies.JsonSerializerProxy");
            if (proxyType != null)
            {
                MethodInfo? method = proxyType.GetMethod("Serialize");

                object? proxy = Activator.CreateInstance(proxyType);
                if (method != null)
                {
                    string? json = (string?)method.Invoke(proxy, new[] { obj });
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
