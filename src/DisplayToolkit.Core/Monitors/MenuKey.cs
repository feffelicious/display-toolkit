namespace DisplayToolkit.Core.Monitors;

/// <summary>
/// The monitor's own menu keys, as ASUS EZ-OSD (<c>0xEB</c>) values. Verified on the PG32UCWM. Some other values act
/// at once (one starts the pixel-cleaning prompt, one toggles frame-rate boost), so only these are offered.
/// </summary>
public enum MenuKey
{
    Close = 0,
    Open = 1,
    Up = 2,
    Down = 3,
    Right = 4,
    Left = 5,
    Enter = 6,
    Back = 7,
}
