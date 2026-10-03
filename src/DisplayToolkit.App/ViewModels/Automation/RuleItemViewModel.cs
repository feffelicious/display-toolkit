using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.Automation.Rules;
using DisplayToolkit.Automation.Sun;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>A rule in the rules list: a sentence, when it next runs, its profile chip and an on/off switch.</summary>
internal sealed partial class RuleItemViewModel : ObservableObject
{
    private readonly AutomationService _automation;
    private readonly Action<Rule> _edit;

    public RuleItemViewModel(Rule rule, int index, AutomationService automation, GeoCoordinate? location, Action<Rule> edit)
    {
        Rule = rule;
        Index = index;
        _automation = automation;
        _edit = edit;
        Title = AutomationText.Title(rule.Trigger);
        Description = AutomationText.Description(rule, location);
        Glyph = GlyphOf(rule.Trigger);
        var profile = automation.Automation.FindProfile(rule.ProfileId);
        ProfileName = profile?.Name ?? "Deleted profile";
        ProfileGlyph = profile?.Glyph ?? "";
    }

    public Rule Rule { get; }

    public int Index { get; }

    public string Title { get; }

    public string Description { get; }

    public string Glyph { get; }

    public string ProfileName { get; }

    public string ProfileGlyph { get; }

    public bool CanMoveUp => Index > 0;

    public bool CanMoveDown => Index < _automation.Rules.Count - 1;

    public bool IsEnabled
    {
        get => Rule.IsEnabled;
        set => _automation.SaveRule(Rule with { IsEnabled = value });
    }

    public static string GlyphOf(Trigger trigger) => trigger switch
    {
        AppTrigger => "",
        FullscreenGameTrigger => "",
        TimeTrigger => "",
        SunTrigger { Event: SunEvent.Sunrise } => "",
        SunTrigger => "",
        PowerTrigger => "",
        _ => "",
    };

    [RelayCommand]
    private void Edit() => _edit(Rule);

    [RelayCommand]
    private void Duplicate() => _automation.SaveRule(Rule with { Id = Guid.NewGuid() });

    [RelayCommand]
    private void Delete() => _automation.DeleteRule(Rule.Id);

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => _automation.MoveRule(Rule.Id, Index - 1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => _automation.MoveRule(Rule.Id, Index + 1);
}
