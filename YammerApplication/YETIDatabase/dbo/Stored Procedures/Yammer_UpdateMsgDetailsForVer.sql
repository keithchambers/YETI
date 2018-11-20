	Create Procedure [dbo].[Yammer_UpdateMsgDetailsForVer]
	(	
	@in_nvarchar_Thread_Id nvarchar(max),	
	@processedBy nvarchar(100)
	)
	as
	BEGIN
	 
	UPDATE YM_Messages 
	SET process_status = 'PageDownloaded' 
	where processed_by = @processedBy
	AND thread_id = @in_nvarchar_Thread_Id

	END

