using System.Collections.Generic;

namespace MDD4All.DME.Configurations
{
    public class DmeConfiguration
    {
        public DataModelDescriptor? CurrentDataModel {  get; set; }

        public List<DataModelDescriptor> RecentDataModels { get; set; } = new List<DataModelDescriptor>();

        public List<DataFileDescriptor> RecentDataFiles { get; set; } = new List<DataFileDescriptor>();

        public string LastUsedDataFilePath { get; set; } = string.Empty;

        public string LastUsedDataModelPath {  get; set; } = string.Empty;

        public string DesiredLanguage {  get; set; } = "en-US";

        // Writes the root object's type into the saved file, so opening it later can restore
        // the matching data model instead of relying on whichever one is currently selected.
        public bool SaveTypeInformation { get; set; } = false;

        // A dictionary keyed by an object cannot be written as a plain JSON object - JSON only
        // allows strings as names. Enabled, those are written as Key/Value pairs, which keeps the
        // keys but is a format only this application understands. Disabled, such a dictionary is
        // written as null, so the file stays readable for other tools at the cost of its content.
        public bool WriteComplexDictionaryKeys { get; set; } = true;

        // How the loss above is announced. A dialog has to be answered before anything is written,
        // the notification bar reports it after the fact and never stops a save.
        public bool ConfirmComplexKeyLossWithDialog { get; set; } = true;
    }
}
