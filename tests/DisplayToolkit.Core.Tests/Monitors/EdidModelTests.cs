using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.Core.Tests.Monitors;

public sealed class EdidModelTests
{
    [Theory]
    [InlineData((ushort)0xB306, (ushort)0x32B1, "AUS32B1")] // ASUS: EDID bytes 06 B3, byte-swapped by the CCD API
    [InlineData((ushort)0xAC10, (ushort)0xA0C2, "DELA0C2")] // Dell: EDID bytes 10 AC
    public void Formats_pnp_model(ushort manufacturerId, ushort productCode, string expected)
    {
        Assert.Equal(expected, Win32MonitorEnumerator.EdidModel(manufacturerId, productCode));
    }
}
