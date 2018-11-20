CREATE PROCEDURE Yammer_LogMissedAttachments (@attachmentType nvarchar(100), @fileId bigint, @threadId nvarchar(100))
AS
BEGIN
	IF(@attachmentType = 'uploadedfile')
	BEGIN		
		;with cte
		as
		(
		select top 1 file_id,name,group_id,group_name,csvfilename,uploaded_at,deleted_at from YM_Files where file_id = @fileId order by id desc
		)
		insert into YM_MissedFileAttachments (
		fileId, 
		fileName,
		threadId,
		groupId,
		groupName,
		csvfilename,
		status ,
		timesTried,
		UploadedAt,
		DeletedAt)
		select distinct cte.file_id,cte.name,ftl.ThreadId,cte.group_id,cte.group_name,cte.csvfilename,NULL,0,cte.uploaded_at,cte.deleted_at 
		from cte join YM_FileThreadList FTL on cte.file_id = ftl.FileId 

		IF( @@ROWCOUNT = 0 )
		BEGIN
			insert into YM_MissedFileAttachments (
			fileId, 
			fileName,
			threadId,
			groupId,
			groupName,
			csvfilename,
			status ,
			timesTried,
			UploadedAt,
			DeletedAt)
			select distinct @fileId,NULL,thread_id,group_id,group_name,csvfilename,NULL,0,NULL,NULL 
			from YM_Messages where thread_id = @threadId
		END

	END
	ELSE
	BEGIN
		;with cte
		as
		(
		select top 1 page_id,name,group_id,group_name,csvfilename,published_at,deleted_at from YM_Pages where page_id = @fileId order by id desc
		)
		insert into YM_MissedPageAttachments (
		pageId, 
		pageName,
		threadId,
		groupId,
		groupName,
		csvfilename,
		status ,
		timesTried,
		UploadedAt,
		DeletedAt)
		select distinct cte.page_id,cte.name,ftl.ThreadId,cte.group_id,cte.group_name,cte.csvfilename,NULL,0,cte.published_at,cte.deleted_at 
		from cte join YM_PageThreadList FTL on cte.page_id = ftl.PageId 

		IF( @@ROWCOUNT = 0 )
		BEGIN
			insert into YM_MissedPageAttachments (
			pageId, 
			pageName,
			threadId,
			groupId,
			groupName,
			csvfilename,
			status ,
			timesTried,
			UploadedAt,
			DeletedAt)
			select distinct @fileId,NULL,thread_id,group_id,group_name,csvfilename,NULL,0,NULL,NULL 
			from YM_Messages where thread_id = @threadId
		END
	END
END
