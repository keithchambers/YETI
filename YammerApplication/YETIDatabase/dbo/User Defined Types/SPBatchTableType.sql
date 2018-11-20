CREATE TYPE [dbo].[SPBatchTableType] AS TABLE (
    [FolderPath] NVARCHAR (300) NULL,
    [ItemCount]  INT            NULL,
    [GroupName]  NVARCHAR (300) NULL,
    [GroupType]  NVARCHAR (30)  NULL,
    [FileName]   NVARCHAR (300) NULL);

