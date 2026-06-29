using System.Collections.ObjectModel;

namespace TaskNestUI;

public class TodoCategory
{
    public string Name { get; set; } = "";
    public bool IsExpanded { get; set; } = true;
    public ObservableCollection<TodoTask> Tasks { get; set; } = new();
}