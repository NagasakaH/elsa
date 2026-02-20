using System.Reflection;

namespace NagasakaEventSystem.Activities.Contracts;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ActivityModuleMetadataAttribute : Attribute
{
    public ActivityModuleMetadataAttribute(string name, string version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }
    public string Version { get; }
    public string? Description { get; init; }
}
