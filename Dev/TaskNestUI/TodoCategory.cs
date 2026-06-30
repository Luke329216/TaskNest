using System.Collections.ObjectModel;

namespace TaskNestUI;

public class TodoCategory
{
    public string Name { get; set; } = "";

    public ObservableCollection<TodoTask> Tasks { get; set; } = new();
    public ObservableCollection<TodoTask> CompletedTasks { get; set; } = new();
}
