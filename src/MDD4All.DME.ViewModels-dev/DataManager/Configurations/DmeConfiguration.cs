namespace MDD4All.DME.Configurations
{
    public class DmeConfiguration
    {
        // Where the file dialogs open. The only path worth remembering now that the data models
        // are compiled in rather than picked from disk.
        public string LastUsedDataFilePath { get; set; } = string.Empty;


        // Writes the root object's type into the saved file, so opening it later can tell which
        // data model it belongs to instead of relying on whichever one is currently open.
        public bool SaveTypeInformation { get; set; } = false;

    }
}
