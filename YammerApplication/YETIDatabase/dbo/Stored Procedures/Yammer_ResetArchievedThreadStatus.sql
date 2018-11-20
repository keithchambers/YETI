CREATE PROCEDURE Yammer_ResetArchievedThreadStatus
AS
BEGIN
	UPDATE YM_Messages SET process_status ='PageDownloaded' where thread_id in (
	SELECT Thread_id FROM YM_ArchivedThreads WHERE Modified_date IS NOT NULL) and process_status in ('FilesGenerated','FilesVerified','FilesCompressed')

	UPDATE YM_ExportDetails SET Status ='PageDownloaded' WHERE FileName IN (SELECT csvfilename FROM YM_Messages WHERE process_status ='PageDownloaded'
	AND thread_id IN (SELECT Thread_id FROM YM_ArchivedThreads WHERE Modified_date IS NOT NULL)) and Status in ('FilesGenerated','FilesVerified','FilesCompressed')

	update YMT set Modified_date = null 
	from YM_ArchivedThreads YMT where (select count(distinct thread_id) from YM_Messages where thread_id = YMT.Thread_id) = 0 
	and YMT.Modified_date is not null
END
