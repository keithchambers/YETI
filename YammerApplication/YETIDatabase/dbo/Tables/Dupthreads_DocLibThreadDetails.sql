CREATE TABLE [dbo].[Dupthreads_DocLibThreadDetails] (
    [DocLibName]           NVARCHAR (MAX) NULL,
    [TopLevelFolderSP]     NVARCHAR (MAX) NULL,
    [ThreadID]             NVARCHAR (MAX) NULL,
    [ThreadID_FileName]    NVARCHAR (MAX) NULL,
    [ThreadID_Size]        BIGINT         NULL,
    [ThreadID_CreatedDate] DATETIME       NULL,
    [ThreadID_Path]        NVARCHAR (MAX) NULL,
    [TobeDeleted]          BIT            NULL,
    [Deleted]              BIT            NULL,
    [ID]                   INT            IDENTITY (1, 1) NOT NULL,
    [ProcessedBy]          NVARCHAR (50)  NULL,
    [NeedMoreAnalysis]     INT            NULL
);

