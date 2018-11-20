CREATE TABLE [dbo].[YM_ExportDetails] (
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [StartDate]     DATETIME       NULL,
    [EndDate]       DATETIME       NULL,
    [IsUploaded]    BIT            DEFAULT ((0)) NULL,
    [FileName]      NVARCHAR (MAX) NULL,
    [Status]        NVARCHAR (50)  NULL,
    [ProcessStage]  NVARCHAR (100) NULL,
    [ProcessedBy]   NVARCHAR (100) NULL,
    [CreatedDate]   DATETIME       DEFAULT (getdate()) NULL,
    [UploadedDate]  DATETIME       NULL,
    [ModifiedDate]  DATETIME       NULL,
    [TimesTried]    INT            NULL,
    [MessagesCount] BIGINT         NULL,
    [FilesCount]    BIGINT         NULL,
    [PagesCount]    BIGINT         NULL,
    [GroupsCount]   BIGINT         NULL,
    [TopicsCount]   BIGINT         NULL,
    [UsersCount]    BIGINT         NULL,
    [IsVerified]    BIT            DEFAULT ((0)) NULL
);

