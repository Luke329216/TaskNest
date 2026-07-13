using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace TaskNestUI;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string server = "localhost", string database = "Tododb")
    {
        // Using Windows Authentication
        _connectionString = $"Server={server};Database={database};Integrated Security=true;Encrypt=false;TrustServerCertificate=true;";
    }

    /// <summary>
    /// Test the database connection
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDB.log"),
                $"[{DateTime.Now:HH:mm:ss}] Connection error: {ex.Message}\n");
            return false;
        }
    }

    /// <summary>
    /// Load all tasks from the database
    /// </summary>
    public async Task<ObservableCollection<TodoCategory>> LoadTasksAsync()
    {
        var categories = new ObservableCollection<TodoCategory>();

        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // First, load all categories
                var categoryDict = new Dictionary<int, (string Name, string Icon)>();
                using (SqlCommand cmd = new SqlCommand("SELECT Id, category FROM dbo.categories", conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            categoryDict[id] = (name, "📁");
                        }
                    }
                }

                // Always add General category if not exists
                if (!categoryDict.Values.Any(c => c.Name == "General"))
                {
                    var generalCategory = new TodoCategory { Name = "General", Icon = "📁" };
                    categories.Add(generalCategory);
                }

                // Load tasks for each category
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT Id, task, IsCompleted, DueDate, Priority, categoryId 
                      FROM dbo.tasks 
                      ORDER BY categoryId, CreatedAt DESC", conn))
                {
                    // Add DueDate and Priority columns if they exist, otherwise handle gracefully
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var tasksByCategory = new Dictionary<int, List<TodoTask>>();

                        while (await reader.ReadAsync())
                        {
                            int taskId = reader.GetInt32(0);
                            string taskText = reader.GetString(1);
                            bool isCompleted = reader.GetBoolean(2);
                            int categoryId = reader.GetInt32(5);

                            var task = new TodoTask
                            {
                                Text = taskText,
                                IsCompleted = isCompleted,
                                Priority = TaskPriority.None
                            };

                            // Try to read optional columns
                            if (!reader.IsDBNull(3))
                            {
                                task.DueDate = reader.GetDateTime(3);
                            }

                            if (!reader.IsDBNull(4))
                            {
                                if (int.TryParse(reader.GetString(4), out int priorityInt))
                                {
                                    task.Priority = (TaskPriority)priorityInt;
                                }
                            }

                            if (!tasksByCategory.ContainsKey(categoryId))
                            {
                                tasksByCategory[categoryId] = new List<TodoTask>();
                            }
                            tasksByCategory[categoryId].Add(task);
                        }

                        // Add tasks to categories
                        foreach (var kvp in tasksByCategory)
                        {
                            int categoryId = kvp.Key;
                            var taskList = kvp.Value;

                            // Find or create category
                            TodoCategory? category = categories.FirstOrDefault(c => c.Name == (categoryDict.ContainsKey(categoryId) ? categoryDict[categoryId].Name : $"Category_{categoryId}"));
                            
                            if (category == null)
                            {
                                string categoryName = categoryDict.ContainsKey(categoryId) ? categoryDict[categoryId].Name : $"Category_{categoryId}";
                                category = new TodoCategory { Name = categoryName, Icon = "📁" };
                                categories.Add(category);
                            }

                            foreach (var task in taskList)
                            {
                                if (task.IsCompleted)
                                {
                                    category.CompletedTasks.Add(task);
                                }
                                else
                                {
                                    category.Tasks.Add(task);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Error loading tasks: {ex.Message}");
        }

        return categories;
    }

    /// <summary>
    /// Save a new task to the database
    /// </summary>
    public async Task<bool> SaveTaskAsync(string taskText, string categoryName, bool isCompleted = false, DateTime? dueDate = null, TaskPriority priority = TaskPriority.None)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Get or create category
                int categoryId = await GetOrCreateCategoryAsync(conn, categoryName);

                // Get a default color (assuming color ID 1 exists or create it)
                int colorId = await GetDefaultColorIdAsync(conn);

                // Insert task
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO dbo.tasks (Id, task, IsCompleted, DueDate, Priority, categoryId, colorId, CreatedAt, UpdatedAt) 
                      VALUES (@id, @task, @isCompleted, @dueDate, @priority, @categoryId, @colorId, GETDATE(), GETDATE())", conn))
                {
                    int newId = await GetNextTaskIdAsync(conn);
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@task", taskText);
                    cmd.Parameters.AddWithValue("@isCompleted", isCompleted);
                    cmd.Parameters.AddWithValue("@dueDate", dueDate.HasValue ? (object)dueDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@priority", (int)priority);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@colorId", colorId);

                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            LogError($"Error saving task: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update task completion status
    /// </summary>
    public async Task<bool> UpdateTaskCompletionAsync(string taskText, bool isCompleted, string categoryName)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.tasks 
                      SET IsCompleted = @isCompleted, UpdatedAt = GETDATE() 
                      WHERE task = @task AND categoryId = (SELECT Id FROM dbo.categories WHERE category = @category)", conn))
                {
                    cmd.Parameters.AddWithValue("@isCompleted", isCompleted);
                    cmd.Parameters.AddWithValue("@task", taskText);
                    cmd.Parameters.AddWithValue("@category", categoryName);

                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            LogError($"Error updating task: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete a task from the database
    /// </summary>
    public async Task<bool> DeleteTaskAsync(string taskText, string categoryName)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(
                    @"DELETE FROM dbo.tasks 
                      WHERE task = @task AND categoryId = (SELECT Id FROM dbo.categories WHERE category = @category)", conn))
                {
                    cmd.Parameters.AddWithValue("@task", taskText);
                    cmd.Parameters.AddWithValue("@category", categoryName);

                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            LogError($"Error deleting task: {ex.Message}");
            return false;
        }
    }

    // Helper Methods

    private async Task<int> GetOrCreateCategoryAsync(SqlConnection conn, string categoryName)
    {
        // Try to get existing category
        using (SqlCommand cmd = new SqlCommand("SELECT Id FROM dbo.categories WHERE category = @name", conn))
        {
            cmd.Parameters.AddWithValue("@name", categoryName);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out int id))
            {
                return id;
            }
        }

        // Create new category
        int newCategoryId = await GetNextCategoryIdAsync(conn);
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.categories (Id, category, CreatedAt, UpdatedAt) VALUES (@id, @name, GETDATE(), GETDATE())", conn))
        {
            cmd.Parameters.AddWithValue("@id", newCategoryId);
            cmd.Parameters.AddWithValue("@name", categoryName);
            await cmd.ExecuteNonQueryAsync();
        }

        return newCategoryId;
    }

    private async Task<int> GetDefaultColorIdAsync(SqlConnection conn)
    {
        using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 Id FROM dbo.colors ORDER BY Id", conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out int id))
            {
                return id;
            }
        }

        // Create a default color if none exist
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.colors (Id, color, CreatedAt, UpdatedAt) VALUES (1, '#0078D4', GETDATE(), GETDATE())", conn))
        {
            try { await cmd.ExecuteNonQueryAsync(); } catch { }
        }

        return 1;
    }

    private async Task<int> GetNextTaskIdAsync(SqlConnection conn)
    {
        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.tasks", conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 1;
        }
    }

    private async Task<int> GetNextCategoryIdAsync(SqlConnection conn)
    {
        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.categories", conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 1;
        }
    }

    private static void LogError(string message)
    {
        try
        {
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestDB.log");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { }
    }
}
