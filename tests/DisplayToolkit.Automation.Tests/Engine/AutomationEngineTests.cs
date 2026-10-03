using DisplayToolkit.Automation.Engine;
using DisplayToolkit.Automation.Rules;
using DisplayToolkit.Automation.Sun;
using DisplayToolkit.Automation.Tests.Fakes;

namespace DisplayToolkit.Automation.Tests.Engine;

public sealed class AutomationEngineTests
{
    private static readonly Guid Desktop = Guid.NewGuid();
    private static readonly Guid Night = Guid.NewGuid();
    private static readonly Guid Gaming = Guid.NewGuid();
    private static readonly Guid Movie = Guid.NewGuid();
    private static readonly GeoCoordinate Stockholm = new(59.3293, 18.0686);

    private static readonly Rule MorningDesktop = Schedule(new TimeTrigger(new TimeOnly(7, 0), Days.Every), Desktop);
    private static readonly Rule EveningNight = Schedule(new TimeTrigger(new TimeOnly(20, 0), Days.Every), Night);
    private static readonly Rule Games = Condition(new FullscreenGameTrigger(), Gaming);
    private static readonly Rule Vlc = Condition(new AppTrigger("vlc", "VLC media player"), Movie);

    // A Wednesday.
    private readonly ManualTimeProvider _time = ManualTimeProvider.AtStockholm(new DateTime(2026, 10, 7, 12, 0, 0));
    private readonly AutomationEngine _engine;

    public AutomationEngineTests() => _engine = new AutomationEngine(_time);

    [Fact]
    public void Without_rules_nothing_is_chosen()
    {
        Configure();

        Assert.Equal(AutomationState.Idle, _engine.State);
        Assert.Null(_engine.NextWakeUp);
    }

    [Theory]
    [InlineData(12, 0, "desktop")]
    [InlineData(21, 0, "night")]
    [InlineData(3, 0, "night")] // Set yesterday evening.
    public void The_most_recent_schedule_rule_sets_the_baseline(int hour, int minute, string expected)
    {
        _time.SetLocal(new DateTime(2026, 10, 7, hour, minute, 0));

        Configure(MorningDesktop, EveningNight);

        Assert.Equal(AutomationReason.Schedule, _engine.State.Reason);
        Assert.Equal(expected == "desktop" ? Desktop : Night, _engine.State.ProfileId);
    }

    [Fact]
    public void A_schedule_rule_fires_when_its_time_comes()
    {
        _time.SetLocal(new DateTime(2026, 10, 7, 19, 59, 0));
        Configure(MorningDesktop, EveningNight);
        var changes = 0;
        _engine.StateChanged += (_, _) => changes++;

        Assert.Equal(At(20, 0), _engine.NextWakeUp);
        _time.Advance(TimeSpan.FromMinutes(1));
        _engine.Tick();

        Assert.Equal(Night, _engine.State.ProfileId);
        Assert.Equal(At(20, 0), _engine.State.Since);
        Assert.Equal(EveningNight, _engine.State.Rule);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Schedule_rules_only_fire_on_their_days()
    {
        var weekendMornings = Schedule(new TimeTrigger(new TimeOnly(9, 0), Days.Weekends), Movie);
        Configure(MorningDesktop, weekendMornings);

        // Wednesday noon: the weekend rule last fired on Sunday, before Wednesday's 07:00 rule.
        Assert.Equal(Desktop, _engine.State.ProfileId);

        _time.SetLocal(new DateTime(2026, 10, 10, 10, 0, 0)); // Saturday
        _engine.Tick();
        Assert.Equal(Movie, _engine.State.ProfileId);
    }

    [Fact]
    public void Conditions_win_over_the_schedule_and_hand_back_when_they_end()
    {
        Configure(Games, MorningDesktop, EveningNight);

        _engine.SetActiveConditions([Games.Id]);
        Assert.Equal(new AutomationState(Gaming, AutomationReason.Condition, Games, _time.GetLocalNow()), _engine.State);

        _engine.SetActiveConditions([]);
        Assert.Equal(Desktop, _engine.State.ProfileId);
    }

    [Fact]
    public void Higher_condition_rules_win()
    {
        Configure(Vlc, Games);

        _engine.SetActiveConditions([Games.Id, Vlc.Id]);

        Assert.Equal(Movie, _engine.State.ProfileId);
    }

    [Fact]
    public void A_manual_choice_holds_until_the_next_schedule_rule()
    {
        Configure(MorningDesktop, EveningNight);

        _engine.SetManual(Movie);
        _time.SetLocal(new DateTime(2026, 10, 7, 19, 0, 0));
        _engine.Tick();
        Assert.Equal(new AutomationState(Movie, AutomationReason.Manual, Since: At(12, 0)), _engine.State);

        _time.SetLocal(new DateTime(2026, 10, 7, 20, 0, 1));
        _engine.Tick();
        Assert.Equal(Night, _engine.State.ProfileId);
        Assert.Equal(AutomationReason.Schedule, _engine.State.Reason);
    }

    [Fact]
    public void A_manual_choice_ends_when_a_condition_starts_or_ends()
    {
        Configure(Games, MorningDesktop);
        _engine.SetActiveConditions([Games.Id]);

        _engine.SetManual(Night);
        _engine.SetActiveConditions([Games.Id]); // No change: still manual.
        Assert.Equal(Night, _engine.State.ProfileId);

        _engine.SetActiveConditions([]);
        Assert.Equal(Desktop, _engine.State.ProfileId);
    }

    [Fact]
    public void A_pause_chooses_nothing_and_ends_by_itself()
    {
        Configure(MorningDesktop);

        _engine.Pause(At(13, 0));
        Assert.Equal(new AutomationState(null, AutomationReason.Paused), _engine.State);
        Assert.Equal(At(13, 0), _engine.NextWakeUp);

        _time.SetLocal(new DateTime(2026, 10, 7, 13, 0, 0));
        _engine.Tick();
        Assert.False(_engine.IsPaused);
        Assert.Equal(Desktop, _engine.State.ProfileId);
    }

    [Fact]
    public void Turned_off_rules_and_rules_without_a_profile_are_ignored()
    {
        Configure(MorningDesktop with { IsEnabled = false }, Schedule(new TimeTrigger(new TimeOnly(8, 0), Days.Every), Guid.NewGuid()));

        Assert.Equal(AutomationState.Idle, _engine.State);
    }

    [Fact]
    public void Sun_rules_need_a_location()
    {
        var sunset = Schedule(new SunTrigger(SunEvent.Sunset, OffsetMinutes: -30, Days.Every), Night);
        _time.SetLocal(new DateTime(2026, 10, 7, 23, 0, 0));

        Configure(sunset);
        Assert.Equal(AutomationState.Idle, _engine.State);

        _engine.Configure([sunset], [Night], Stockholm);
        Assert.Equal(Night, _engine.State.ProfileId);
        // Sunset in Stockholm on 7 October 2026 is about 18:05 CEST.
        Assert.InRange(_engine.State.Since!.Value, At(17, 32), At(17, 38));
    }

    [Fact]
    public void Two_rules_at_the_same_moment_go_to_the_higher_one()
    {
        var sameTime = Schedule(new TimeTrigger(new TimeOnly(7, 0), Days.Every), Movie);

        Configure(sameTime, MorningDesktop);

        Assert.Equal(Movie, _engine.State.ProfileId);
    }

    [Fact]
    public void The_day_is_split_at_each_schedule_rule()
    {
        Configure(MorningDesktop, EveningNight);

        var day = _engine.GetDay(new DateOnly(2026, 10, 7));

        Assert.Equal(
            [
                new ScheduleSegment(At(0, 0), At(7, 0), Night, EveningNight),
                new ScheduleSegment(At(7, 0), At(20, 0), Desktop, MorningDesktop),
                new ScheduleSegment(At(20, 0), At(0, 0).AddDays(1), Night, EveningNight),
            ],
            day);
    }

    [Fact]
    public void A_time_skipped_by_daylight_saving_runs_an_hour_later()
    {
        // Clocks go from 02:00 to 03:00 on 29 March 2026 in Stockholm.
        var trigger = new TimeTrigger(new TimeOnly(2, 30), Days.Every);

        var occurrence = trigger.OccurrenceOn(new DateOnly(2026, 3, 29), ManualTimeProvider.Stockholm, location: null);

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 3, 30, 0, TimeSpan.FromHours(2)), occurrence);
    }

    private void Configure(params Rule[] rules) => _engine.Configure(rules, [Desktop, Night, Gaming, Movie], location: null);

    private DateTimeOffset At(int hour, int minute)
    {
        var local = new DateTime(2026, 10, 7, hour, minute, 0);
        return new DateTimeOffset(local, _time.LocalTimeZone.GetUtcOffset(local));
    }

    private static Rule Schedule(ScheduleTrigger trigger, Guid profile) => new() { Id = Guid.NewGuid(), ProfileId = profile, Trigger = trigger };

    private static Rule Condition(Trigger trigger, Guid profile) => new() { Id = Guid.NewGuid(), ProfileId = profile, Trigger = trigger };
}
