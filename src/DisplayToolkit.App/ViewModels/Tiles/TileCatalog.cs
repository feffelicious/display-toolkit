using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>What a tile is built from: the monitor, a way to open sub-pages, and the app's own features.</summary>
public sealed record TileContext(MonitorViewModel Monitor, Action<PageViewModel> Navigate, TargetMode TargetMode);

/// <summary>A tile that can appear in the flyout: how to name it in the Add list and how to build it for a monitor.</summary>
/// <param name="IsSupported">Whether the monitor has what the tile needs. Cheap: builds nothing.</param>
/// <param name="Build">Builds the tile. Only call when <paramref name="IsSupported"/> is true.</param>
public sealed record TileDefinition(
    string Id,
    string Name,
    string Group,
    string KindLabel,
    Func<MonitorViewModel, bool> IsSupported,
    Func<TileContext, TileViewModel> Build)
{
    /// <summary>Builds the tile, or returns null if the monitor doesn't support it.</summary>
    public TileViewModel? Create(TileContext context) => IsSupported(context.Monitor) ? Build(context) : null;
}

/// <summary>Every flyout tile (design spec §4.4). Ids are stored in the layout file: never rename one.</summary>
public static class TileCatalog
{
    /// <summary>The layout a monitor starts with (design spec §13). Unsupported tiles are skipped.</summary>
    public static IReadOnlyList<string> DefaultLayout { get; } =
        ["picture-mode", "hdr", "blue-light", "shadow-boost", "crosshair", "oled-anti-flicker"];

    public static IReadOnlyList<TileDefinition> All { get; } =
    [
        new("picture-mode", "Picture mode", "Picture", "List",
            monitor => monitor[FeatureCatalog.PictureMode] is not null || monitor[FeatureCatalog.HdrMode] is not null,
            context => new PictureModeTileViewModel(context.Monitor, context.Navigate)),
        new("hdr", "HDR", "Picture", "Toggle",
            monitor => monitor.WindowsHdr?.IsSupported == true,
            context => new HdrTileViewModel(context.Monitor, context.Navigate)),
        Picker("color-temperature", "", FeatureCatalog.ColorTemperature, "Picture"),

        Levels("blue-light", "", FeatureCatalog.BlueLightFilter, "Gaming and comfort", defaultLevel: 2, "Blue light",
            note: "Level 4 matches TÜV low blue light."),
        Levels("shadow-boost", "", FeatureCatalog.ShadowBoost, "Gaming and comfort", defaultLevel: 1),
        Switch("vrr", "", FeatureCatalog.VariableRefreshRate, "Gaming and comfort", "Variable refresh"),
        Switch("frame-rate-boost", "", FeatureCatalog.FrameRateBoost, "Gaming and comfort"),

        Styles("crosshair", "", FeatureCatalog.Crosshair, "GamePlus"),
        Picker("fps-counter", "", FeatureCatalog.FpsCounter, "GamePlus"),
        Picker("timer", "", FeatureCatalog.Timer, "GamePlus"),
        Switch("display-alignment", "", FeatureCatalog.DisplayAlignment, "GamePlus", "Alignment"),
        new("target-mode", "Target mode", "GamePlus", "Toggle", _ => true, context => new TargetModeTileViewModel(context.TargetMode)),

        Switch("oled-anti-flicker", "", FeatureCatalog.OledAntiFlicker, "OLED care", "Anti-flicker"),
        Switch("uniform-brightness", "", FeatureCatalog.UniformBrightness, "OLED care"),
        Styles("screen-move", "", FeatureCatalog.ScreenMove, "OLED care"),

        Picker("aura", "\uEA80", FeatureCatalog.AuraEffect, "Lighting", "Aura"),

        Picker("input-source", "", FeatureCatalog.InputSource, "Sound and input"),
        Switch("mute", "", FeatureCatalog.Mute, "Sound and input"),
    ];

    public static TileDefinition? Find(string id) => All.FirstOrDefault(definition => definition.Id == id);

    private static TileDefinition Switch(string id, string glyph, Feature feature, string group, string? name = null) =>
        new(id, name ?? feature.Name, group, "Toggle", monitor => monitor[feature] is not null,
            context => new SwitchTileViewModel(id, glyph, context.Monitor[feature]!, name));

    private static TileDefinition Picker(string id, string glyph, EnumFeature feature, string group, string? name = null) =>
        new(id, name ?? feature.Name, group, "List", monitor => monitor[feature] is not null,
            context => new PickerTileViewModel(id, glyph, context.Monitor[feature]!, context.Monitor.OptionsOf(feature), context.Navigate, name));

    /// <summary>Off or a level from 1 to the monitor's maximum.</summary>
    private static TileDefinition Levels(
        string id, string glyph, RangeFeature feature, string group, uint defaultLevel, string? name = null, string? note = null) =>
        new(id, name ?? feature.Name, group, "Levels", monitor => monitor[feature] is not null, context =>
        {
            var state = context.Monitor[feature]!;
            return new SplitTileViewModel(id, glyph, state, defaultLevel, () => new LevelsPageViewModel(state, note), context.Navigate, name);
        });

    /// <summary>Off or one of several named styles (option value 0 means off).</summary>
    private static TileDefinition Styles(string id, string glyph, EnumFeature feature, string group, string? name = null) =>
        new(id, name ?? feature.Name, group, "Styles", monitor => monitor[feature] is not null, context =>
        {
            var state = context.Monitor[feature]!;
            var options = context.Monitor.OptionsOf(feature);
            var firstOn = options.FirstOrDefault(option => option.Value != 0)?.Value ?? 1;
            return new SplitTileViewModel(id, glyph, state, firstOn, () => new OptionsPageViewModel(name ?? feature.Name, state, options), context.Navigate, name);
        });
}
