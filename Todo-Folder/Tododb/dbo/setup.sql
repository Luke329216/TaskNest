
CREATE DATABASE Tododb;
GO

USE Tododb
GO

-- Drop existing tables if they exist
IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL DROP TABLE dbo.tasks;
IF OBJECT_ID(N'dbo.categories', N'U') IS NOT NULL DROP TABLE dbo.categories;
IF OBJECT_ID(N'dbo.colors', N'U') IS NOT NULL DROP TABLE dbo.colors;
IF OBJECT_ID(N'dbo.duedates', N'U') IS NOT NULL DROP TABLE dbo.duedates;
IF OBJECT_ID(N'dbo.priorities', N'U') IS NOT NULL DROP TABLE dbo.priorities;
IF OBJECT_ID(N'dbo.icons', N'U') IS NOT NULL DROP TABLE dbo.icons;
IF OBJECT_ID(N'dbo.themes', N'U') IS NOT NULL DROP TABLE dbo.themes;
IF OBJECT_ID(N'dbo.[accent colors]', N'U') IS NOT NULL DROP TABLE dbo.[accent colors];
IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL DROP TABLE dbo.users;

GO

-- Create colors table
CREATE TABLE [dbo].[colors]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [color] NVARCHAR(255) NOT NULL,
  [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create categories table
CREATE TABLE [dbo].[categories]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [category] NVARCHAR(255) NOT NULL,
  [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create users table
CREATE TABLE [dbo].[users]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [username] NVARCHAR(255) NOT NULL,
  [email] NVARCHAR(255) NOT NULL,
  [password] NVARCHAR(255) NOT NULL,
  [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create tasks table
CREATE TABLE [dbo].[tasks]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [task] NVARCHAR(255) NOT NULL,
  [IsCompleted] BIT NOT NULL DEFAULT 0,
  [DueDate] DATETIME NULL,
  [Priority] NVARCHAR(50) NULL,
  [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [categoryId] INT NOT NULL,
  [colorId] INT NOT NULL,
  FOREIGN KEY (categoryId) REFERENCES [dbo].[categories]([Id]),
  FOREIGN KEY (colorId) REFERENCES [dbo].[colors]([Id])
);

-- Create duedates table
CREATE TABLE [dbo].[duedates]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [dueDate] DATETIME NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

-- Create priorities table
CREATE TABLE [dbo].[priorities]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [priority] NVARCHAR(255) NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

-- Create icons table
CREATE TABLE [dbo].[icons]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [icon] NVARCHAR(255) NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

-- Create themes table
CREATE TABLE [dbo].[themes]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [theme] NVARCHAR(255) NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

-- Create accent colors table
CREATE TABLE [dbo].[accent colors]
(
  [Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
  [accentColor] NVARCHAR(255) NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

-- Insert default color if none exists
INSERT INTO [dbo].[colors] ([color]) VALUES ('#0078D4');

-- Insert General category if none exists
INSERT INTO [dbo].[categories] ([category]) VALUES ('General');

GO


create table [dbo].[text sizes] 
(id INT NOT NULL PRIMARY KEY
  , [textSize] NVARCHAR(255) NOT NULL
  , [taskId] INT NOT NULL
  , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
 
)