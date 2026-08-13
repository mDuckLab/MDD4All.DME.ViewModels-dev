namespace MDD4All.DME.ViewModels.DataManager
{
    // What came of an attempt to turn file content back into an object. Says what happened, not
    // what to tell the user - phrasing that is the job of whoever is closer to the screen.
    public enum LoadResult
    {
        Loaded,
        NotReadableAsJson,
        DoesNotMatchType,
        DeserializationFailed,
        NoObject
    }
}
