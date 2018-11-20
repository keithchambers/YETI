CREATE TABLE [dbo].[YM_Topics] (
    [id]           BIGINT         NULL,
    [name]         NVARCHAR (MAX) NULL,
    [created_by]   NVARCHAR (300) NULL,
    [created_at]   DATETIME       NULL,
    [api_url]      NVARCHAR (MAX) NULL,
    [processed_by] NVARCHAR (150) NULL,
    [csvfilename]  NVARCHAR (250) NULL
);

