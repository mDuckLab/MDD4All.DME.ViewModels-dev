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
    }
}
