CREATE TABLE [dbo].[tempAzure] (
    [id]               INT            IDENTITY (1, 1) NOT NULL,
    [ymyear]           INT            NULL,
    [SPLibraryName]    NVARCHAR (MAX) NULL,
    [ContainerName]    NVARCHAR (MAX) NULL,
    [PakcageGUID]      NVARCHAR (MAX) NULL,
    [status]           NVARCHAR (MAX) NULL,
    [processedBy]      NVARCHAR (300) NULL,
    [sourceFolderPath] NVARCHAR (MAX) NULL,
    [threadcount]      BIGINT         NULL,
    [created_at]       DATETIME       NULL
);

