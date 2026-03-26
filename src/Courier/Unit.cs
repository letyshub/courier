namespace Courier;

/// <summary>
/// Represents the absence of a meaningful return value.
/// Used as the result type for commands that do not produce output.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>The single value of the <see cref="Unit"/> type.</summary>
    public static readonly Unit Value = new();

    /// <summary>Returns a completed task that yields <see cref="Value"/>.</summary>
    public static Task<Unit> Task => System.Threading.Tasks.Task.FromResult(Value);

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
