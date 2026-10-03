namespace DisplayToolkit.Automation.Profiles;

/// <summary>Modifier keys, with the values <c>RegisterHotKey</c> uses.</summary>
[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
}

/// <summary>A system-wide key combination: modifiers plus one Win32 virtual-key code.</summary>
public sealed record Shortcut(ShortcutModifiers Modifiers, int VirtualKey);
