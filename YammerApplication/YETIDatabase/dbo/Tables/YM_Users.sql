CREATE TABLE [dbo].[YM_Users] (
    [id]                BIGINT         NULL,
    [name]              NVARCHAR (MAX) NULL,
    [email]             NVARCHAR (MAX) NULL,
    [job_title]         NVARCHAR (MAX) NULL,
    [location]          NVARCHAR (MAX) NULL,
    [department]        NVARCHAR (MAX) NULL,
    [api_url]           NVARCHAR (MAX) NULL,
    [deleted_by_id]     BIGINT         NULL,
    [deleted_by_type]   NVARCHAR (MAX) NULL,
    [joined_at]         DATETIME       NULL,
    [deleted_at]        DATETIME       NULL,
    [suspended_by_id]   BIGINT         NULL,
    [suspended_by_type] NVARCHAR (MAX) NULL,
    [suspended_at]      DATETIME       NULL,
    [guid]              NVARCHAR (MAX) NULL,
    [state]             NVARCHAR (MAX) NULL,
    [processed_by]      NVARCHAR (150) NULL,
    [csvfilename]       NVARCHAR (250) NULL
);

