CREATE TABLE [dbo].[ym_yammeryears] (
    [id]            INT            IDENTITY (1, 1) NOT NULL,
    [ymyear]        INT            NULL,
    [sequence]      INT            NULL,
    [ToDownload]    BIT            DEFAULT ((1)) NULL,
    [ToProcess]     BIT            DEFAULT ((0)) NULL,
    [ToCompress]    BIT            DEFAULT ((0)) NULL,
    [ToRun]         BIT            NULL,
    [ProcessedBy]   NVARCHAR (100) NULL,
    [ToSPUpload]    BIT            NULL,
    [ToDeDuplicate] BIT            NULL,
    PRIMARY KEY CLUSTERED ([id] ASC)
);

