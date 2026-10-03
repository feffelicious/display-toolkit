namespace DisplayToolkit.Automation.Rules;

/// <summary>"When <see cref="Trigger"/>, use <see cref="ProfileId"/>." Rules are kept in priority order.</summary>
/// <remarks>
/// Properties with defaults are settable rather than init-only: the JSON source generator assigns every init-only
/// property when reading, so a file written by an older version would replace the defaults with empty values.
/// </remarks>
public sealed record Rule
{
    public required Guid Id { get; init; }

    public required Guid ProfileId { get; init; }

    public required Trigger Trigger { get; init; }

    public bool IsEnabled { get; set; } = true;
}
