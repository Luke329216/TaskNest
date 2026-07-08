using System.Text.Json;

namespace TaskNest
{
    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }

    public class TodoTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Category { get; set; } = "General";
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
        public bool IsCompleted { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Launch the Avalonia UI from the TaskNestUI project.
            TaskNestUI.Program.Main(args);
        }
    }
}
