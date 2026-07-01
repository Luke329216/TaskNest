using System;
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

        var taskInput = this.FindControl<TextBox>("TaskInput");

        if (taskInput != null)
        {
            taskInput.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    AddTaskToGeneral_Click(null, new RoutedEventArgs());
                }
            };
        }

        var themePicker = this.FindControl<ComboBox>("ThemePicker");

        if (themePicker != null)
        {
            themePicker.SelectionChanged += ThemePicker_SelectionChanged;
        }

        categories.Add(new TodoCategory
        {
            Name = "General",
            Icon = "📁"
        });

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
            int completedCount = category.CompletedTasks.Count;
            int activeCount = category.Tasks.Count;
            int totalCount = completedCount + activeCount;

            double progressPercent = 0;

            if (totalCount > 0)
            {
                progressPercent = (double)completedCount / totalCount;
            }

            var headerStack = new StackPanel
            {
                Spacing = 3
            };

            // ⭐ CATEGORY ICON + NAME
            var headerRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            headerRow.Children.Add(new TextBlock
            {
                Text = category.Icon,
                FontSize = 22,
                Margin = new Avalonia.Thickness(0, -2, 0, 0)
            });

            headerRow.Children.Add(new TextBlock
            {
                Text = $"{category.Name} ({activeCount})",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            });

            headerStack.Children.Add(headerRow);

            headerStack.Children.Add(new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = progressPercent * 100,
                Width = 250,
                Height = 12
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = $"{(int)(progressPercent * 100)}% Complete",
                Foreground = Brushes.LightGray,
                FontSize = 11
            });

            var expander = new Expander
            {
                Header = headerStack,
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

        // ⭐ CATEGORY ICON PICKER
        var changeIcon = new MenuItem { Header = "Change Icon" };

        string[] icons =
        {
            "📁","📚","🏠","💼","🎮","🛒","💪","🧹","⭐","🔥"
        };

        foreach (var icon in icons)
        {
            var item = new MenuItem { Header = icon };
            item.Click += (_, _) =>
            {
                category.Icon = icon;
                BuildUI();
            };
            changeIcon.Items.Add(item);
        }

        var deleteCategory = new MenuItem
        {
            Header = "Delete Category"
        };

        deleteCategory.Click += (_, _) =>
        {
            if (category.Name == "General")
                return;

            var general = categories.First(c => c.Name == "General");

            foreach (var task in category.Tasks.ToList())
            {
                general.Tasks.Add(task);
            }

            foreach (var task in category.CompletedTasks.ToList())
            {
                general.CompletedTasks.Add(task);
            }

            categories.Remove(category);

            BuildUI();
        };

        menu.Items.Add(addTask);
        menu.Items.Add(addCategory);
        menu.Items.Add(changeIcon);

        if (category.Name != "General")
        {
            menu.Items.Add(deleteCategory);
        }

        return menu;
    }

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
        dueMenu.Items.Add(clearDate);

        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) => DeleteTask(task, category);

        menu.Items.Add(moveTo);
        menu.Items.Add(priorityMenu);
        menu.Items.Add(dueMenu);
        menu.Items.Add(delete);

        return menu;
    }

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
            categories.Add(new TodoCategory { Name = text, Icon = "📁" });
        }
        else
        {
            category.Tasks.Add(new TodoTask { Text = text });
        }

        inlineAction = "";
        inlineCategory = null;

        BuildUI();
    }

    private Border CreateTaskRow(TodoTask task, TodoCategory category)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var check = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 0)
        };

        check.Click += (_, _) =>
        {
            category.Tasks.Remove(task);
            category.CompletedTasks.Add(task);
            BuildUI();
        };

        var titlePanel = new StackPanel
        {
            Spacing = 6
        };

        var topRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };

        topRow.Children.Add(new TextBlock
        {
            Text = task.Icon,
            FontSize = 18,
            Width = 24,
            VerticalAlignment = VerticalAlignment.Center
        });

        topRow.Children.Add(new TextBlock
        {
            Text = task.Text,
            FontSize = 15,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        titlePanel.Children.Add(topRow);

        var metaRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        metaRow.Children.Add(new TextBlock
        {
            Text = task.Priority == TaskPriority.None ? "Normal" : task.Priority.ToString(),
            Foreground = Brushes.LightGray,
            FontSize = 12
        });

        if (task.DueDate.HasValue)
        {
            var due = task.DueDate.Value;
            string display = due < DateTime.Today ? "⚠️ OVERDUE" : $"Due {due:MMM d}";

            metaRow.Children.Add(new TextBlock
            {
                Text = display,
                Foreground = due < DateTime.Today ? Brushes.Red : Brushes.LightGray,
                FontSize = 12
            });
        }

        titlePanel.Children.Add(metaRow);

        var deleteBtn = new Button
        {
            Content = "✕",
            Width = 34,
            Height = 34,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.LightGray
        };
        deleteBtn.Click += (_, _) => DeleteTask(task, category);

        row.Children.Add(check);
        row.Children.Add(titlePanel);
        row.Children.Add(deleteBtn);

        Grid.SetColumn(check, 0);
        Grid.SetColumn(titlePanel, 1);
        Grid.SetColumn(deleteBtn, 2);

        var wrapper = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0F172A")),
            CornerRadius = new CornerRadius(14),
            Padding = new Avalonia.Thickness(14),
            Margin = new Avalonia.Thickness(0, 0, 0, 10),
            Child = row,
            ContextMenu = BuildTaskMenu(task, category)
        };

        return wrapper;
    }

    private Border CreateCompletedRow(TodoTask task, TodoCategory category)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var check = new CheckBox
        {
            IsChecked = true,
            VerticalAlignment = VerticalAlignment.Center
        };

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
            Opacity = 0.6,
            Foreground = Brushes.LightGray,
            TextWrapping = TextWrapping.Wrap
        };

        var delete = new Button
        {
            Content = "✕",
            Width = 34,
            Height = 34,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.LightGray
        };
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

        var wrapper = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0F172A")),
            CornerRadius = new CornerRadius(14),
            Padding = new Avalonia.Thickness(14),
            Margin = new Avalonia.Thickness(0, 0, 0, 10),
            Child = row
        };

        return wrapper;
    }

    private void ThemePicker_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo)
            return;

        switch (combo.SelectedIndex)
        {
            case 0:
                Background = new SolidColorBrush(Color.Parse("#0B1120"));
                break;

            case 1:
                Background = new SolidColorBrush(Color.Parse("#0F172A"));
                break;

            case 2:
                Background = new SolidColorBrush(Color.Parse("#2D1B69"));
                break;

            case 3:
                Background = new SolidColorBrush(Color.Parse("#064E3B"));
                break;

            case 4:
                Background = Brushes.White;
                break;
        }
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
