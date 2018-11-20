CREATE PROCEDURE Yammer_ResetArchievedXML
AS
BEGIN	
	IF((select COUNT(1) from YM_ArchivedThreads WHERE  ThreadXMLContent.exist('/Thread/ParentMessage/ParentMessage' ) = 1) > 0)
	BEGIN
		UPDATE YM_Messages SET process_status = 'PageDownloaded' where 
		thread_id in (select thread_id from YM_ArchivedThreads WHERE  ThreadXMLContent.exist('/Thread/ParentMessage/ParentMessage' ) = 1)
		SELECT @@ROWCOUNT 
		update YM_ArchivedThreads set ThreadXMLContent = ThreadXMLContent.query('<Thread> {/Thread/ParentMessage/ParentMessage } </Thread>' ) WHERE 
		ThreadXMLContent.exist('/Thread/ParentMessage/ParentMessage' ) = 1	
	END
END
