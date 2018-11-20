CREATE TABLE [dbo].[YM_Files] (
    [id]                      BIGINT         NULL,
    [file_id]                 BIGINT         NULL,
    [name]                    NVARCHAR (MAX) NULL,
    [description]             NVARCHAR (MAX) NULL,
    [uploader_id]             BIGINT         NULL,
    [uploader_type]           NVARCHAR (100) NULL,
    [group_id]                BIGINT         NULL,
    [group_name]              NVARCHAR (MAX) NULL,
    [reverted_to_id]          BIGINT         NULL,
    [deleted_by_user_id]      BIGINT         NULL,
    [in_private_group]        NVARCHAR (10)  NULL,
    [in_private_conversation] NVARCHAR (10)  NULL,
    [file_api_url]            NVARCHAR (MAX) NULL,
    [download_url]            NVARCHAR (MAX) NULL,
    [path]                    NVARCHAR (MAX) NULL,
    [uploaded_at]             DATETIME       DEFAULT (NULL) NULL,
    [deleted_at]              DATETIME       DEFAULT (NULL) NULL,
    [original_network]        NVARCHAR (300) NULL,
    [processed_by]            NVARCHAR (150) NULL,
    [csvfilename]             NVARCHAR (250) NULL
);

