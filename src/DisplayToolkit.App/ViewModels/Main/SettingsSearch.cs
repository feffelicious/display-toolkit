using System.Globalization;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>A setting that "Find a setting" knows about.</summary>
/// <param name="Title">Exactly as the card (or section heading) shows it, so the page can point at it.</param>
/// <param name="Page">The navigation pane title of the page it's on.</param>
/// <param name="Keywords">Other words people may type for it.</param>
/// <param name="Feature">The monitor feature it needs; it's left out for monitors without it.</param>
/// <param name="NeedsMonitor">Only on a monitor page (no feature of its own, but the page needs a monitor).</param>
/// <param name="Card">The card to point at, when the setting is part of another card; otherwise <paramref name="Title"/>.</param>
/// <param name="SubPage">The sub-page it's on, such as <see cref="SettingsSearch.SixAxis"/>; null for the page itself.</param>
public sealed record SearchEntry(
    string Title, string Page, string Keywords = "", Feature? Feature = null, bool NeedsMonitor = false, string? Card = null,
    string? SubPage = null);

/// <summary>
/// The search box in the main window. Every entry names a card on a page; choosing one opens the page and points at
/// the card. Titles must match the pages: when one is renamed there, rename it here too.
/// </summary>
public static class SettingsSearch
{
    private const string Display = "Display";
    private const string Automation = "Profiles & automation";
    private const string OledCare = "OLED care";
    private const string GamePlus = "GamePlus";
    private const string Settings = "Settings";

    /// <summary>The Display page's six-axis color sub-page.</summary>
    public const string SixAxis = "six-axis";

    public static IReadOnlyList<SearchEntry> All { get; } =
    [
        new("Brightness", Display, "backlight dim", FeatureCatalog.Brightness),
        new("Contrast", Display, "", FeatureCatalog.Contrast),
        new("Picture mode", Display, "gamevisual preset mode", FeatureCatalog.PictureMode),
        new("Use HDR", Display, "high dynamic range windows hdr", NeedsMonitor: true),
        new("HDR mode", Display, "high dynamic range preset dolby vision", FeatureCatalog.HdrMode),
        new("Color temperature", Display, "warmth white point kelvin", FeatureCatalog.ColorTemperature),
        new("Red", Display, "rgb gain color", FeatureCatalog.RedGain),
        new("Green", Display, "rgb gain color", FeatureCatalog.GreenGain),
        new("Blue", Display, "rgb gain color", FeatureCatalog.BlueGain),
        new("Gamma", Display, "", FeatureCatalog.Gamma),
        new("Saturation", Display, "vibrance color", FeatureCatalog.Saturation),
        new("Sharpness", Display, "", FeatureCatalog.Sharpness),
        new("Six-axis color", Display, "hue saturation colors", FeatureCatalog.SaturationRed),
        new("Red saturation", Display, "six-axis color", FeatureCatalog.SaturationRed, Card: "Red", SubPage: SixAxis),
        new("Yellow saturation", Display, "six-axis color", FeatureCatalog.SaturationYellow, Card: "Yellow", SubPage: SixAxis),
        new("Green saturation", Display, "six-axis color", FeatureCatalog.SaturationGreen, Card: "Green", SubPage: SixAxis),
        new("Cyan saturation", Display, "six-axis color", FeatureCatalog.SaturationCyan, Card: "Cyan", SubPage: SixAxis),
        new("Blue saturation", Display, "six-axis color", FeatureCatalog.SaturationBlue, Card: "Blue", SubPage: SixAxis),
        new("Magenta saturation", Display, "six-axis color pink purple", FeatureCatalog.SaturationMagenta, Card: "Magenta", SubPage: SixAxis),
        new("Reset picture mode", Display, "factory defaults restore", FeatureCatalog.PictureMode, Card: "Picture mode"),
        new("Blue light filter", Display, "night eye care tüv low blue light", FeatureCatalog.BlueLightFilter),
        new("Shadow boost", Display, "dark black detail", FeatureCatalog.ShadowBoost),
        new("Variable refresh rate", Display, "vrr g-sync freesync adaptive-sync", FeatureCatalog.VariableRefreshRate),
        new("Frame-rate boost", Display, "dual mode resolution refresh", FeatureCatalog.FrameRateBoost),
        new("Aura lighting", Display, "light rgb led glow back effect", FeatureCatalog.AuraEffect),
        new("Aura color", Display, "light rgb led", FeatureCatalog.AuraColor, Card: "Aura lighting"),
        new("Volume", Display, "sound mute speaker headphones audio", FeatureCatalog.Volume),
        new("Monitor information", Display, "firmware model diagnostics capabilities about", NeedsMonitor: true),

        new("Profiles", Automation, "presets switch", NeedsMonitor: true),
        new("Automation", Automation, "rules schedule time app game power battery", NeedsMonitor: true),
        new("Follow the sun", Automation, "sunrise sunset day night warmth brightness", NeedsMonitor: true),

        new("Pixel cleaning", OledCare, "burn-in image retention refresh", FeatureCatalog.PixelCleaning),
        new("Screen dimming", OledCare, "burn-in static", FeatureCatalog.ScreenDimming),
        new("Logo detection", OledCare, "burn-in static", FeatureCatalog.LogoDetection),
        new("Taskbar detection", OledCare, "burn-in static", FeatureCatalog.TaskbarDetection),
        new("Boundary detection", OledCare, "burn-in static", FeatureCatalog.BoundaryDetection),
        new("Outer dimming", OledCare, "", FeatureCatalog.OuterDimming),
        new("Global dimming", OledCare, "abl", FeatureCatalog.GlobalDimming),
        new("Uniform brightness", OledCare, "abl", FeatureCatalog.UniformBrightness),
        new("Anti-flicker", OledCare, "flicker vrr", FeatureCatalog.OledAntiFlicker),
        new("Screen move", OledCare, "pixel shift orbit burn-in", FeatureCatalog.ScreenMove),
        new("Turn the screen off when I leave", OledCare, "proximity sensor presence away", FeatureCatalog.ProximityDistance),

        new("Crosshair", GamePlus, "aim reticle", FeatureCatalog.Crosshair),
        new("FPS counter", GamePlus, "frame rate refresh", FeatureCatalog.FpsCounter),
        new("Timer", GamePlus, "countdown", FeatureCatalog.Timer),
        new("Display alignment", GamePlus, "lines multiple monitors", FeatureCatalog.DisplayAlignment),
        new("Target mode", GamePlus, "focus dim spotlight", NeedsMonitor: true),

        new("Start with Windows", Settings, "startup login boot autostart"),
        new("App theme", Settings, "dark light mode"),
        new("Shortcuts", Settings, "hotkeys keyboard keys"),
        new("Open quick settings", Settings, "shortcut hotkey flyout"),
        new("Scroll over the tray icon", Settings, "mouse wheel brightness notification area"),
        new("Show changes from shortcuts", Settings, "overlay hud"),
        new("Back up settings", Settings, "export import backup restore move"),
        new("Location for sunrise and sunset", Settings, "latitude longitude gps where"),
        new("Power light", Settings, "led indicator", FeatureCatalog.PowerIndicator),
        new("Lock menu buttons", Settings, "key lock osd joystick", FeatureCatalog.KeyLock),
        new("Lock power button", Settings, "key lock", FeatureCatalog.PowerKeyLock),
        new("Monitor menu", Settings, "osd on-screen remote joystick buttons keys", NeedsMonitor: true),
        new("Find input automatically", Settings, "auto input detection source", FeatureCatalog.InputAutoDetection),
        new("Check for updates", Settings, "version new release download"),
        new("Log and settings folder", Settings, "logs bug report files"),
    ];

    /// <summary>
    /// The entries matching <paramref name="query"/>, best first: every word typed must start a word of the title or
    /// keywords. Titles that start with the query come first, then other title matches, then keyword matches.
    /// </summary>
    public static IReadOnlyList<SearchEntry> Find(string query, MonitorViewModel? monitor, int limit = 8)
    {
        var words = Words(query);
        if (words.Length == 0)
        {
            return [];
        }

        return [.. All
            .Where(entry => IsAvailable(entry, monitor))
            .Select(entry => (Entry: entry, Rank: Rank(entry, query.Trim(), words)))
            .Where(match => match.Rank >= 0)
            .OrderBy(match => match.Rank)
            .Select(match => match.Entry)
            .Take(limit)];
    }

    private static bool IsAvailable(SearchEntry entry, MonitorViewModel? monitor) =>
        entry.Feature is { } feature ? monitor?[feature] is not null
        : !entry.NeedsMonitor || monitor is not null;

    /// <summary>0: the title starts with the query; 1: all words in the title; 2: with the keywords; -1: no match.</summary>
    private static int Rank(SearchEntry entry, string query, string[] words)
    {
        if (entry.Title.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 0;
        }
        var titleWords = Words(entry.Title);
        if (words.All(word => titleWords.Any(title => title.StartsWith(word, StringComparison.Ordinal))))
        {
            return 1;
        }
        var allWords = titleWords.Concat(Words(entry.Keywords)).ToList();
        return words.All(word => allWords.Any(candidate => candidate.StartsWith(word, StringComparison.Ordinal))) ? 2 : -1;
    }

    private static string[] Words(string text) =>
        text.ToLower(CultureInfo.CurrentCulture).Split([' ', '-', ',', '&'], StringSplitOptions.RemoveEmptyEntries);
}
