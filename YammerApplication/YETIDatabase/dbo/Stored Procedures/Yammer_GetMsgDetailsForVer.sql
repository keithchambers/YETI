	Create Procedure [dbo].[Yammer_GetMsgDetailsForVer]
	(		
	@processedBy nvarchar(100)
	)
	as
	BEGIN
	 
	SELECT ROW_NUMBER() OVER (PARTITION BY THREAD_ID ORDER BY CSVFILENAME DESC) Id, * FROM (
    select distinct group_id,group_name,thread_id,csvfilename
	 from YM_Messages 
	 where processed_by = @processedBy AND  process_status = 'FilesGenerated') A

	END

