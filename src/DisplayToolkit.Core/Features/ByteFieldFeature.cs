using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Features;

/// <summary>
/// A choice stored in one byte of a register that packs two settings, like the Neo proximity sensor
/// (<c>0xED</c> = screen-off minutes in the high byte, distance in the low byte).
/// </summary>
/// <remarks>
/// The capabilities string advertises high-byte choices as 4-digit values (<c>0500</c>) and low-byte choices as
/// 2-digit values (<c>03</c>), which is how <see cref="SupportedOptions"/> tells them apart.
/// </remarks>
public sealed class ByteFieldFeature(string id, string name, byte code, bool highByte, IReadOnlyList<FeatureOption> options)
    : EnumFeature(id, name, code, options)
{
    private int Shift => highByte ? 8 : 0;

    public override bool IsPartialRegister => true;

    public override IReadOnlyList<FeatureOption> SupportedOptions(MonitorCapabilities capabilities)
    {
        var advertised = capabilities.ValuesOf(Code)
            .Where(value => highByte ? value > 0xFF : value <= 0xFF)
            .Select(value => (value >> Shift) & 0xFF)
            .ToHashSet();

        // "Off" (0) is implied for the high byte: the monitor only lists the real choices there.
        return [.. Options.Where(option => advertised.Contains(option.Value) || (highByte && option.Value == 0))];
    }

    public override uint Decode(VcpReply reply) => (reply.Current >> Shift) & 0xFF;

    public override uint DecodeMaximum(VcpReply reply) => (reply.Maximum >> Shift) & 0xFF;

    public override uint Encode(uint value, uint register) =>
        (register & ~(0xFFu << Shift)) | ((value & 0xFF) << Shift);
}
