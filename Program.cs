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
        private static List<TodoTask> tasks = new();
        private const string SaveFile = "tasks.json";

        static void Main()
        {
            LoadTasks();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== TaskNest ===");
                Console.WriteLine("1. View Tasks");
                Console.WriteLine("2. Add Task");
                Console.WriteLine("3. Complete Task");
                Console.WriteLine("4. Edit Task");
                Console.WriteLine("5. Delete Task");
                Console.WriteLine("6. Exit");

                Console.Write("\nSelect an option: ");
                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ViewTasks();
                        break;
                    case "2":
                        AddTask();
                        break;
                    case "3":
                        CompleteTask();
                        break;
                    case "4":
                        EditTask();
                        break;
                    case "5":
                        DeleteTask();
                        break;
                    case "6":
                        SaveTasks();
                        return;
                }
            }
        }

        static void AddTask()
        {
            Console.Write("Task title: ");
            string title = Console.ReadLine() ?? "";

            Console.Write("Category: ");
            string category = Console.ReadLine() ?? "General";

            Console.Write("Priority (Low, Medium, High): ");
            Enum.TryParse(Console.ReadLine(), true, out PriorityLevel priority);

            tasks.Add(new TodoTask
            {
                Title = title,
                Category = category,
                Priority = priority
            });

            SaveTasks();
        }

        static void ViewTasks()
        {
            Console.Clear();

            Console.WriteLine("Filter:");
            Console.WriteLine("1. All");
            Console.WriteLine("2. Active");
            Console.WriteLine("3. Completed");

            string? filter = Console.ReadLine();

            IEnumerable<TodoTask> filtered = tasks;

            switch (filter)
            {
                case "2":
                    filtered = tasks.Where(t => !t.IsCompleted);
                    break;
                case "3":
                    filtered = tasks.Where(t => t.IsCompleted);
                    break;
            }

            filtered = filtered.OrderByDescending(t => t.Priority);

            int i = 1;
            foreach (var task in filtered)
            {
                Console.WriteLine(
                    $"{i++}. [{(task.IsCompleted ? "X" : " ")}] {task.Title} | {task.Category} | {task.Priority}");
            }

            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }

        static void CompleteTask()
        {
            ShowAllTasks();

            Console.Write("Task number to complete: ");
            if (int.TryParse(Console.ReadLine(), out int index)
                && index > 0 && index <= tasks.Count)
            {
                tasks[index - 1].IsCompleted = true;
                SaveTasks();
            }
        }

        static void EditTask()
        {
            ShowAllTasks();

            Console.Write("Task number to edit: ");
            if (int.TryParse(Console.ReadLine(), out int index)
                && index > 0 && index <= tasks.Count)
            {
                var task = tasks[index - 1];

                Console.Write("New title: ");
                task.Title = Console.ReadLine() ?? task.Title;

                Console.Write("New category: ");
                task.Category = Console.ReadLine() ?? task.Category;

                Console.Write("New priority (Low, Medium, High): ");
                if (Enum.TryParse(Console.ReadLine(), true, out PriorityLevel priority))
                    task.Priority = priority;

                SaveTasks();
            }
        }

        static void DeleteTask()
        {
            ShowAllTasks();

            Console.Write("Task number to delete: ");
            if (int.TryParse(Console.ReadLine(), out int index)
                && index > 0 && index <= tasks.Count)
            {
                tasks.RemoveAt(index - 1);
                SaveTasks();
            }
        }

        static void ShowAllTasks()
        {
            Console.Clear();

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                Console.WriteLine(
                    $"{i + 1}. [{(task.IsCompleted ? "X" : " ")}] {task.Title}");
            }
        }

        static void SaveTasks()
        {
            File.WriteAllText(
                SaveFile,
                JsonSerializer.Serialize(tasks, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
        }

        static void LoadTasks()
        {
            if (File.Exists(SaveFile))
            {
                tasks = JsonSerializer.Deserialize<List<TodoTask>>
                    (File.ReadAllText(SaveFile)) ?? new();
            }
        }
    }
}
