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

        // Off, the data model list only offers types a new object can be built from. On, it
        // shows every public class in the assembly - useful to see what a model contains, at
        // the price of entries New cannot do anything with.
        public bool ShowAllDataModels { get; set; } = false;

    }
}
