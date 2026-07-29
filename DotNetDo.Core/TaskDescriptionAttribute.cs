namespace DotNetDo;

/// <summary>Describes a task in task lists, help, and shell completion.</summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TaskDescriptionAttribute(string description) : Attribute
{
    /// <summary>The concise task description.</summary>
    public string Description { get; } = string.IsNullOrWhiteSpace(description)
        ? throw new ArgumentException("A task description must be non-empty.", nameof(description))
        : description;
}
