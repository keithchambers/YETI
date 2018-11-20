	Create Procedure [dbo].[Yammer_DeleteArchThread]
	(	
	@in_nvarchar_thread_id nvarchar(max)
	)
	as
	BEGIN
	delete from YM_ArchivedThreads where thread_id = @in_nvarchar_thread_id;

	END

