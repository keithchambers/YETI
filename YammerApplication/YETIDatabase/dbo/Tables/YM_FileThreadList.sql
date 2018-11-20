CREATE TABLE [dbo].[YM_FileThreadList] (
    [Id]          BIGINT         IDENTITY (1, 1) NOT NULL,
    [VersionId]   BIGINT         NULL,
    [FileId]      BIGINT         NULL,
    [FileName]    NVARCHAR (MAX) NULL,
    [ThreadId]    BIGINT         NULL,
    [GroupId]     BIGINT         NULL,
    [GroupName]   NVARCHAR (500) NULL,
    [CsvFileName] NVARCHAR (500) NULL
);

