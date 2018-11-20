CREATE TABLE [dbo].[YM_DocumentLibraries] (
    [id]            INT            IDENTITY (1, 1) NOT NULL,
    [yammeryear]    NVARCHAR (MAX) NULL,
    [SPLibraryName] NVARCHAR (MAX) NULL,
    [groupcount]    BIGINT         NULL,
    [threadcount]   BIGINT         NULL,
    [capacity]      BIGINT         NULL,
    [created_at]    DATETIME       DEFAULT (getdate()) NULL,
    [processedBy]   NVARCHAR (300) NULL
);

