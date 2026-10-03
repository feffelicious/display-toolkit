namespace DisplayToolkit.Automation.Rules;

/// <summary>"When <see cref="Trigger"/>, use <see cref="ProfileId"/>." Rules are kept in priority order.</summary>
public sealed record Rule
{
    public required Guid Id { get; init; }

    public required Guid ProfileId { get; init; }

    public required Trigger Trigger { get; init; }

    public bool IsEnabled { get; init; } = true;
}
