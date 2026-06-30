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

    // ✅ EMPTY SPACE MENU
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

            // ✅ COMPLETED AT TOP
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

            // ✅ INLINE TASK INPUT INSIDE CATEGORY
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

        // ✅ INLINE CATEGORY INPUT AT BOTTOM (NOT TOP)
        if (inlineAction == "AddCategory")
        {
            panel.Children.Add(CreateInlineInput(null));
        }

        inputToFocus?.Focus();
    }

    // ✅ CATEGORY MENU
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

    // ✅ TASK MENU (MOVE BACK ✅)
    private ContextMenu BuildTaskMenu(TodoTask task, TodoCategory category)
    {
        var menu = new ContextMenu();

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

        var delete = new MenuItem { Header = "Delete" };

        delete.Click += (_, _) => DeleteTask(task, category);

        menu.Items.Add(moveTo);
        menu.Items.Add(delete);

        return menu;
    }

    // ✅ INLINE INPUT BOX (THIS FIXES YOUR MAIN ISSUE)
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

        // ✅ ENTER KEY FIX
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

        var delete = new Button { Content = "X" };
        delete.Click += (_, _) => DeleteTask(task, category);

        row.Children.Add(check);
        row.Children.Add(text);
        row.Children.Add(delete);

        Grid.SetColumn(check, 0);
        Grid.SetColumn(text, 1);
        Grid.SetColumn(delete, 2);

        // ✅ RIGHT CLICK TASK MENU RESTORED
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