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

    // Priority icons intentionally removed; task label color indicates priority.
    public string Icon => string.Empty;

    public string DueIcon
    {
        get
        {
            if (!DueDate.HasValue)
                return "";

            if (DueDate.Value < DateTime.Today)
                return "⛔";   // overdue

            return "📅";       // normal due date
        }
    }
}
