using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Platform;
using System.IO;
using System.Timers;
using Avalonia.Threading;

namespace TaskNestUI;

public partial class MainWindow : Window
{
    private ObservableCollection<TodoCategory> categories = new();
    private Stack<(TodoTask Task, TodoCategory Category, int Index)> deletedTasks = new();
    private HashSet<TodoCategory> expandedCategories = new();

    private string inlineAction = "";
    private TodoCategory? inlineCategory = null;
    private TextBox? inputToFocus = null;
    private double fontScale = 1.0;
    private double S(double v) => v * fontScale;

    public MainWindow()
    {
        try
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

            // Wire ThemePicker selection in code-behind to avoid XAML hookup issues
            var themePicker = this.FindControl<ComboBox>("ThemePicker");
            if (themePicker != null)
            {
                themePicker.SelectionChanged += ThemePicker_SelectionChanged;
                // Apply the currently selected theme immediately
                if (themePicker.SelectedIndex >= 0)
                {
                    ThemePicker_SelectionChanged(themePicker, null);
                }
            }

            // Ensure TabItem header text doesn't turn white on hover by forcing Foreground
            var tabControl = this.FindControl<TabControl>("MainTabs");
            if (tabControl != null)
            {
                // Iterate TabItem instances declared in XAML
                foreach (var obj in tabControl.Items)
                {
                    if (obj is TabItem ti)
                    {
                        void ApplyPrimaryForeground() => ti.Foreground = Application.Current?.Resources["PrimaryText"] as IBrush ?? Brushes.Black;

                        ApplyPrimaryForeground();
                    }
                }
            }

            categories.Add(new TodoCategory { Name = "General", Icon = "📁" });

            // Apply initial theme based on BackgroundColorSelector if present
            var bgSelector = this.FindControl<ComboBox>("BackgroundColorSelector");
            if (bgSelector != null && bgSelector.SelectedIndex >= 0)
            {
                ApplyThemeByIndex(bgSelector.SelectedIndex);
            }

            // Wire Settings button clicks programmatically
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDebug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Constructor: Starting button wiring\n");
            
            // TEST BUTTON
            var testBtn = this.FindControl<Button>("TestBtn");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] TestBtn found: {testBtn != null}\n");
            if (testBtn != null) testBtn.Click += (_, _) => File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] *** TEST BUTTON CLICKED ***\n");
            
            var exportBtn = this.FindControl<Button>("ExportTasksBtn");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ExportTasksBtn found: {exportBtn != null}\n");
            if (exportBtn != null) exportBtn.Click += OnExportTasks_Click;
            
            var importBtn = this.FindControl<Button>("ImportTasksBtn");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ImportTasksBtn found: {importBtn != null}\n");
            if (importBtn != null) importBtn.Click += OnImportTasks_Click;
            
            var cacheBtn = this.FindControl<Button>("ClearCacheBtn");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ClearCacheBtn found: {cacheBtn != null}\n");
            if (cacheBtn != null) cacheBtn.Click += OnClearCache_Click;
            
            var resetBtn = this.FindControl<Button>("ResetDefaultBtn");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ResetDefaultBtn found: {resetBtn != null}\n");
            if (resetBtn != null) resetBtn.Click += OnResetToDefault_Click;
            
            var shortcutsBtn = this.FindControl<Button>("ShortcutsBtn");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ShortcutsBtn found: {shortcutsBtn != null}\n");
            if (shortcutsBtn != null) shortcutsBtn.Click += OnViewShortcuts_Click;
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Constructor: Button wiring complete\n");

            SetupRightClick();
            BuildUI();
        }
        catch (System.Exception ex)
        {
            try
            {
                var log = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestStartup.log");
                System.IO.File.AppendAllText(log, "MainWindow ctor exception: " + ex + System.Environment.NewLine);
            }
            catch {}

            throw;
        }
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
        // Use class-level S(double) for consistent scaling across methods
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
                FontSize = S(22),
                Margin = new Avalonia.Thickness(0, -2, 0, 0)
            });

            headerRow.Children.Add(new TextBlock
            {
                Text = $"{category.Name} ({activeCount})",
                Foreground = Application.Current?.Resources["PrimaryText"] as IBrush ?? Brushes.White,
                FontWeight = FontWeight.Bold
            });

            headerStack.Children.Add(headerRow);

            headerStack.Children.Add(new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = progressPercent * 100,
                Width = 250,
                Height = 12,
                Foreground = Application.Current?.Resources["AccentColor"] as IBrush
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = $"{(int)(progressPercent * 100)}% Complete",
                Foreground = Application.Current?.Resources["SubtleText"] as IBrush ?? Brushes.LightGray,
                FontSize = S(11)
            });
            var isExpanded = expandedCategories.Contains(category);

            var contentPanel = new StackPanel { Spacing = 8 };

            // Create header grid and border so we can control header layout/background
            var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            headerGrid.Children.Add(headerStack);

            var headerBorder = new Border
            {
                Background = Application.Current?.Resources["SectionBackground"] as IBrush,
                Padding = new Avalonia.Thickness(8,6,8,6)
            };
            headerBorder.CornerRadius = new CornerRadius(12,12,0,0);

            var toggleBtn = new Button
            {
                Content = isExpanded ? "▴" : "▾",
                Background = Application.Current?.Resources["SectionBackground"] as IBrush,
                Foreground = Application.Current?.Resources["PrimaryText"] as IBrush,
                BorderBrush = Application.Current?.Resources["CardBorder"] as IBrush,
                Width = 44,
                Height = 44,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Padding = new Avalonia.Thickness(0)
            };

            toggleBtn.Click += (_, _) =>
            {
                var now = !(contentPanel.IsVisible);
                contentPanel.IsVisible = now;
                toggleBtn.Content = now ? "▴" : "▾";
                if (now) expandedCategories.Add(category); else expandedCategories.Remove(category);
            };

            Grid.SetColumn(toggleBtn, 1);
            headerGrid.Children.Add(toggleBtn);

            headerBorder.Child = headerGrid;
            // Attach category context menu to header
            headerBorder.ContextMenu = BuildCategoryMenu(category);

            // Content for the pseudo-expander
            var mainStack = contentPanel;
            mainStack.Spacing = 8;
            mainStack.IsVisible = isExpanded;

            // Build content later (we'll populate mainStack below)

            if (category.CompletedTasks.Count > 0)
            {
                // Build a completed section header + toggle (matches category header style)
                var compHeader = new StackPanel { Spacing = 0 };
                compHeader.Children.Add(new TextBlock { Text = $"Completed ({category.CompletedTasks.Count})", Foreground = Application.Current?.Resources["PrimaryText"] as IBrush, FontWeight = FontWeight.SemiBold });

                var compHeaderBorder = new Border
                {
                    Background = Application.Current?.Resources["SectionBackground"] as IBrush,
                    Padding = new Avalonia.Thickness(8,6,8,6)
                };

                var compHeaderGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                compHeaderGrid.Children.Add(compHeader);

                var compContentPanel = new StackPanel { Spacing = 5 };
                foreach (var task in category.CompletedTasks.ToList())
                {
                    compContentPanel.Children.Add(CreateCompletedRow(task, category));
                }

                var compIsExpanded = true;
                var compToggle = new Button
                {
                    Content = compIsExpanded ? "▴" : "▾",
                    Background = Application.Current?.Resources["SectionBackground"] as IBrush,
                    Foreground = Application.Current?.Resources["PrimaryText"] as IBrush,
                    BorderBrush = Application.Current?.Resources["CardBorder"] as IBrush,
                    Width = 44,
                    Height = 44,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Padding = new Avalonia.Thickness(0)
                };
                compToggle.Click += (_, _) =>
                {
                    var now = !(compContentPanel.IsVisible);
                    compContentPanel.IsVisible = now;
                    compToggle.Content = now ? "▴" : "▾";
                };

                Grid.SetColumn(compToggle, 1);
                compHeaderGrid.Children.Add(compToggle);
                compHeaderBorder.Child = compHeaderGrid;
                compHeaderBorder.CornerRadius = new CornerRadius(10,10,0,0);

                compContentPanel.IsVisible = compIsExpanded;

                var compContainer = new StackPanel { Spacing = 0 };
                compContainer.Children.Add(compHeaderBorder);
                compContainer.Children.Add(compContentPanel);

                mainStack.Children.Add(compContainer);
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

            // Combine header and content into a single container and wrap in card Border
            var containerStack = new StackPanel { Spacing = 0 };
            containerStack.Children.Add(headerBorder);
            containerStack.Children.Add(mainStack);

            var cardWrapper = new Border
            {
                Child = containerStack,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            cardWrapper.Classes.Add("card");

            panel.Children.Add(cardWrapper);
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
            Background = Application.Current?.Resources["SectionBackground"] as IBrush ?? Brushes.LightGray,
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
        IBrush GetPriorityBrushLocal(TaskPriority p)
        {
            return p switch
            {
                TaskPriority.High => new SolidColorBrush(Color.Parse("#EF4444")),
                TaskPriority.Medium => new SolidColorBrush(Color.Parse("#F59E0B")),
                TaskPriority.Low => new SolidColorBrush(Color.Parse("#10B981")),
                _ => Application.Current?.Resources["PrimaryText"] as IBrush ?? Brushes.White,
            };
        }
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        var check = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 8, 0),
            Foreground = Application.Current?.Resources["PrimaryText"] as IBrush,
            BorderBrush = Application.Current?.Resources["ControlBorder"] as IBrush,
            Background = Application.Current?.Resources["SectionBackground"] as IBrush,
            Width = 28,
            Height = 28
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
            FontSize = S(18),
            Width = 24,
            VerticalAlignment = VerticalAlignment.Center
        });

        topRow.Children.Add(new TextBlock
        {
            Text = task.Text,
            FontSize = S(15),
            Foreground = GetPriorityBrushLocal(task.Priority),
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
            Foreground = Application.Current?.Resources["SubtleText"] as IBrush ?? Brushes.LightGray,
            FontSize = S(12)
        });

        if (task.DueDate.HasValue)
        {
            var due = task.DueDate.Value;
            string display = due < DateTime.Today ? "⚠️ OVERDUE" : $"Due {due:MMM d}";

            metaRow.Children.Add(new TextBlock
            {
                Text = display,
                Foreground = due < DateTime.Today ? Brushes.Red : Application.Current?.Resources["SubtleText"] as IBrush ?? Brushes.LightGray,
                FontSize = S(12)
            });
        }

        titlePanel.Children.Add(metaRow);

        var deleteBtn = new Button
        {
            Content = "✕",
            Width = 28,
            Height = 28,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Application.Current?.Resources["SubtleText"] as IBrush ?? Brushes.LightGray,
            Margin = new Avalonia.Thickness(0,0,20,0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Avalonia.Thickness(2),
            VerticalAlignment = VerticalAlignment.Center
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
            Background = Application.Current?.Resources["SectionBackground"] as IBrush ?? new SolidColorBrush(Color.Parse("#0F172A")),
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
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Application.Current?.Resources["PrimaryText"] as IBrush,
            BorderBrush = Application.Current?.Resources["ControlBorder"] as IBrush,
            Background = Application.Current?.Resources["SectionBackground"] as IBrush,
            Width = 24,
            Height = 24
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
            Foreground = Application.Current?.Resources["SubtleText"] as IBrush ?? Brushes.LightGray,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        var delete = new Button
        {
            Content = "✕",
            Width = 28,
            Height = 28,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Application.Current?.Resources["SubtleText"] as IBrush ?? Brushes.LightGray,
            Margin = new Avalonia.Thickness(0,0,20,0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Avalonia.Thickness(2),
            VerticalAlignment = VerticalAlignment.Center
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
            Background = Application.Current?.Resources["SectionBackground"] as IBrush ?? new SolidColorBrush(Color.Parse("#0F172A")),
            CornerRadius = new CornerRadius(14),
            Padding = new Avalonia.Thickness(14),
            Margin = new Avalonia.Thickness(0, 0, 0, 10),
            Child = row
        };

        return wrapper;
    }

    private void ThemePicker_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        if (sender is not ComboBox combo)
            return;
        ApplyThemeByIndex(combo.SelectedIndex);
    }

    private void ApplyThemeByIndex(int index)
    {
        var resources = Application.Current?.Resources;
        if (resources == null) return;

        Color ParseColor(string hex) => Color.Parse(hex);

        void SetBrush(string key, string hex)
        {
            if (resources.TryGetValue(key, out var existing) && existing is SolidColorBrush sb)
            {
                sb.Color = ParseColor(hex);
            }
            else
            {
                resources[key] = new SolidColorBrush(ParseColor(hex));
            }
        }

        void SetBrushObj(string key, IBrush brush)
        {
            if (resources.TryGetValue(key, out var existing) && existing is SolidColorBrush sb && brush is SolidColorBrush newSb)
            {
                sb.Color = newSb.Color;
            }
            else
            {
                resources[key] = brush;
            }
        }

        switch (index)
        {
            // Midnight
            case 0:
                SetBrush("WindowBackground", "#0B1120");
                SetBrush("CardBackground", "#0F172A");
                SetBrush("SectionBackground", "#0F172A");
                SetBrush("SubtleText", "#94A3B8");
                SetBrush("AccentColor", "#2563EB");
                SetBrushObj("PrimaryText", Brushes.White);
                SetBrush("ControlBorder", "#334155");
                SetBrush("CardBorder", "#1E293B");
                SetBrush("MutedButtonBackground", "#334155");
                break;

            // Ocean Blue
            case 1:
                SetBrush("WindowBackground", "#071025");
                SetBrush("CardBackground", "#081935");
                SetBrush("SectionBackground", "#0B2540");
                SetBrush("SubtleText", "#9FB8D6");
                SetBrush("AccentColor", "#0EA5E9");
                SetBrushObj("PrimaryText", Brushes.White);
                SetBrush("ControlBorder", "#223049");
                SetBrush("CardBorder", "#0F2433");
                SetBrush("MutedButtonBackground", "#223049");
                break;

            // Purple Night
            case 2:
                SetBrush("WindowBackground", "#120A27");
                SetBrush("CardBackground", "#1A0F3A");
                SetBrush("SectionBackground", "#2D1B69");
                SetBrush("SubtleText", "#BDAFF6");
                SetBrush("AccentColor", "#8B5CF6");
                SetBrushObj("PrimaryText", Brushes.White);
                SetBrush("ControlBorder", "#3A2550");
                SetBrush("CardBorder", "#24143A");
                SetBrush("MutedButtonBackground", "#3A2550");
                break;

            // Emerald
            case 3:
                SetBrush("WindowBackground", "#052020");
                SetBrush("CardBackground", "#063634");
                SetBrush("SectionBackground", "#064E3B");
                SetBrush("SubtleText", "#9BD6C6");
                SetBrush("AccentColor", "#10B981");
                SetBrushObj("PrimaryText", Brushes.White);
                SetBrush("ControlBorder", "#063F36");
                SetBrush("CardBorder", "#042E28");
                SetBrush("MutedButtonBackground", "#063F36");
                break;

            // Light Mode
            case 4:
                SetBrush("WindowBackground", "#FFFFFF");
                SetBrush("CardBackground", "#F3F4F6");
                SetBrush("SectionBackground", "#FFFFFF");
                SetBrush("SubtleText", "#6B7280");
                SetBrush("AccentColor", "#2563EB");
                SetBrushObj("PrimaryText", new SolidColorBrush(ParseColor("#0F172A")));
                SetBrush("ControlBorder", "#D1D5DB");
                SetBrush("CardBorder", "#E5E7EB");
                SetBrush("MutedButtonBackground", "#E5E7EB");
                break;
        }

        // Update window background to reflect resource change immediately
        if (resources.TryGetValue("WindowBackground", out var winBg) && winBg is IBrush brush)
        {
            Background = brush;
        }

        // Force style refresh by rebuilding UI where applicable
        BuildUI();
    }

    private void OnBackgroundColorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo)
        {
            // Map combobox selection to theme index
            int themeIndex = combo.SelectedIndex switch
            {
                0 => 4,  // Light
                1 => 0,  // Midnight
                2 => 1,  // Ocean
                3 => 2,  // Purple
                4 => 3,  // Emerald
                5 => 0,  // Red (map to midnight for now)
                _ => 4
            };

            // Apply theme directly
            ApplyThemeByIndex(themeIndex);
        }
    }

    private void OnAccentColorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && Application.Current?.Resources is IDictionary<object, object> resources)
        {
            Color accentColor = combo.SelectedIndex switch
            {
                0 => Color.Parse("#3B82F6"),      // Blue
                1 => Color.Parse("#10B981"),      // Green
                2 => Color.Parse("#8B5CF6"),      // Purple
                3 => Color.Parse("#F59E0B"),      // Orange
                4 => Color.Parse("#EF4444"),      // Red
                5 => Color.Parse("#FBBF24"),      // Yellow
                _ => Color.Parse("#3B82F6")
            };

            if (resources["AccentColor"] is SolidColorBrush brush)
            {
                brush.Color = accentColor;
            }
            
            BuildUI();
        }
    }

    private void OnTextSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo)
        {
            fontScale = combo.SelectedIndex switch
            {
                0 => 0.9,
                1 => 1.0,
                2 => 1.1,
                3 => 1.25,
                _ => 1.0
            };

            // Defer UI rebuild to the dispatcher to avoid running during XAML initialization
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    BuildUI();
                    ShowStatusMessage($"✓ Text size updated ({combo.SelectedIndex})");
                }
                catch (Exception ex)
                {
                    var log = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestStartup.log");
                    try { System.IO.File.AppendAllText(log, "OnTextSizeChanged deferred BuildUI exception: " + ex + System.Environment.NewLine); } catch {}
                }
            });
        }
    }

    // Settings Button Handlers
    private void ShowStatusMessage(string message, string color = "SuccessColor")
    {
        string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDebug.log");
        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ShowStatusMessage called with: {message}\n");
        
        var statusMsg = this.FindControl<TextBlock>("StatusMessage");
        var statusBorder = this.FindControl<Border>("StatusMessageBorder");
        
        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] StatusMessage found: {statusMsg != null}\n");
        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] StatusMessageBorder found: {statusBorder != null}\n");
        
        if (statusMsg != null && statusBorder != null)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Setting text and making visible\n");
            statusMsg.Text = message;
            statusBorder.IsVisible = true;
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Status visible: {statusBorder.IsVisible}\n");
            
            // Auto-hide after 4 seconds
            var timer = new System.Timers.Timer(4000);
            timer.Elapsed += (_, _) =>
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Timer elapsed, hiding message\n");
                Dispatcher.UIThread.Post(() =>
                {
                    if (statusBorder != null)
                        statusBorder.IsVisible = false;
                });
                timer.Stop();
            };
            timer.Start();
        }
        else
        {
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ERROR: Could not find status message controls!\n");
        }
    }

    public void OnHeaderTestBtn_Click(object? sender, RoutedEventArgs e)
    {
        string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDebug.log");
        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] *** HEADER TEST BUTTON CLICKED ***\n");
    }

    public void OnTestBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ShowStatusMessage("✅ TEST BUTTON WORKS! Click event fired!");
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDebug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] *** TEST BUTTON CLICKED ***\n");
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error in test button: {ex.Message}");
        }
    }

    public void OnExportTasks_Click(object? sender, RoutedEventArgs e)
    {
        string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDebug.log");
        try
        {
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] *** EXPORT BUTTON CLICKED ***\n");
            
            string tasksJson = System.Text.Json.JsonSerializer.Serialize(categories);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = System.IO.Path.Combine(documentsPath, $"TaskNest_Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");
            System.IO.File.WriteAllText(filePath, tasksJson);
            
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Export succeeded, calling ShowStatusMessage\n");
            ShowStatusMessage($"✓ Tasks exported successfully!\nFile: {System.IO.Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] Export failed: {ex.Message}\n{ex.StackTrace}\n");
            ShowStatusMessage($"✗ Export failed: {ex.Message}");
        }
    }

    public void OnImportTasks_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ShowStatusMessage("📂 Import feature ready. JSON files export to your Documents folder.");
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"✗ Import error: {ex.Message}");
        }
    }

    public void OnClearCache_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string tempPath = System.IO.Path.GetTempPath();
            string[] cacheFiles = System.IO.Directory.GetFiles(tempPath, "TaskNest*.tmp");
            
            int deletedCount = 0;
            foreach (var file in cacheFiles)
            {
                try 
                { 
                    System.IO.File.Delete(file);
                    deletedCount++;
                } 
                catch { }
            }

            ShowStatusMessage($"✓ Cache cleared! Removed {deletedCount} temporary files.");
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"✗ Cache clear failed: {ex.Message}");
        }
    }

    public void OnResetToDefault_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Reset theme to light
            if (this.FindControl<ComboBox>("BackgroundColorSelector") is ComboBox bgSelector)
            {
                bgSelector.SelectedIndex = 0;
            }

            // Reset accent to blue
            if (this.FindControl<ComboBox>("AccentColorSelector") is ComboBox accentSelector)
            {
                accentSelector.SelectedIndex = 0;
            }

            // Reset text size to normal
            if (this.FindControl<ComboBox>("TextSizeSelector") is ComboBox textSelector)
            {
                textSelector.SelectedIndex = 1;
            }

            ShowStatusMessage("✓ All settings reset to default!");
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"✗ Reset failed: {ex.Message}");
        }
    }

    public void OnViewShortcuts_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string shortcuts = @"⌨️ KEYBOARD SHORTCUTS

📋 Task Management:
  • Enter - Add task
  • Ctrl+A - Select all
  • Delete - Remove task
  • Ctrl+Z - Undo delete

📂 Categories:
  • Right-click - Menu
  • Ctrl+N - New

🎨 Themes:
  • Shift+T - Toggle
  • Shift+A - Change color

⚙️ App:
  • Ctrl+Q - Quit
  • F1 - Help";

            ShowStatusMessage(shortcuts);
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"✗ Shortcuts error: {ex.Message}");
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
