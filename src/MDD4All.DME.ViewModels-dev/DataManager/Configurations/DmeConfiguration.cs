namespace MDD4All.DME.Configurations
{
    public class DmeConfiguration
    {
        // Where the file dialogs open. The only path worth remembering now that the data models
        // are compiled in rather than picked from disk.
        public string LastUsedDataFilePath { get; set; } = string.Empty;

        public string DesiredLanguage {  get; set; } = "en-US";

        // Writes the root object's type into the saved file, so opening it later can tell which
        // data model it belongs to instead of relying on whichever one is currently open.
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
