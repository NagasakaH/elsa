namespace ElsaServer.Options;

public class ActivityAssemblyOptions
{
    public string Directory { get; set; } = "Activities";
    public string SearchPattern { get; set; } = "*.dll";
    public bool Strict { get; set; } = false;
}
