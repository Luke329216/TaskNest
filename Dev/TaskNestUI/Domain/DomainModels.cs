using System;
using System.Collections.Generic;

namespace TaskNestUI.Domain
{
    // Represents a single task in the app
    public class TaskItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Description { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Guid CategoryId { get; set; }

        public void MarkComplete()
        {
            IsCompleted = true;
        }

        public void MarkIncomplete()
        {
            IsCompleted = false;
        }
    }

    // Represents a category that holds tasks
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public List<TaskItem> Tasks { get; set; } = new();

        public void AddTask(TaskItem task)
        {
            task.CategoryId = Id;
            Tasks.Add(task);
        }

        public void RemoveTask(TaskItem task)
        {
            Tasks.Remove(task);
        }
    }

    // Stores user settings like theme, accent color, and text size
    public class UserSettings
    {
        public string BackgroundTheme { get; set; } = "Light";
        public string AccentColor { get; set; } = "Blue";
        public string TextSize { get; set; } = "Normal";

        public void ResetToDefault()
        {
            BackgroundTheme = "Light";
            AccentColor = "Blue";
            TextSize = "Normal";
        }
    }

    // Represents a note in the Notes tab
    public class NoteItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = "";
        public DateTime LastEdited { get; set; } = DateTime.Now;

        public void Update(string newContent)
        {
            Content = newContent;
            LastEdited = DateTime.Now;
        }
    }
}
