namespace MDD4All.DME.Configurations
{
    // Serializable stand-in for a Type - a Type can't be persisted and its identity isn't stable across AssemblyLoadContext reloads, so this is what actually gets saved and resolved back later.
    public class DataModelDescriptor
    {
        public string DllPath { get; set; } = "";

        public string FullTypeName { get; set; } = "";
    }
}
