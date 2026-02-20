using ElsaServer.Services;
using FluentAssertions;
using Xunit;

namespace ElsaServer.UnitTests.Services;

public class DefaultPayloadMapperTests
{
    [Fact]
    public void Map_Returns_Empty_Dictionary_When_Null()
    {
        var mapper = new DefaultPayloadMapper();

        var result = mapper.Map(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Map_Copies_With_OrdinalIgnoreCase()
    {
        var mapper = new DefaultPayloadMapper();
        var payload = new Dictionary<string, object> { { "Key", "value" } };

        var result = mapper.Map(payload);

        result.Should().ContainKey("key");
        result["KEY"].Should().Be("value");
    }
}
