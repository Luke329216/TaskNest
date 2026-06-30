using System;

namespace TaskNestUI;

public enum TaskPriority
{
    None,
    Low,
    Medium,
    High
}

public class TodoTask
{
    public string Text { get; set; } = "";
    public bool IsCompleted { get; set; } = false;
    public TaskPriority Priority { get; set; } = TaskPriority.None;

    // Due Date
    public DateTime? DueDate { get; set; }
}
