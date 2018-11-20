CREATE TABLE [dbo].[YM_Pages] (
    [id]                    BIGINT         NULL,
    [page_id]               BIGINT         NULL,
    [creator_user_id]       BIGINT         NULL,
    [published_by_user_id]  BIGINT         NULL,
    [collaborator_user_ids] NVARCHAR (MAX) NULL,
    [name]                  NVARCHAR (MAX) NULL,
    [group_id]              BIGINT         NULL,
    [group_name]            NVARCHAR (MAX) NULL,
    [reverted_to_id]        BIGINT         NULL,
    [deleted_by_id]         BIGINT         NULL,
    [deleted_by_type]       NVARCHAR (100) NULL,
    [in_private_group]      NVARCHAR (10)  NULL,
    [page_api_url]          NVARCHAR (MAX) NULL,
    [download_url]          NVARCHAR (MAX) NULL,
    [path]                  NVARCHAR (MAX) NULL,
    [published_at]          DATETIME       NULL,
    [deleted_at]            DATETIME       NULL,
    [processed_by]          NVARCHAR (150) NULL,
    [csvfilename]           NVARCHAR (250) NULL
);

