CREATE TABLE [dbo].[YM_SPDirectoryMapping] (
    [id]              INT            IDENTITY (1, 1) NOT NULL,
    [UploadStartDate] DATETIME       NOT NULL,
    [UploadEndDate]   DATETIME       NOT NULL,
    [FolderPath]      NVARCHAR (300) NOT NULL,
    [ymYear]          INT            NOT NULL,
    [Status]          NVARCHAR (100) NULL,
    [ThreadCount]     BIGINT         NULL,
    [ProcessedBy]     NVARCHAR (100) NULL,
    [CreatedDate]     DATETIME       DEFAULT (getdate()) NOT NULL,
    [ModifiedDate]    DATETIME       NULL
);

