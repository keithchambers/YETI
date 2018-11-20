CREATE TABLE [dbo].[YM_ArchivedThreadsBak] (
    [id]               INT      IDENTITY (1, 1) NOT NULL,
    [Thread_id]        BIGINT   NULL,
    [ThreadXMLContent] XML      NULL,
    [Created_date]     DATETIME NULL,
    [Modified_date]    DATETIME NULL
);

