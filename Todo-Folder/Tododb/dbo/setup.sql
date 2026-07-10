

CREATE DATABASE Tododb;
GO

USE Tododb
GO

IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL DROP TABLE dbo.tasks;
IF OBJECT_ID(N'dbo.categories', N'U') IS NOT NULL DROP TABLE dbo.categories;
IF OBJECT_ID(N'dbo.colors', N'U') IS NOT NULL DROP TABLE dbo.colors;
GO

CREATE TABLE [dbo].[colors]
(
  [Id] INT NOT NULL PRIMARY KEY,
  [color] NVARCHAR(255) NOT NULL,
  [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE [dbo].[categories]
(
  [Id] INT NOT NULL PRIMARY KEY
  , [task] NVARCHAR(255) NOT NULL
  , [IsCompleted] BIT NOT NULL DEFAULT 0
  , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  , [categoryId] INT NOT NULL
  , FOREIGN KEY (categoryId) REFERENCES [dbo].[categories]([Id])
  , [colorID] INT NOT NULL
  , FOREIGN KEY (colorID) REFERENCES [dbo].[colors]([Id])
);
IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
  CREATE TABLE [dbo].[users]
  (
    [Id] INT NOT NULL PRIMARY KEY,
    [username] NVARCHAR(255) NOT NULL,
    [email] NVARCHAR(255) NOT NULL,
    [password] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  );
END;

CREATE TABLE [dbo].[tasks]
(
  [Id] INT NOT NULL PRIMARY KEY,
  [title] NVARCHAR(255) NOT NULL,
  [description] NVARCHAR(MAX) NULL,
  [IsCompleted] BIT NOT NULL DEFAULT 0,
  [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
  [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
);

IF OBJECT_ID(N'dbo.duedates', N'U') IS NOT NULL DROP TABLE dbo.duedates;
CREATE TABLE [dbo].[duedates]
(
  [Id] INT NOT NULL PRIMARY KEY,
  [dueDate] DATETIME NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

IF OBJECT_ID(N'dbo.priorities', N'U') IS NOT NULL DROP TABLE dbo.priorities;
CREATE TABLE [dbo].[priorities]
(
  [Id] INT NOT NULL PRIMARY KEY,
  [priority] NVARCHAR(255) NOT NULL,
  [taskId] INT NOT NULL,
  FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
);

IF OBJECT_ID(N'dbo.icons', N'U') IS NULL
BEGIN
  create table [dbo].[icons]
  (
    [Id] INT NOT NULL PRIMARY KEY
    , [icon] NVARCHAR(255) NOT NULL
    , [taskId] INT NOT NULL
    , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
  )
END
create table [dbo].[themes]
(
  [Id] INT NOT NULL PRIMARY KEY
  , [theme] NVARCHAR(255) NOT NULL
  , [taskId] INT NOT NULL
  , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
)

create table [dbo].[accent colors]
(
  [Id] INT NOT NULL PRIMARY KEY
  , [accentColor] NVARCHAR(255) NOT NULL
  , [taskId] INT NOT NULL
  , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
)

create table [dbo].[text sizes] 
(id INT NOT NULL PRIMARY KEY
  , [textSize] NVARCHAR(255) NOT NULL
  , [taskId] INT NOT NULL
  , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
 
)