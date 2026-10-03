using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Features;

/// <summary>A continuous setting from 0 to the monitor-reported maximum, such as brightness.</summary>
/// <remarks>
/// Only the low word is the value. Some ASUS registers put extra data in the high word (factory default for RGB gains,
/// the "physical" value for six-axis saturation).
/// </remarks>
public sealed class RangeFeature(string id, string name, byte code) : Feature(id, name, code)
{
    public override bool IsSupportedBy(MonitorCapabilities capabilities) => capabilities.Supports(Code);

    public override uint Decode(VcpReply reply) => reply.Current & 0xFFFF;

    public override uint DecodeMaximum(VcpReply reply) => reply.Maximum & 0xFFFF;
}
