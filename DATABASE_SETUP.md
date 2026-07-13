# TaskNest Database Setup Guide

## Overview
TaskNest now integrates with SQL Server for persistent data storage. This guide will help you set up the database and connect the app.

## Prerequisites
- **SQL Server** (2019 or later, or SQL Server Express)
- **SQL Server Management Studio (SSMS)** or **Azure Data Studio** (optional, for manual database setup)
- **.NET 10.0** (for building the app)

## Database Setup

### Option 1: Using SQL Server Management Studio (SSMS)

1. **Open SSMS** and connect to your SQL Server instance
2. **Open a New Query** window
3. **Copy and paste** the entire contents of `Todo-Folder/Tododb/dbo/setup.sql`
4. **Execute** the script (F5 or click Execute)
5. The database `Tododb` will be created with all necessary tables

### Option 2: Using Command Line (sqlcmd)

```powershell
cd "c:\Work\Repository\TaskNest"
sqlcmd -S localhost -i "Todo-Folder\Tododb\dbo\setup.sql"
```

### Option 3: Using Azure Data Studio

1. Open Azure Data Studio
2. Connect to your SQL Server
3. File → Open File → Select `setup.sql`
4. Click Execute or press Ctrl+Shift+E

## Connection Configuration

The app uses **Windows Authentication** by default. Update the connection string in [DatabaseService.cs](Dev/TaskNestUI/DatabaseService.cs) if needed:

```csharp
public DatabaseService(string server = "localhost", string database = "Tododb")
{
    _connectionString = $"Server={server};Database={database};Integrated Security=true;...";
}
```

### Modify Server/Database Name

If your SQL Server is on a different machine or uses a different name:

```csharp
// In MainWindow.xaml.cs constructor:
_dbService = new DatabaseService("YOUR_SERVER_NAME", "Tododb");
```

## Features Implemented

✅ **Save Tasks** - Tasks are automatically saved when created  
✅ **Load Tasks** - All tasks load from the database on app startup  
✅ **Mark Complete** - Completion status is saved to the database  
✅ **Delete Tasks** - Deletions are persisted to the database  
✅ **Categories** - Tasks are organized by categories (stored in DB)  
✅ **Due Dates & Priority** - Stored and loaded from the database  

## Troubleshooting

### "Database connection failed" message
- **Check SQL Server**: Ensure SQL Server is running
- **Verify Connection String**: Check server name and database name
- **Firewall**: Ensure port 1433 is open (if SQL Server is remote)
- **Logs**: Check `%TEMP%\TaskNestDB.log` for detailed error messages

### Database not found
- Run the setup.sql script again
- Verify the database name is correct
- Check that the script executed without errors

### Tables not found
- Ensure all tables were created by the setup.sql script
- Run the script again if needed
- Verify you're connected to the correct database

## Application Usage

1. **Run the app**: `dotnet run` from the TaskNestUI folder
2. **Status messages**: Watch for connection status at the bottom of the window
3. **Add tasks**: Type in the input box and press Enter (automatically saved)
4. **Complete tasks**: Check the checkbox next to a task
5. **Delete tasks**: Click the ✕ button (stored in database)
6. **Organize**: Right-click categories to add/manage them

## Database Schema

### Main Tables

- **categories** - Task categories
- **tasks** - Task items with status, due dates, and priorities
- **colors** - Color definitions for tasks
- **users** - User information (for future features)

See `Todo-Folder/Tododb/dbo/setup.sql` for full schema details.

## Development Notes

The database service is implemented in [DatabaseService.cs](Dev/TaskNestUI/DatabaseService.cs):

- **LoadTasksAsync()** - Fetches all tasks on app startup
- **SaveTaskAsync()** - Saves new tasks
- **UpdateTaskCompletionAsync()** - Updates task completion status
- **DeleteTaskAsync()** - Removes tasks from database

## Support

For issues or questions about the database setup, check:
1. The log file at `%TEMP%\TaskNestDB.log`
2. SQL Server error logs
3. The MainWindow status messages (shown at bottom of app)
