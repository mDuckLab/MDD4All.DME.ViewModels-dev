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
    }
}
