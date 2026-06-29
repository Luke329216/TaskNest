using Avalonia;
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

        categories.Add(new TodoCategory
        {
            Name = "General"
        });

        BuildUI();
    }

    private void AddTaskToGeneral_Click(object? sender, RoutedEventArgs e)
    {
        var input = this.FindControl<TextBox>("TaskInput");

        if (input == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(input.Text))
        {
            return;
        }

        var generalCategory = GetOrCreateCategory("General");

        generalCategory.Tasks.Add(new TodoTask
        {
            Text = input.Text.Trim()
        });

        input.Text = "";

        BuildUI();
    }

    private void TaskInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddTaskToGeneral_Click(sender, new RoutedEventArgs());
        }
    }

    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        if (deletedTasks.Count == 0)
        {
            return;
        }

        var deleted = deletedTasks.Pop();

        if (deleted.Index >= 0 && deleted.Index <= deleted.Category.Tasks.Count)
        {
            deleted.Category.Tasks.Insert(deleted.Index, deleted.Task);
        }
        else
        {
            deleted.Category.Tasks.Add(deleted.Task);
        }

        BuildUI();
    }

    private void BuildUI()
    {
        inputToFocus = null;

        var panel = this.FindControl<StackPanel>("CategoryPanel");

        if (panel == null)
        {
            return;
        }

        panel.Children.Clear();

        foreach (var category in categories)
        {
            var currentCategory = category;

            var expander = new Expander
            {
                Header = $"{currentCategory.Name} ({currentCategory.Tasks.Count})",
                IsExpanded = currentCategory.IsExpanded,
                HorizontalAlignment = HorizontalAlignment.Left,
                MinWidth = 300
            };

            expander.PropertyChanged += (_, args) =>
            {
                if (args.Property.Name == nameof(Expander.IsExpanded))
                {
                    currentCategory.IsExpanded = expander.IsExpanded;
                }
            };

            expander.ContextMenu = MakeCategoryMenu(currentCategory);

            var taskStack = new StackPanel
            {
                Margin = new Thickness(20, 5, 0, 5),
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // If user clicked "Add Task", show textbox INSIDE this category
            if (inlineAction == "AddTask" && inlineCategory == currentCategory)
            {
                var inputRow = MakeInlineInputRow(
                    "New Task:",
                    "Type task name...",
                    name =>
                    {
                        currentCategory.Tasks.Add(new TodoTask
                        {
                            Text = name
                        });

                        ClearInlineBox();
                        BuildUI();
                    });

                taskStack.Children.Add(inputRow);
            }

            foreach (var task in currentCategory.Tasks)
            {
                var currentTask = task;

                var taskRow = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MinWidth = 300
                };

                var taskText = new TextBlock
                {
                    Text = currentTask.Text,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var deleteButton = new Button
                {
                    Content = "X"
                };

                deleteButton.Click += (_, _) =>
                {
                    DeleteTask(currentTask, currentCategory);
                };

                taskRow.Children.Add(taskText);
                taskRow.Children.Add(deleteButton);

                Grid.SetColumn(taskText, 0);
                Grid.SetColumn(deleteButton, 1);

                taskRow.ContextMenu = MakeTaskMenu(currentTask, currentCategory);

                taskStack.Children.Add(taskRow);
            }

            expander.Content = taskStack;

            panel.Children.Add(expander);
        }

        // If user clicked "Add Category", show textbox in the category area
        if (inlineAction == "AddCategory")
        {
            var inputRow = MakeInlineInputRow(
                "New Category:",
                "Type category name...",
                name =>
                {
                    GetOrCreateCategory(name);

                    ClearInlineBox();
                    BuildUI();
                });

            panel.Children.Add(inputRow);
        }

        if (inputToFocus != null)
        {
            inputToFocus.Focus();
        }
    }

    private Border MakeInlineInputRow(string labelText, string watermark, System.Action<string> confirmAction)
    {
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8),
            Background = Brushes.LightGray,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 450
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8
        };

        var label = new TextBlock
        {
            Text = labelText,
            VerticalAlignment = VerticalAlignment.Center
        };

        var input = new TextBox
        {
            PlaceholderText = watermark,
            MinWidth = 220
        };

        var okButton = new Button
        {
            Content = "OK"
        };

        var cancelButton = new Button
        {
            Content = "Cancel"
        };

        okButton.Click += (_, _) =>
        {
            string name = input.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            confirmAction(name);
        };

        cancelButton.Click += (_, _) =>
        {
            ClearInlineBox();
            BuildUI();
        };

        input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                string name = input.Text?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                confirmAction(name);
            }

            if (e.Key == Key.Escape)
            {
                ClearInlineBox();
                BuildUI();
            }
        };

        grid.Children.Add(label);
        grid.Children.Add(input);
        grid.Children.Add(okButton);
        grid.Children.Add(cancelButton);

        Grid.SetColumn(label, 0);
        Grid.SetColumn(input, 1);
        Grid.SetColumn(okButton, 2);
        Grid.SetColumn(cancelButton, 3);

        border.Child = grid;

        inputToFocus = input;

        return border;
    }

    private ContextMenu MakeCategoryMenu(TodoCategory category)
    {
        var menu = new ContextMenu();

        var addTaskItem = new MenuItem
        {
            Header = "Add Task"
        };

        addTaskItem.Click += (_, _) =>
        {
            ShowInlineAddTask(category);
        };

        var addCategoryItem = new MenuItem
        {
            Header = "Add Category"
        };

        addCategoryItem.Click += (_, _) =>
        {
            ShowInlineAddCategory();
        };

        menu.Items.Add(addTaskItem);
        menu.Items.Add(addCategoryItem);

        return menu;
    }

    private ContextMenu MakeTaskMenu(TodoTask task, TodoCategory currentCategory)
    {
        var menu = new ContextMenu();

        var addTaskItem = new MenuItem
        {
            Header = "Add Task To This Category"
        };

        addTaskItem.Click += (_, _) =>
        {
            ShowInlineAddTask(currentCategory);
        };

        var addCategoryItem = new MenuItem
        {
            Header = "Add Category"
        };

        addCategoryItem.Click += (_, _) =>
        {
            ShowInlineAddCategory();
        };

        var moveToItem = new MenuItem
        {
            Header = "Move To"
        };

        foreach (var category in categories)
        {
            var targetCategory = category;

            var categoryMoveItem = new MenuItem
            {
                Header = targetCategory.Name
            };

            categoryMoveItem.Click += (_, _) =>
            {
                MoveTask(task, currentCategory, targetCategory);
            };

            moveToItem.Items.Add(categoryMoveItem);
        }

        var deleteItem = new MenuItem
        {
            Header = "Delete"
        };

        deleteItem.Click += (_, _) =>
        {
            DeleteTask(task, currentCategory);
        };

        menu.Items.Add(addTaskItem);
        menu.Items.Add(addCategoryItem);
        menu.Items.Add(moveToItem);
        menu.Items.Add(deleteItem);

        return menu;
    }

    private void AddCategoryFromEmptySpace_Click(object? sender, RoutedEventArgs e)
    {
        ShowInlineAddCategory();
    }

    private void ShowInlineAddCategory()
    {
        inlineAction = "AddCategory";
        inlineCategory = null;

        BuildUI();
    }

    private void ShowInlineAddTask(TodoCategory category)
    {
        inlineAction = "AddTask";
        inlineCategory = category;

        category.IsExpanded = true;

        BuildUI();
    }

    private void ClearInlineBox()
    {
        inlineAction = "";
        inlineCategory = null;
        inputToFocus = null;
    }

    private void DeleteTask(TodoTask task, TodoCategory category)
    {
        int index = category.Tasks.IndexOf(task);

        if (index < 0)
        {
            return;
        }

        deletedTasks.Push((task, category, index));

        category.Tasks.Remove(task);

        BuildUI();
    }

    private void MoveTask(TodoTask task, TodoCategory oldCategory, TodoCategory newCategory)
    {
        if (oldCategory == newCategory)
        {
            return;
        }

        oldCategory.Tasks.Remove(task);
        newCategory.Tasks.Add(task);

        BuildUI();
    }

    private TodoCategory GetOrCreateCategory(string name)
    {
        name = name.Trim();

        var existing = categories.FirstOrDefault(c =>
            c.Name.ToLower() == name.ToLower());

        if (existing != null)
        {
            return existing;
        }

        var newCategory = new TodoCategory
        {
            Name = name
        };

        categories.Add(newCategory);

        return newCategory;
    }
}