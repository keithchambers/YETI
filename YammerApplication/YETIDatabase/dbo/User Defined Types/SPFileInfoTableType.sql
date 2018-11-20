CREATE TYPE [dbo].[SPFileInfoTableType] AS TABLE (
    [DocLibName]         NVARCHAR (100) NULL,
    [TopLevelSubFolder]  NVARCHAR (100) NULL,
    [Thread_FileName]    NVARCHAR (MAX) NULL,
    [Thread_FileSize]    INT            NULL,
    [Thread_CreatedDate] DATETIME       NULL,
    [Thread_FilePath]    NVARCHAR (100) NULL);

