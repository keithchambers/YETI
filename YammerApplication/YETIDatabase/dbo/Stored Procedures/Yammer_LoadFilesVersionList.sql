CREATE PROCEDURE Yammer_LoadFilesVersionList (@processedBy nvarchar(100),@FILTER nvarchar(100))
AS
BEGIN
	DECLARE @NOTFILTER NVARCHAR(100)
	IF(@FILTER = 'page')
	SET @NOTFILTER = 'uploadedfile'
	ELSE
	SET @NOTFILTER = 'page'
	declare @TEMP_TABLE table
	( 
	ids nvarchar(max),
	thread_id nvarchar(max),
	group_id nvarchar(max),
	group_name nvarchar(max),
	CsvFileName nvarchar(max)
	)

	;with cte
	as
	(
	select replace(attachments, @FILTER+':','') ids,thread_id,group_id,group_name,CsvFileName from YM_Messages where (process_status = 'PageMapped'  or process_status = 'FileVersionsLoaded')
	and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%'

	)
	insert into @TEMP_TABLE
	select * from cte

	declare @temtable table
	(file_id nvarchar(max),thread_id nvarchar(max),group_id nvarchar(max),group_name nvarchar(max),CsvFileName nvarchar(max))
	insert into @temtable
	select ids as File_Id,thread_id,group_id,group_name,CsvFileName  from @TEMP_TABLE WHERE ids = CASE WHEN (CHARINDEX(',',ids,0) =  0 ) THEN ids ELSE '1' END
	union  
	select * from (
	SELECT 		 rtrim(LTRIM(Split.a.value('.', 'NVARCHAR(MAX)'))) AS File_Id,thread_id,group_id,group_name,CsvFileName 
			FROM  (SELECT CAST ('<M>' + REPLACE(ids, ',', '</M><M>') + '</M>' AS XML) AS fileId, thread_id,group_id,group_name,CsvFileName  
				FROM  @TEMP_TABLE WHERE ids = CASE WHEN (CHARINDEX(',',ids,0) >  0 ) THEN ids ELSE '1' END) AS A CROSS APPLY fileId.nodes ('/M') AS Split(a)
	) a where File_Id not like '%opengraphobject%' and File_Id not like '%ymodule%' and File_Id not like '%'+@NOTFILTER+'%'	
	order by ids
	if(@NOTFILTER = 'Page')
	begin
		insert into YM_FileThreadList			
		select distinct yf.id versionid,t.file_id,yf.name,t.thread_id,t.group_id,t.group_name,t.CsvFileName from @temtable t join ym_files yf on t.file_id = yf.file_id 
		where yf.csvfilename in (select CsvFileName from YM_Messages where process_status = 'PageMapped'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%')
		order by file_id

		insert into YM_FileThreadList (VersionId,fileid,filename,groupid,groupname,csvfilename)	
		select id,file_id,name,group_id,group_name,csvfilename from ym_files where csvfilename in (select CsvFileName from YM_Messages where process_status = 'PageMapped'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%')
		except
		select versionId,fileId,FileName,GroupId,GroupName,csvfilename from YM_FileThreadList where csvfilename in (select CsvFileName from YM_Messages where process_status = 'PageMapped'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%')
	end
	else
	begin
		insert into YM_PageThreadList		
		select distinct yp.id versionid,t.file_id,yp.name+'.html' as name,t.thread_id,t.group_id,t.group_name,t.CsvFileName from @temtable t join ym_pages yp on t.file_id = yp.page_id 
		where yp.csvfilename in (select CsvFileName from YM_Messages where process_status = 'FileVersionsLoaded'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%')
		order by file_id

		insert into YM_PageThreadList (VersionId,pageid,pagename,groupid,groupname,csvfilename)		
		select id,page_id,name,group_id,group_name,csvfilename from ym_pages where csvfilename in (select CsvFileName from YM_Messages where process_status = 'FileVersionsLoaded'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%')
		except
		select versionId,PageId,PageName,GroupId,GroupName,csvfilename from YM_PageThreadList where csvfilename in (select CsvFileName from YM_Messages where process_status = 'FileVersionsLoaded'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@FILTER+':'+'%')
	end
END
