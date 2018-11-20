CREATE TABLE [dbo].[YM_ArchivedThreadsBuffer] (
    [id]           INT      IDENTITY (1, 1) NOT NULL,
    [Thread_id]    BIGINT   NULL,
    [Created_date] DATETIME DEFAULT (getdate()) NULL
);


GO
CREATE CLUSTERED INDEX [ClusteredIndexRepoBuffer-20170613-110117]
    ON [dbo].[YM_ArchivedThreadsBuffer]([Thread_id] ASC);

