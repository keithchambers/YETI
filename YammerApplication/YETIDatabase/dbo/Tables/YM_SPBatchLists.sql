CREATE TABLE [dbo].[YM_SPBatchLists] (
    [BatchNo]       INT            IDENTITY (1, 1) NOT NULL,
    [YmYear]        INT            NOT NULL,
    [GroupName]     NVARCHAR (300) NULL,
    [GroupType]     NVARCHAR (30)  NOT NULL,
    [FolderPath]    NVARCHAR (300) NOT NULL,
    [ItemCount]     INT            NULL,
    [status]        NVARCHAR (300) NULL,
    [parentBatchNo] INT            NULL,
    [processedBy]   NVARCHAR (300) NULL,
    [SPMappingId]   INT            NOT NULL,
    [TimesTried]    INT            NULL
);

