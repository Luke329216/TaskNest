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

    // ⭐ Removed priority icons — now always blank
    public string Icon => Priority switch
    {
        TaskPriority.High => "🔥",
        TaskPriority.Medium => "⚡",
        TaskPriority.Low => "✅",
        _ => "•"
    };

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
