namespace NagasakaEventSystem.Activities.Loader;

public sealed class ActivityLoaderOptions
{
    public string Directory { get; set; } = "Activities";
    public string SearchPattern { get; set; } = "*.dll";
    public bool Strict { get; set; } = false;

    /// <summary>
    /// If true, also scan subdirectories.
    /// </summary>
    public bool Recursive { get; set; } = false;
}
