CREATE PROCEDURE Yammer_DownloadThreadStatus
(--@THREAD_IDS NVARCHAR(MAX),
@THREAD_IDS dbo.IDTableType READONLY,@NEW_STATUS NVARCHAR(MAX),@PREV_STATUS NVARCHAR(MAX),@processedBy NVARCHAR(MAX))
AS
BEGIN
	--EXEC('
	--UPDATE YM_Messages SET process_status = '''+@NEW_STATUS
	--+''' WHERE process_status IN ('+@PREV_STATUS+') and processed_by = '''
	--+@processedBy+''' AND thread_id IN ('+@THREAD_IDS+')')

	--EXEC('
	--UPDATE YM_ExportDetails SET Status = '''+@NEW_STATUS+''' WHERE ProcessStage = ''CSVProcessing'' AND
	--Status IN ('+@PREV_STATUS+') and ProcessedBy = '''+@processedBy+''' and FileName in 
	--(select distinct csvfilename 
	--		from YM_Messages 
	--	where ISNULL(process_status,'''') = '''+@NEW_STATUS+''' 
	--and processed_by = '''+@processedBy+''' AND thread_id IN ('+@THREAD_IDS+'))')
	
	Update YM_Messages  SET process_status = @NEW_STATUS 
	where process_status IN (@PREV_STATUS) and processed_by =@processedBy
	and thread_id in (select ID from @THREAD_IDS)

	UPDATE YM_ExportDetails SET Status = @NEW_STATUS WHERE ProcessStage = 'CSVProcessing' AND
	Status IN (@PREV_STATUS) and ProcessedBy = @processedBy and FileName in 
	(select distinct csvfilename 
			from YM_Messages 
		where ISNULL(process_status,'') = @NEW_STATUS
	and processed_by = @processedBy AND thread_id IN (select ID from @THREAD_IDS))
END
