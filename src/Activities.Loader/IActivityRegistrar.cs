namespace NagasakaEventSystem.Activities.Loader;

public interface IActivityRegistrar
{
    Task RegisterExportedActivitiesAsync(System.Reflection.Assembly assembly, CancellationToken cancellationToken);
}
