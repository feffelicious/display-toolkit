using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Tests.Fakes;

namespace DisplayToolkit.Core.Tests.Features;

public sealed class FeatureCatalogTests
{
    private static readonly MonitorCapabilities Pg32ucwm = CapabilitiesParser.Parse(SimulatedMonitor.Pg32ucwmCapabilities);

    [Fact]
    public void Feature_ids_are_unique()
    {
        var ids = FeatureCatalog.All.Select(feature => feature.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Every_feature_is_listed_in_All()
    {
        var declared = typeof(FeatureCatalog).GetFields()
            .Where(field => field.IsStatic && typeof(Feature).IsAssignableFrom(field.FieldType))
            .Select(field => (Feature)field.GetValue(null)!);

        Assert.Equal(declared.ToHashSet(), FeatureCatalog.All.ToHashSet());
    }

    [Theory]
    [InlineData("brightness")]
    [InlineData("picture-mode")]
    [InlineData("hdr-mode")]
    [InlineData("vrr")]
    [InlineData("frame-rate-boost")]
    [InlineData("taskbar-detection")]
    [InlineData("global-dimming")]
    [InlineData("pixel-cleaning")]
    [InlineData("proximity-distance")]
    [InlineData("key-lock")]
    [InlineData("input-auto-detection")]
    public void Pg32ucwm_supports(string id)
    {
        Assert.True(FeatureCatalog.Find(id)!.IsSupportedBy(Pg32ucwm));
    }

    [Fact]
    public void Flag_support_follows_the_supported_bits_mask()
    {
        // Bit 12 (boundary detection) is missing from the PG32UCWM's 0xFD mask 0x6979.
        Assert.False(FeatureCatalog.BoundaryDetection.IsSupportedBy(Pg32ucwm));
        Assert.True(FeatureCatalog.OuterDimming.IsSupportedBy(Pg32ucwm));
    }

    [Fact]
    public void Picture_modes_follow_the_advertised_values()
    {
        var names = FeatureCatalog.PictureMode.SupportedOptions(Pg32ucwm).Select(option => option.Name);

        // The PG32UCWM advertises sRGB Cal (10) instead of plain sRGB (3).
        Assert.Equal(["Cinema", "Scenery", "User", "Racing", "RTS/RPG", "FPS", "MOBA", "Night vision", "sRGB Cal"], names);
    }

    [Fact]
    public void Hdr_presets_combine_advertised_groups_and_presets()
    {
        var values = FeatureCatalog.HdrMode.SupportedOptions(Pg32ucwm).Select(option => option.Value);

        Assert.Equal([0x0101u, 0x0102, 0x0103, 0x0104, 0x0205, 0x0206, 0x0207], values);
    }

    [Fact]
    public void Proximity_fields_split_the_register()
    {
        Assert.Equal([0u, 3, 2, 1, 0xFF], FeatureCatalog.ProximityDistance.SupportedOptions(Pg32ucwm).Select(option => option.Value));
        Assert.Equal([0u, 5, 10, 15], FeatureCatalog.ProximityScreenOff.SupportedOptions(Pg32ucwm).Select(option => option.Value));

        var reply = new VcpReply(0x0501, 0x0FFF);
        Assert.Equal(1u, FeatureCatalog.ProximityDistance.Decode(reply));
        Assert.Equal(5u, FeatureCatalog.ProximityScreenOff.Decode(reply));
        Assert.Equal(0x0A01u, FeatureCatalog.ProximityScreenOff.Encode(10, 0x0501));
        Assert.Equal(0x0503u, FeatureCatalog.ProximityDistance.Encode(3, 0x0501));
    }

    [Fact]
    public void Flags_decode_the_measured_oled_register()
    {
        var reply = new VcpReply(0x6829, 0x6979);

        Assert.Equal(1u, FeatureCatalog.VariableRefreshRate.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.ScreenDimming.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.LogoDetection.Decode(reply));
        Assert.Equal(0u, FeatureCatalog.UniformBrightness.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.TaskbarDetection.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.OuterDimming.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.GlobalDimming.Decode(reply));
        Assert.Equal(0u, FeatureCatalog.PixelCleaning.Decode(reply));
    }

    [Fact]
    public void Flag_encoding_changes_only_its_bit()
    {
        Assert.Equal(0x6869u, FeatureCatalog.UniformBrightness.Encode(1, 0x6829));
        Assert.Equal(0x6829u, FeatureCatalog.UniformBrightness.Encode(0, 0x6869));
    }

    [Fact]
    public void Range_features_ignore_the_high_word()
    {
        // ASUS puts the factory default in the high word of the RGB gain registers.
        var reply = new VcpReply(0x0064_0032, 0x0064);

        Assert.Equal(0x32u, FeatureCatalog.RedGain.Decode(reply));
    }

    [Fact]
    public void Locked_replies_are_recognised()
    {
        Assert.True(new VcpReply(0xFE, 0xFE).IsLocked);
        Assert.False(new VcpReply(0xFE, 0xFF).IsLocked);
    }
}
