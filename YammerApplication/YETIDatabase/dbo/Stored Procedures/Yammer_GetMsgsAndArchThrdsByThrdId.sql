	CREATE Procedure [dbo].[Yammer_GetMsgsAndArchThrdsByThrdId]
	(	
	@in_nvarchar_thread_id nvarchar(max)
	)
	as
	BEGIN
		select distinct * 
		from YM_Messages (NOLOCK) 
		where thread_id = (@in_nvarchar_thread_id) or (id =@in_nvarchar_thread_id) 
		order by created_at ; 

		select * 

		from YM_ArchivedThreads 

		where thread_id =  @in_nvarchar_thread_id;
	END

