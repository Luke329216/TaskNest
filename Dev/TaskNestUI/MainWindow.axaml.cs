using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace TaskNestUI;

public partial class MainWindow : Window
{
    private ObservableCollection<TodoCategory> categories = new();
    private Stack<(TodoTask Task, TodoCategory Category, int Index)> deletedTasks = new();

    private string inlineAction = "";
    private TodoCategory? inlineCategory = null;
    private TextBox? inputToFocus = null;

    public MainWindow()
    {
        InitializeComponent();

        categories.Add(new TodoCategory { Name = "General" });

        SetupRightClick();
        BuildUI();
    }

    private void SetupRightClick()
    {
        var container = this.FindControl<Border>("CategoryContainer");

        if (container == null) return;

        var menu = new ContextMenu();

        var addCategory = new MenuItem { Header = "Add Category" };

        addCategory.Click += (_, _) =>
        {
            inlineAction = "AddCategory";
            inlineCategory = null;
            BuildUI();
        };

        menu.Items.Add(addCategory);

        container.ContextMenu = menu;
    }

    private void AddTaskToGeneral_Click(object? sender, RoutedEventArgs e)
    {
        var input = this.FindControl<TextBox>("TaskInput");

        if (string.IsNullOrWhiteSpace(input?.Text)) return;

        categories.First(c => c.Name == "General")
                  .Tasks.Add(new TodoTask { Text = input.Text.Trim() });

        input.Text = "";
        BuildUI();
    }

    private void TaskInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            AddTaskToGeneral_Click(sender, new RoutedEventArgs());
    }

    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        if (deletedTasks.Count == 0) return;

        var d = deletedTasks.Pop();
        d.Category.Tasks.Insert(d.Index, d.Task);

        BuildUI();
    }

    private void BuildUI()
    {
        var panel = this.FindControl<StackPanel>("CategoryPanel");
        if (panel == null) return;

        panel.Children.Clear();

        foreach (var category in categories)
        {
            var expander = new Expander
            {
                Header = $"{category.Name} ({category.Tasks.Count})",
                IsExpanded = true
            };

            expander.ContextMenu = BuildCategoryMenu(category);

            var mainStack = new StackPanel { Spacing = 8 };

            if (category.CompletedTasks.Count > 0)
            {
                var completedExpander = new Expander
                {
                    Header = $"Completed ({category.CompletedTasks.Count})",
                    IsExpanded = true
                };

                var completedStack = new StackPanel { Spacing = 5 };

                foreach (var task in category.CompletedTasks.ToList())
                {
                    completedStack.Children.Add(CreateCompletedRow(task, category));
                }

                completedExpander.Content = completedStack;
                mainStack.Children.Add(completedExpander);
            }

            var taskStack = new StackPanel { Spacing = 5 };

            if (inlineAction == "AddTask" && inlineCategory == category)
            {
                taskStack.Children.Add(CreateInlineInput(category));
            }

            foreach (var task in category.Tasks.ToList())
            {
                taskStack.Children.Add(CreateTaskRow(task, category));
            }

            mainStack.Children.Add(taskStack);
            expander.Content = mainStack;

            panel.Children.Add(expander);
        }

        if (inlineAction == "AddCategory")
        {
            panel.Children.Add(CreateInlineInput(null));
        }

        inputToFocus?.Focus();
    }

    private ContextMenu BuildCategoryMenu(TodoCategory category)
    {
        var menu = new ContextMenu();

        var addTask = new MenuItem { Header = "Add Task" };
        addTask.Click += (_, _) =>
        {
            inlineAction = "AddTask";
            inlineCategory = category;
            BuildUI();
        };

        var addCategory = new MenuItem { Header = "Add Category" };
        addCategory.Click += (_, _) =>
        {
            inlineAction = "AddCategory";
            inlineCategory = null;
            BuildUI();
        };

        menu.Items.Add(addTask);
        menu.Items.Add(addCategory);

        return menu;
    }

    // ⭐ TASK RIGHT‑CLICK MENU: MOVE TO + PRIORITY + DUE DATE + DELETE
    private ContextMenu BuildTaskMenu(TodoTask task, TodoCategory category)
    {
        var menu = new ContextMenu();

        // Move To submenu
        var moveTo = new MenuItem { Header = "Move To" };

        foreach (var cat in categories)
        {
            var item = new MenuItem { Header = cat.Name };

            item.Click += (_, _) =>
            {
                category.Tasks.Remove(task);
                cat.Tasks.Add(task);
                BuildUI();
            };

            moveTo.Items.Add(item);
        }

        // Priority submenu
        var priorityMenu = new MenuItem { Header = "Set Priority" };

        var high = new MenuItem { Header = "High (Red)" };
        high.Click += (_, _) =>
        {
            task.Priority = TaskPriority.High;
            BuildUI();
        };

        var medium = new MenuItem { Header = "Medium (Yellow)" };
        medium.Click += (_, _) =>
        {
            task.Priority = TaskPriority.Medium;
            BuildUI();
        };

        var low = new MenuItem { Header = "Low (Green)" };
        low.Click += (_, _) =>
        {
            task.Priority = TaskPriority.Low;
            BuildUI();
        };

        var none = new MenuItem { Header = "None" };
        none.Click += (_, _) =>
        {
            task.Priority = TaskPriority.None;
            BuildUI();
        };

        priorityMenu.Items.Add(high);
        priorityMenu.Items.Add(medium);
        priorityMenu.Items.Add(low);
        priorityMenu.Items.Add(none);

        // ⭐ Due Date submenu
        var dueMenu = new MenuItem { Header = "Set Due Date" };

        var today = new MenuItem { Header = "Today" };
        today.Click += (_, _) =>
        {
            task.DueDate = DateTime.Today;
            BuildUI();
        };

        var tomorrow = new MenuItem { Header = "Tomorrow" };
        tomorrow.Click += (_, _) =>
        {
            task.DueDate = DateTime.Today.AddDays(1);
            BuildUI();
        };

        var nextWeek = new MenuItem { Header = "Next Week" };
        nextWeek.Click += (_, _) =>
        {
            task.DueDate = DateTime.Today.AddDays(7);
            BuildUI();
        };

        var pickDate = new MenuItem { Header = "Pick Date…" };
        pickDate.Click += (_, _) => ShowDatePicker(task);

        var enterDate = new MenuItem { Header = "Enter Date…" };
        enterDate.Click += (_, _) => ShowManualDateEntry(task);

        var clearDate = new MenuItem { Header = "Clear Due Date" };
        clearDate.Click += (_, _) =>
        {
            task.DueDate = null;
            BuildUI();
        };

        dueMenu.Items.Add(today);
        dueMenu.Items.Add(tomorrow);
        dueMenu.Items.Add(nextWeek);
        dueMenu.Items.Add(pickDate);
        dueMenu.Items.Add(enterDate);
        dueMenu.Items.Add(clearDate);

        // Delete
        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) => DeleteTask(task, category);

        menu.Items.Add(moveTo);
        menu.Items.Add(priorityMenu);
        menu.Items.Add(dueMenu);
        menu.Items.Add(delete);

        return menu;
    }

    // ⭐ Calendar popup (fully fixed)
    private async void ShowDatePicker(TodoTask task)
    {
        var dialog = new Window
        {
            Width = 300,
            Height = 300,
            Title = "Pick Due Date"
        };

        var datePicker = new CalendarDatePicker
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        datePicker.SelectedDateChanged += (_, e) =>
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is DateTime selected)
                {
                    task.DueDate = selected.Date;
                    dialog.Close();
                    BuildUI();
                }
            }
        };

        dialog.Content = datePicker;
        await dialog.ShowDialog(this);
    }

    // ⭐ Manual date entry popup
    private async void ShowManualDateEntry(TodoTask task)
    {
        var dialog = new Window
        {
            Width = 300,
            Height = 150,
            Title = "Enter Due Date"
        };

        var stack = new StackPanel
        {
            Margin = new Avalonia.Thickness(10),
            Spacing = 10
        };

        var input = new TextBox { PlaceholderText = "YYYY-MM-DD" };

        var ok = new Button { Content = "OK" };
        ok.Click += (_, _) =>
        {
            if (DateTime.TryParse(input.Text, out var date))
            {
                task.DueDate = date.Date;
            }
            dialog.Close();
            BuildUI();
        };

        stack.Children.Add(input);
        stack.Children.Add(ok);

        dialog.Content = stack;
        await dialog.ShowDialog(this);
    }

    private Border CreateInlineInput(TodoCategory? category)
    {
        var border = new Border
        {
            Background = Brushes.LightGray,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(8)
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        var input = new TextBox
        {
            Width = 200,
            PlaceholderText = "Type name..."
        };

        inputToFocus = input;

        var ok = new Button { Content = "OK" };
        var cancel = new Button { Content = "Cancel" };

        ok.Click += (_, _) => SubmitInput(input.Text, category);
        cancel.Click += (_, _) =>
        {
            inlineAction = "";
            inlineCategory = null;
            BuildUI();
        };

        input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SubmitInput(input.Text, category);
            }
        };

        row.Children.Add(input);
        row.Children.Add(ok);
        row.Children.Add(cancel);

        border.Child = row;
        return border;
    }

    private void SubmitInput(string? text, TodoCategory? category)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (category == null)
        {
            categories.Add(new TodoCategory { Name = text });
        }
        else
        {
            category.Tasks.Add(new TodoTask { Text = text });
        }

        inlineAction = "";
        inlineCategory = null;

        BuildUI();
    }

    private Grid CreateTaskRow(TodoTask task, TodoCategory category)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 10
        };

        var check = new CheckBox();

        check.Click += (_, _) =>
        {
            category.Tasks.Remove(task);
            category.CompletedTasks.Add(task);
            BuildUI();
        };

        var text = new TextBlock { Text = task.Text };

        // Priority colors
        switch (task.Priority)
        {
            case TaskPriority.High:
                text.Foreground = Brushes.Red;
                break;

            case TaskPriority.Medium:
                text.Foreground = new SolidColorBrush(Color.Parse("#FFFF00"));
                break;

            case TaskPriority.Low:
                text.Foreground = Brushes.LightGreen;
                break;

            default:
                text.Foreground = Brushes.White;
                break;
        }

        // ⭐ Due date display
        if (task.DueDate.HasValue)
        {
            var due = task.DueDate.Value;

            string display = $"(Due: {due:MMM d})";

            var dueText = new TextBlock
            {
                Text = display,
                Foreground = Brushes.LightGray,
                Margin = new Avalonia.Thickness(10, 0, 0, 0)
            };

            // ⭐ Overdue icon
            if (due < DateTime.Today)
            {
                dueText.Text = "⚠️ OVERDUE";
                dueText.Foreground = Brushes.Red;
            }

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            stack.Children.Add(text);
            stack.Children.Add(dueText);

            row.Children.Add(check);
            row.Children.Add(stack);

            Grid.SetColumn(check, 0);
            Grid.SetColumn(stack, 1);

            var delete = new Button { Content = "X" };
            delete.Click += (_, _) => DeleteTask(task, category);

            row.Children.Add(delete);
            Grid.SetColumn(delete, 2);

            row.ContextMenu = BuildTaskMenu(task, category);

            return row;
        }

        var deleteBtn = new Button { Content = "X" };
        deleteBtn.Click += (_, _) => DeleteTask(task, category);

        row.Children.Add(check);
        row.Children.Add(text);
        row.Children.Add(deleteBtn);

        Grid.SetColumn(check, 0);
        Grid.SetColumn(text, 1);
        Grid.SetColumn(deleteBtn, 2);

        row.ContextMenu = BuildTaskMenu(task, category);

        return row;
    }

    private Grid CreateCompletedRow(TodoTask task, TodoCategory category)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 10
        };

        var check = new CheckBox { IsChecked = true };

        check.Click += (_, _) =>
        {
            category.CompletedTasks.Remove(task);
            category.Tasks.Add(task);
            BuildUI();
        };

        var text = new TextBlock
        {
            Text = task.Text,
            TextDecorations = TextDecorations.Strikethrough,
            Opacity = 0.5
        };

        var delete = new Button { Content = "X" };
        delete.Click += (_, _) =>
        {
            category.CompletedTasks.Remove(task);
            BuildUI();
        };

        row.Children.Add(check);
        row.Children.Add(text);
        row.Children.Add(delete);

        Grid.SetColumn(check, 0);
        Grid.SetColumn(text, 1);
        Grid.SetColumn(delete, 2);

        return row;
    }

    private void DeleteTask(TodoTask task, TodoCategory category)
    {
        int index = category.Tasks.IndexOf(task);

        if (index < 0) return;

        deletedTasks.Push((task, category, index));
        category.Tasks.Remove(task);

        BuildUI();
    }
}
