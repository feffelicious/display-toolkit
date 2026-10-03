using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Tests.Fakes;

namespace DisplayToolkit.Core.Tests.Capabilities;

public sealed class CapabilitiesParserTests
{
    [Fact]
    public void Parses_model_and_mccs_version()
    {
        var capabilities = CapabilitiesParser.Parse(SimulatedMonitor.Pg32ucwmCapabilities);

        Assert.Equal("PG32UCWM", capabilities.Model);
        Assert.Equal("2.2", capabilities.MccsVersion);
    }

    [Fact]
    public void Parses_codes_with_and_without_value_lists()
    {
        var capabilities = CapabilitiesParser.Parse(SimulatedMonitor.Pg32ucwmCapabilities);

        Assert.True(capabilities.Supports(0x10));
        Assert.Empty(capabilities.ValuesOf(0x10));
        Assert.Equal([0x03u, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0B], capabilities.ValuesOf(0x14));
        Assert.True(capabilities.Supports(0xFF));
        Assert.False(capabilities.Supports(0x9B));
    }

    [Fact]
    public void Keeps_four_digit_values_as_written()
    {
        var capabilities = CapabilitiesParser.Parse(SimulatedMonitor.Pg32ucwmCapabilities);

        Assert.Equal([0x5000u, 0x6400, 0x7800, 0x8C00, 0xA000], capabilities.ValuesOf(0x72));
        Assert.Equal([0x6979u], capabilities.ValuesOf(0xFD));
        Assert.Equal([0x0500u, 0x0A00, 0x0F00, 0x00, 0x01, 0x02, 0x03, 0xFF], capabilities.ValuesOf(0xED));
    }

    [Fact]
    public void Does_not_mistake_other_sections_for_vcp()
    {
        var capabilities = CapabilitiesParser.Parse("(prot(monitor)cmds(01 02 03)vcp(10 12)vcpname(10(Brightness)))");

        Assert.Equal([0x10, 0x12], capabilities.VcpCodes);
    }

    [Theory]
    [InlineData("prot(monitor)vcp(10 12 14(05 08))mccs_ver(2.1)")] // no outer parentheses
    [InlineData("(prot(monitor)vcp(101214(05 08))mccs_ver(2.1))")] // codes without spaces
    [InlineData("(prot(monitor)vcp(10 12 14(05 08")] // truncated reply
    public void Tolerates_sloppy_strings(string raw)
    {
        var capabilities = CapabilitiesParser.Parse(raw);

        Assert.Equal([0x10, 0x12, 0x14], capabilities.VcpCodes);
        Assert.Equal([0x05u, 0x08], capabilities.ValuesOf(0x14));
    }

    [Fact]
    public void Splits_long_value_runs_into_bytes()
    {
        // Four digits can't be split (ASUS uses real 4-digit values such as 0500), but longer runs can.
        var capabilities = CapabilitiesParser.Parse("(vcp(14(030405) ED(0500)))");

        Assert.Equal([0x03u, 0x04, 0x05], capabilities.ValuesOf(0x14));
        Assert.Equal([0x0500u], capabilities.ValuesOf(0xED));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    public void Returns_empty_capabilities_for_unusable_input(string? raw)
    {
        Assert.Empty(CapabilitiesParser.Parse(raw).VcpCodes);
    }
}
