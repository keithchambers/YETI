CREATE PROCEDURE [dbo].[Yammer_GetCopyFromArchiveRepo]
AS
BEGIN
	DECLARE @Thread_Table table
	( thread_id bigint )

	insert into @Thread_Table
	select thread_id from YM_Messages

	INSERT INTO YM_ArchivedThreads WITH (TABLOCKX) (Thread_id,ThreadXMLContent,Created_Date,Modified_Date)

	select Thread_id,ThreadXMLContent,Created_Date,Modified_Date from YM_ArchivedThreadsRepo where Thread_id in (
	select thread_id from @Thread_Table
	EXCEPT 
	select Thread_id from YM_ArchivedThreads where Thread_id in (select thread_id from @Thread_Table))

	DELETE FROM YM_ArchivedThreadsRepo where 
	Thread_id in (select thread_id from @Thread_Table)
END
