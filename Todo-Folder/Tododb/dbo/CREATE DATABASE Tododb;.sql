
CREATE DATABASE [Tododb];
GO
USE [Tododb];
GO

IF OBJECT_ID(N'dbo.categories', N'U') IS NULL
BEGIN
  create table [dbo].[categories]
  (
    [Id] INT NOT NULL PRIMARY KEY
    , [category] NVARCHAR(255) NOT NULL
    , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  )
END


IF OBJECT_ID(N'dbo.colors', N'U') IS NULL
BEGIN
  create table [dbo].[colors]
  (
    [Id] INT NOT NULL PRIMARY KEY
    , [color] NVARCHAR(255) NOT NULL
    , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  )

END

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
  create table [dbo].[users]
  (
    [Id] INT NOT NULL PRIMARY KEY
    , [username] NVARCHAR(255) NOT NULL
    , [email] NVARCHAR(255) NOT NULL
    , [password] NVARCHAR(255) NOT NULL
    , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
    , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  )
END

IF OBJECT_ID(N'dbo.duedates', N'U') IS NULL
BEGIN
  create table [dbo].[duedates]
  (
    [Id] INT NOT NULL PRIMARY KEY
    , [dueDate] DATETIME NOT NULL
    , [taskId] INT NOT NULL
    , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
  )
END

IF OBJECT_ID(N'dbo.priorities', N'U') IS NULL
BEGIN
  create table [dbo].[priorities]
  (
    [Id] INT NOT NULL PRIMARY KEY
    , [priority] NVARCHAR(255) NOT NULL
    , [taskId] INT NOT NULL
    , FOREIGN KEY (taskId) REFERENCES [dbo].[tasks]([Id])
  )
END

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

