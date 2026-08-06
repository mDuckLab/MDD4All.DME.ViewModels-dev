namespace MDD4All.DME.Configurations
{
    public class DataFileDescriptor
    {
        public string FilePath { get; set; } = "";

        public DataModelDescriptor DataModelDescription { get; set; } = new DataModelDescriptor();
    }
}
