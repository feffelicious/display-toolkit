namespace DisplayToolkit.Core;

/// <summary>
/// VCP (Virtual Control Panel) codes. Standard codes are from VESA MCCS 2.2; the ASUS ones are documented in
/// <c>docs/research/asus-ddc-protocol.md</c>. This is the only place raw codes appear.
/// </summary>
public static class Vcp
{
    // Standard MCCS
    public const byte RestoreFactoryDefaults = 0x04;
    public const byte RestoreColorDefaults = 0x08;
    public const byte Brightness = 0x10;
    public const byte Contrast = 0x12;
    public const byte ColorPreset = 0x14;
    public const byte RedGain = 0x16;
    public const byte GreenGain = 0x18;
    public const byte BlueGain = 0x1A;
    public const byte ActiveControl = 0x52;
    public const byte SaturationRed = 0x59;
    public const byte SaturationYellow = 0x5A;
    public const byte SaturationGreen = 0x5B;
    public const byte SaturationCyan = 0x5C;
    public const byte SaturationBlue = 0x5D;
    public const byte SaturationMagenta = 0x5E;
    public const byte InputSource = 0x60;
    public const byte AudioVolume = 0x62;
    public const byte RedBlackLevel = 0x6C;
    public const byte GreenBlackLevel = 0x6E;
    public const byte BlueBlackLevel = 0x70;
    public const byte Gamma = 0x72;
    public const byte Sharpness = 0x87;
    public const byte Saturation = 0x8A;
    public const byte AudioMute = 0x8D;
    public const byte OsdLanguage = 0xCC;
    public const byte PowerMode = 0xD6;

    // ASUS
    public const byte AsusOledProtectionIntensity = 0x45;
    public const byte AsusProximitySensitivity = 0x47;
    public const byte AsusOledAntiFlicker = 0xC5;
    public const byte AsusGameVisual = 0xDC;
    public const byte AsusOverdrive = 0xE0;
    public const byte AsusPowerSaving = 0xE1;
    public const byte AsusHdrMode = 0xE2;
    public const byte AsusCrosshair = 0xE3;
    public const byte AsusGamePlusTimer = 0xE4;
    public const byte AsusShadowBoost = 0xE5;
    public const byte AsusBlueLightFilter = 0xE6;
    public const byte AsusDisplayAlignment = 0xE7;
    public const byte AsusGamePlusPosition = 0xE8;
    public const byte AsusFpsCounter = 0xEA;
    public const byte AsusEzOsd = 0xEB;

    /// <summary>Sharpening of upscaled (lower-resolution) input, 0 off to 3; ASUS calls it Smart Pixel.</summary>
    public const byte AsusUpscalingSharpness = 0xD1;

    /// <summary>Aura lighting on the back: effect in the low byte, color in the high byte.</summary>
    public const byte AsusAura = 0xF2;
    public const byte AsusResetMode = 0xEC;
    public const byte AsusProximitySensor = 0xED;
    public const byte AsusElmb = 0xEE;
    public const byte AsusVcpVersion = 0xEF;
    public const byte AsusPixelCleaningReminder = 0xF8;
    public const byte AsusScreenMove = 0xF9;
    public const byte AsusToggles1 = 0xFC;
    public const byte AsusToggles2 = 0xFD;
    public const byte AsusFirmware = 0xFF;
}
