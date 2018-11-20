
CREATE PROCEDURE Yammer_GetListToMapNewVersionFiles (@processedBy nvarchar(100),@FILTER nvarchar(100))
AS
BEGIN
	declare @FileTemp_Table table
	(
	VerCount int,
	versionid bigint,
	file_id bigint,
	filename nvarchar(max)
	)
	if(@filter = 'uploadedfile')
	begin
		insert into @FileTemp_Table
		select ROW_NUMBER() OVER (PARTITION BY File_Id
		ORDER BY versionid desc) VerCount, * from (
		select yf.id versionid,file_id,name
		from ym_files yf join YM_FileThreadList ft	on ft.fileid = yf.file_id	
		where yf.csvfilename in (select CsvFileName from YM_Messages where process_status = 'UsersLoaded' 
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@filter+':'+'%')
		UNION
		select ft.versionid,file_id,name
		from ym_files yf join YM_FileThreadList ft	on ft.fileid = yf.file_id	
		where yf.csvfilename in (select CsvFileName from YM_Messages where process_status = 'UsersLoaded'  
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@filter+':'+'%')) a

		select distinct FTT.*,FTL.ThreadId,FTL.GroupId,FTL.GroupName from @FileTemp_Table FTT Join YM_FileThreadList FTL
		on FTT.file_id = FTL.fileid
	end
	else
	begin
		insert into @FileTemp_Table
		select ROW_NUMBER() OVER (PARTITION BY page_id
		ORDER BY versionid desc) VerCount, * from (
		select yf.id versionid,page_id,name
		from ym_pages yf join YM_PageThreadList ft	on ft.pageid = yf.page_id	
		where yf.csvfilename in (select CsvFileName from YM_Messages where process_status = 'FileMapped'
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@filter+':'+'%')
		UNION
		select ft.versionid,page_id,name 
		from ym_pages yf join YM_PageThreadList ft	on ft.pageid = yf.page_id	
		where yf.csvfilename in (select CsvFileName from YM_Messages where process_status = 'FileMapped' 
		and  attachments is not null and processed_by = @processedBy and attachments like '%'+@filter+':'+'%')) a

		select distinct FTT.*,FTL.ThreadId,FTL.GroupId,FTL.GroupName from @FileTemp_Table FTT Join YM_PageThreadList FTL
		on FTT.file_id = FTL.pageid order by file_id,VerCount
	end
END
