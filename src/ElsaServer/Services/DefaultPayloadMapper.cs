namespace ElsaServer.Services;

public interface IPayloadMapper
{
    IDictionary<string, object> Map(IDictionary<string, object>? payload);
}

public class DefaultPayloadMapper : IPayloadMapper
{
    public IDictionary<string, object> Map(IDictionary<string, object>? payload)
    {
        if (payload == null)
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, object>(payload, StringComparer.OrdinalIgnoreCase);
    }
}
