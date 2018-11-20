CREATE TABLE [dbo].[YM_AzureContainer] (
    [id]                  INT            IDENTITY (1, 1) NOT NULL,
    [ymyear]              INT            NULL,
    [SPLibraryName]       NVARCHAR (MAX) NULL,
    [ContainerName]       NVARCHAR (MAX) NULL,
    [FileContainerUri]    NVARCHAR (MAX) NULL,
    [PackageContainerUri] NVARCHAR (MAX) NULL,
    [ReportingQueueUri]   NVARCHAR (MAX) NULL,
    [PackageGUID]         NVARCHAR (MAX) NULL,
    [BatchNo]             INT            NULL,
    [JobStatus]           NVARCHAR (MAX) NULL,
    [PackageStage]        NVARCHAR (MAX) NULL,
    [processedBy]         NVARCHAR (300) NULL,
    [sourceFolderPath]    NVARCHAR (MAX) NULL,
    [threadcount]         BIGINT         NULL,
    [created_at]          DATETIME       DEFAULT (getdate()) NULL
);

