namespace DisplayToolkit.Core.Features;

/// <summary>
/// Every monitor setting Display Toolkit knows about. A monitor exposes the subset its capabilities string supports.
/// Values and semantics are documented and hardware-verified in <c>docs/research/asus-ddc-protocol.md</c>.
/// </summary>
public static class FeatureCatalog
{
    // Picture
    // Settings marked AvailableInHdr = false are greyed out by ASUS DisplayWidget Center in HDR, and brightness
    // writes were verified to have no effect in HDR on a PG32UCWM.
    public static readonly RangeFeature Brightness = new("brightness", "Brightness", Vcp.Brightness) { AvailableInHdr = false };
    public static readonly RangeFeature Contrast = new("contrast", "Contrast", Vcp.Contrast);
    public static readonly RangeFeature Sharpness = new("sharpness", "Sharpness", Vcp.Sharpness);
    public static readonly RangeFeature Saturation = new("saturation", "Saturation", Vcp.Saturation);

    public static readonly EnumFeature PictureMode = new("picture-mode", "Picture mode", Vcp.AsusGameVisual,
    [
        new(1, "Cinema"),
        new(2, "Scenery"),
        new(3, "sRGB"),
        new(4, "User"),
        new(5, "Racing"),
        new(6, "RTS/RPG"),
        new(7, "FPS"),
        new(8, "MOBA"),
        new(9, "Night vision"),
        new(10, "sRGB Cal"),
    ])
    {
        // In HDR the register reads 5 (Racing in SDR) regardless of the preset.
        AvailableInHdr = false,
    };

    // Group/preset pairs. HDR10 presets are hardware-verified; the Dolby Vision pairing (group 2 with presets 5–7)
    // follows the capabilities string and still needs verification with Dolby Vision content.
    public static readonly HdrModeFeature HdrMode = new("hdr-mode", "HDR mode", Vcp.AsusHdrMode,
    [
        new(0x0101, "Cinema HDR"),
        new(0x0102, "Gaming HDR"),
        new(0x0103, "Console HDR"),
        new(0x0104, "True Black 400"),
        new(0x0205, "Dolby Vision Bright"),
        new(0x0206, "Dolby Vision Dark"),
        new(0x0207, "Dolby Vision Gaming"),
    ])
    {
        Settling = WriteSettling.ModeSwitch,
    };

    // Color
    public static readonly EnumFeature ColorTemperature = new("color-temperature", "Color temperature", Vcp.ColorPreset,
    [
        new(3, "4000K"),
        new(4, "5000K"),
        new(5, "6500K"),
        new(6, "7500K"),
        new(7, "8200K"),
        new(8, "9300K"),
        new(9, "10000K"),
        new(11, "Custom"), // "User" in the monitor's menu, which clashes with the User picture mode.
    ]);

    public static readonly RangeFeature RedGain = new("red-gain", "Red", Vcp.RedGain);
    public static readonly RangeFeature GreenGain = new("green-gain", "Green", Vcp.GreenGain);
    public static readonly RangeFeature BlueGain = new("blue-gain", "Blue", Vcp.BlueGain);

    public static readonly EnumFeature Gamma = new("gamma", "Gamma", Vcp.Gamma,
    [
        new(0x5000, "1.8"),
        new(0x6400, "2.0"),
        new(0x7800, "2.2"),
        new(0x8C00, "2.4"),
        new(0xA000, "2.6"),
    ]);

    public static readonly RangeFeature SaturationRed = new("saturation-red", "Red saturation", Vcp.SaturationRed);
    public static readonly RangeFeature SaturationYellow = new("saturation-yellow", "Yellow saturation", Vcp.SaturationYellow);
    public static readonly RangeFeature SaturationGreen = new("saturation-green", "Green saturation", Vcp.SaturationGreen);
    public static readonly RangeFeature SaturationCyan = new("saturation-cyan", "Cyan saturation", Vcp.SaturationCyan);
    public static readonly RangeFeature SaturationBlue = new("saturation-blue", "Blue saturation", Vcp.SaturationBlue);
    public static readonly RangeFeature SaturationMagenta = new("saturation-magenta", "Magenta saturation", Vcp.SaturationMagenta);

    // Gaming and comfort
    public static readonly RangeFeature BlueLightFilter = new("blue-light", "Blue light filter", Vcp.AsusBlueLightFilter) { AvailableInHdr = false };
    public static readonly RangeFeature ShadowBoost = new("shadow-boost", "Shadow boost", Vcp.AsusShadowBoost) { AvailableInHdr = false };
    // Both change the signal timing, so Windows drops and re-detects the monitor (VRR verified on a PG32UCWM).
    public static readonly FlagFeature VariableRefreshRate = new("vrr", "Variable refresh rate", Vcp.AsusToggles2, bit: 0)
    {
        Settling = WriteSettling.ModeSwitch,
    };

    public static readonly FlagFeature FrameRateBoost = new("frame-rate-boost", "Frame-rate boost", Vcp.AsusToggles2, bit: 8)
    {
        Settling = WriteSettling.ModeSwitch,
    };
    public static readonly SwitchFeature Elmb = new("elmb", "ELMB", Vcp.AsusElmb);

    // GamePlus (overlays drawn by the monitor)
    public static readonly EnumFeature Crosshair = new("crosshair", "Crosshair", Vcp.AsusCrosshair,
    [
        new(0, "Off"),
        new(7, "Blue dot"),
        new(8, "Green dot"),
        new(9, "Blue mini duplex"),
        new(10, "Green mini duplex"),
        new(11, "Blue heavy duplex"),
        new(12, "Green heavy duplex"),
    ]);

    public static readonly EnumFeature FpsCounter = new("fps-counter", "FPS counter", Vcp.AsusFpsCounter,
    [
        new(0, "Off"),
        new(1, "Number"),
        new(2, "Bar"),
    ]);

    public static readonly EnumFeature Timer = new("timer", "Timer", Vcp.AsusGamePlusTimer,
    [
        new(0, "Off"),
        new(1, "30 s"),
        new(2, "40 s"),
        new(3, "50 s"),
        new(4, "60 s"),
        new(5, "90 s"),
    ]);

    public static readonly SwitchFeature DisplayAlignment = new("display-alignment", "Display alignment", Vcp.AsusDisplayAlignment);

    // OLED care
    public static readonly FlagFeature ScreenDimming = new("screen-dimming", "Screen dimming control", Vcp.AsusToggles2, bit: 3);
    public static readonly FlagFeature LogoDetection = new("logo-detection", "Logo detection", Vcp.AsusToggles2, bit: 5);
    public static readonly FlagFeature UniformBrightness = new("uniform-brightness", "Uniform brightness", Vcp.AsusToggles2, bit: 6);
    public static readonly FlagFeature TaskbarDetection = new("taskbar-detection", "Taskbar detection", Vcp.AsusToggles2, bit: 11);
    public static readonly FlagFeature BoundaryDetection = new("boundary-detection", "Boundary detection", Vcp.AsusToggles2, bit: 12);
    public static readonly FlagFeature OuterDimming = new("outer-dimming", "Outer dimming control", Vcp.AsusToggles2, bit: 13);
    public static readonly FlagFeature GlobalDimming = new("global-dimming", "Global dimming control", Vcp.AsusToggles2, bit: 14);

    /// <summary>Writing 1 starts pixel cleaning: the screen goes dark for about 6 minutes.</summary>
    public static readonly FlagFeature PixelCleaning = new("pixel-cleaning", "Pixel cleaning", Vcp.AsusToggles2, bit: 4)
    {
        Settling = WriteSettling.Unverified,
    };

    // Changes the panel timing: Windows drops and re-detects the monitor (verified on a PG32UCWM).
    public static readonly SwitchFeature OledAntiFlicker = new("oled-anti-flicker", "OLED anti-flicker", Vcp.AsusOledAntiFlicker)
    {
        Settling = WriteSettling.ModeSwitch,
    };

    public static readonly EnumFeature ScreenMove = new("screen-move", "Screen move", Vcp.AsusScreenMove,
    [
        new(0, "Off"),
        new(1, "Light"),
        new(2, "Middle"),
        new(3, "Strong"),
    ]);

    public static readonly EnumFeature PixelCleaningReminder = new("pixel-cleaning-reminder", "Pixel cleaning reminder", Vcp.AsusPixelCleaningReminder,
    [
        new(0, "Off"),
        new(2, "2 hours"),
        new(4, "4 hours"),
        new(8, "8 hours"),
    ]);

    public static readonly ByteFieldFeature ProximityDistance = new("proximity-distance", "Proximity sensor", Vcp.AsusProximitySensor, highByte: false,
    [
        new(0, "Off"),
        new(3, "Up to 60 cm"),
        new(2, "Up to 90 cm"),
        new(1, "Up to 120 cm"),
        new(0xFF, "Tailored") { NeedsConfirmation = true },
    ]);

    public static readonly ByteFieldFeature ProximityScreenOff = new("proximity-screen-off", "Turn screen off after", Vcp.AsusProximitySensor, highByte: true,
    [
        new(0, "Off"),
        new(5, "5 minutes"),
        new(10, "10 minutes"),
        new(15, "15 minutes"),
    ]);

    public static readonly RangeFeature ProximitySensitivity = new("proximity-sensitivity", "Proximity sensitivity", Vcp.AsusProximitySensitivity)
    {
        IsReadOnly = true,
    };

    // Input and sound
    public static readonly EnumFeature InputSource = new("input-source", "Input", Vcp.InputSource,
    [
        new(0x0F, "DisplayPort"),
        new(0x10, "DisplayPort 2"),
        new(0x11, "HDMI 1"),
        new(0x12, "HDMI 2"),
        new(0x13, "HDMI 3"),
        new(0x1A, "USB-C"),
        new(0x1B, "USB-C 2"),
    ]);

    public static readonly RangeFeature Volume = new("volume", "Volume", Vcp.AudioVolume);
    public static readonly SwitchFeature Mute = new("mute", "Mute", Vcp.AudioMute, offValue: 2, onValue: 1);

    // Monitor (system setup)
    public static readonly FlagFeature PowerIndicator = new("power-indicator", "Power indicator", Vcp.AsusToggles1, bit: 0);
    public static readonly FlagFeature PowerKeyLock = new("power-key-lock", "Power key lock", Vcp.AsusToggles1, bit: 1);
    public static readonly FlagFeature KeyLock = new("key-lock", "Key lock", Vcp.AsusToggles1, bit: 2);
    public static readonly FlagFeature InputAutoDetection = new("input-auto-detection", "Input auto-detection", Vcp.AsusToggles1, bit: 4);

    public static IReadOnlyList<Feature> All { get; } =
    [
        Brightness, Contrast, Sharpness, Saturation, PictureMode, HdrMode,
        ColorTemperature, RedGain, GreenGain, BlueGain, Gamma,
        SaturationRed, SaturationYellow, SaturationGreen, SaturationCyan, SaturationBlue, SaturationMagenta,
        BlueLightFilter, ShadowBoost, VariableRefreshRate, FrameRateBoost, Elmb,
        Crosshair, FpsCounter, Timer, DisplayAlignment,
        ScreenDimming, LogoDetection, UniformBrightness, TaskbarDetection, BoundaryDetection, OuterDimming, GlobalDimming,
        PixelCleaning, OledAntiFlicker, ScreenMove, PixelCleaningReminder,
        ProximityDistance, ProximityScreenOff, ProximitySensitivity,
        InputSource, Volume, Mute,
        PowerIndicator, PowerKeyLock, KeyLock, InputAutoDetection,
    ];

    public static Feature? Find(string id) => All.FirstOrDefault(feature => feature.Id == id);
}
