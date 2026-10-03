using DisplayToolkit.Core.Features;

namespace DisplayToolkit.Automation.Profiles;

/// <summary>When a setting is written while a profile is applied. Earlier stages can reset what later stages set.</summary>
public enum ApplyStage
{
    /// <summary>Windows HDR: switching it changes which picture settings the monitor accepts.</summary>
    WindowsHdr,

    /// <summary>Picture mode or HDR mode: the monitor stores the remaining settings per mode.</summary>
    Mode,

    /// <summary>Color temperature: choosing one resets the RGB gains.</summary>
    ColorTemperature,

    Rest,
}

/// <summary>One setting a profile can include: a monitor feature, or Windows HDR (<see cref="Feature"/> is null).</summary>
public sealed record ProfileSetting(string Id, string Name, Feature? Feature, ApplyStage Stage, bool IsCommon);
