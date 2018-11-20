CREATE PROCEDURE [dbo].[Yammer_DownloadAttachments] (@FILTER NVARCHAR(100),@processedBy NVARCHAR(150))
AS
BEGIN
declare @TEMP_TABLE table
		( 
		ids nvarchar(max),
		threadId nvarchar(max)
		)
	IF(@FILTER = 'uploadedfile')
	BEGIN
		;with cte
		as
		(
		select replace(attachments, @FILTER+':','') ids,thread_id from YM_Messages where process_status in ('UsersLoaded','FileMapped','PageMapped','FileVersionsLoaded','PageVersionsLoaded','FilesRenamed','PagesRenamed') and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%'
		)
		insert into @TEMP_TABLE
		select * from cte

		select ids as File_Id,threadId  from @TEMP_TABLE WHERE ids = CASE WHEN (CHARINDEX(',',ids,0) =  0 ) THEN ids ELSE '1' END
		union 
		select * from (
		SELECT 		 rtrim(LTRIM(Split.a.value('.', 'NVARCHAR(MAX)'))) AS File_Id,threadId 
			 FROM  (SELECT CAST ('<M>' + REPLACE(ids, ',', '</M><M>') + '</M>' AS XML) AS UserId,threadId  
				 FROM  @TEMP_TABLE WHERE ids = CASE WHEN (CHARINDEX(',',ids,0) >  0 ) THEN ids ELSE '1' END) AS A CROSS APPLY UserId.nodes ('/M') AS Split(a)
		) a where File_Id not like '%opengraphobject%' and File_Id not like '%ymodule%' and File_Id not like '%page%'	
	order by ids
	END	
	ELSE
	BEGIN
		;with cte
		as
		(
		select replace(attachments, @FILTER+':','') ids,thread_id from YM_Messages where process_status in ('UsersLoaded','FileMapped','PageMapped','FileVersionsLoaded','PageVersionsLoaded','FilesRenamed','PagesRenamed','FilesDownloaded') and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%'
		)
		insert into @TEMP_TABLE
		select * from cte

		select ids as File_Id,threadId  from @TEMP_TABLE WHERE ids = CASE WHEN (CHARINDEX(',',ids,0) =  0 ) THEN ids ELSE '1' END
		union 
		select * from (
		SELECT 		 rtrim(LTRIM(Split.a.value('.', 'NVARCHAR(MAX)'))) AS File_Id,threadId 
			 FROM  (SELECT CAST ('<M>' + REPLACE(ids, ',', '</M><M>') + '</M>' AS XML) AS UserId,threadId  
				 FROM  @TEMP_TABLE WHERE ids = CASE WHEN (CHARINDEX(',',ids,0) >  0 ) THEN ids ELSE '1' END) AS A CROSS APPLY UserId.nodes ('/M') AS Split(a)
		) a where File_Id not like '%opengraphobject%' and File_Id not like '%ymodule%' and File_Id not like '%uploadedfile%'	
	order by ids
	END		
END
