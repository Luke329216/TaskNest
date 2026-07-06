CREATE TABLE [dbo].[tasks]
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
) 

create table [dbo].[categories]
(
  [Id] INT NOT NULL PRIMARY KEY
  , [category] NVARCHAR(255) NOT NULL
  , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
)


create table [dbo].[colors]
(
  [Id] INT NOT NULL PRIMARY KEY
  , [color] NVARCHAR(255) NOT NULL
  , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
)
create table [dbo].[users]
(
  [Id] INT NOT NULL PRIMARY KEY
  , [username] NVARCHAR(255) NOT NULL
  , [email] NVARCHAR(255) NOT NULL
  , [password] NVARCHAR(255) NOT NULL
  , [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
  , [UpdatedAt] DATETIME NOT NULL DEFAULT GETDATE()
)
