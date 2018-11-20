CREATE TABLE [dbo].[YM_PageThreadList] (
    [Id]          BIGINT         IDENTITY (1, 1) NOT NULL,
    [VersionId]   BIGINT         NULL,
    [PageId]      BIGINT         NULL,
    [PageName]    NVARCHAR (MAX) NULL,
    [ThreadId]    BIGINT         NULL,
    [GroupId]     BIGINT         NULL,
    [GroupName]   NVARCHAR (500) NULL,
    [CsvFileName] NVARCHAR (500) NULL
);

