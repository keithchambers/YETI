CREATE TYPE [dbo].[TBLDeletedThreads] AS TABLE (
    [DocLibname]        NVARCHAR (100) NULL,
    [ThreadID_Filename] NVARCHAR (100) NULL,
    [ThreadID_Size]     BIGINT         NULL,
    [ThreadID_path]     NVARCHAR (100) NULL);

