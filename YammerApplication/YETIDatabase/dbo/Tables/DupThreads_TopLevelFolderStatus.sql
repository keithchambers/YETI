CREATE TABLE [dbo].[DupThreads_TopLevelFolderStatus] (
    [DocLibName]       VARCHAR (50)   NULL,
    [TopLevelFolderSP] NVARCHAR (100) NULL,
    [ProcessStage]     NVARCHAR (50)  NULL,
    [ID]               INT            IDENTITY (1, 1) NOT NULL,
    [OrderID]          INT            NULL
);

