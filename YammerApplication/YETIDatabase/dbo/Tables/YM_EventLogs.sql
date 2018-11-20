CREATE TABLE [dbo].[YM_EventLogs] (
    [id]               BIGINT         IDENTITY (1, 1) NOT NULL,
    [Source]           NVARCHAR (100) NULL,
    [EventType]        NVARCHAR (100) NULL,
    [FileName]         NVARCHAR (500) NULL,
    [ErrorDescription] NVARCHAR (MAX) NULL,
    [CreatedDate]      DATETIME       NULL,
    [ProcessedBy]      NVARCHAR (300) NULL
);

