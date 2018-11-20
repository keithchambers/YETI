CREATE TABLE [dbo].[YM_ArchivedThreadsRepo] (
    [id]               INT      IDENTITY (1, 1) NOT NULL,
    [Thread_id]        BIGINT   NULL,
    [ThreadXMLContent] XML      NULL,
    [Created_date]     DATETIME DEFAULT (getdate()) NULL,
    [Modified_date]    DATETIME NULL,
    [Year]             INT      NULL
);


GO
CREATE CLUSTERED INDEX [ClusteredIndex-20170613-110117]
    ON [dbo].[YM_ArchivedThreadsRepo]([Thread_id] ASC);

