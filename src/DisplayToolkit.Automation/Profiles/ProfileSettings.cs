using DisplayToolkit.Core.Features;

namespace DisplayToolkit.Automation.Profiles;

/// <summary>
/// The settings a profile can include, in editor order (the common ones first). Panel care, the proximity sensor,
/// the input and the monitor's own setup stay out: they aren't things people switch with the time of day or an app.
/// </summary>
public static class ProfileSettings
{
    /// <summary>Windows "Use HDR". Stored as 1 (on) or 0 (off).</summary>
    public const string WindowsHdrId = "windows-hdr";

    public static ProfileSetting WindowsHdr { get; } = new(WindowsHdrId, "HDR", null, ApplyStage.WindowsHdr, IsCommon: true);

    public static IReadOnlyList<ProfileSetting> All { get; } =
    [
        Of(FeatureCatalog.Brightness, common: true),
        Of(FeatureCatalog.PictureMode, common: true, ApplyStage.Mode),
        Of(FeatureCatalog.ColorTemperature, common: true, ApplyStage.ColorTemperature),
        Of(FeatureCatalog.BlueLightFilter, common: true),
        Of(FeatureCatalog.Contrast, common: true),
        WindowsHdr,
        Of(FeatureCatalog.ShadowBoost, common: true),
        Of(FeatureCatalog.HdrMode, common: false, ApplyStage.Mode),
        Of(FeatureCatalog.RedGain),
        Of(FeatureCatalog.GreenGain),
        Of(FeatureCatalog.BlueGain),
        Of(FeatureCatalog.Gamma),
        Of(FeatureCatalog.Saturation),
        Of(FeatureCatalog.Sharpness),
        Of(FeatureCatalog.SaturationRed),
        Of(FeatureCatalog.SaturationYellow),
        Of(FeatureCatalog.SaturationGreen),
        Of(FeatureCatalog.SaturationCyan),
        Of(FeatureCatalog.SaturationBlue),
        Of(FeatureCatalog.SaturationMagenta),
        Of(FeatureCatalog.VariableRefreshRate),
        Of(FeatureCatalog.FrameRateBoost),
        Of(FeatureCatalog.Elmb),
        Of(FeatureCatalog.Crosshair),
        Of(FeatureCatalog.FpsCounter),
        Of(FeatureCatalog.Timer),
        Of(FeatureCatalog.DisplayAlignment),
        Of(FeatureCatalog.AuraEffect),
        Of(FeatureCatalog.AuraColor),
        Of(FeatureCatalog.Volume),
        Of(FeatureCatalog.Mute),
    ];

    public static ProfileSetting? Find(string id) => All.FirstOrDefault(setting => setting.Id == id);

    private static ProfileSetting Of(Feature feature, bool common = false, ApplyStage stage = ApplyStage.Rest) =>
        new(feature.Id, feature.Name, feature, stage, common);
}
