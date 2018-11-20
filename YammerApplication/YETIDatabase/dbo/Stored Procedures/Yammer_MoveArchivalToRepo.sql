CREATE PROCEDURE [dbo].[Yammer_MoveArchivalToRepo]
(@ProcessedBy NVARCHAR(100), @Year int)
AS
BEGIN
	IF((SELECT COUNT(1) FROM YM_Messages WHERE process_status !='FilesVerified' AND Processed_By = @ProcessedBy) = 0 )
	BEGIN
		INSERT INTO YM_ArchivedThreadsRepo  WITH (TABLOCKX) (Thread_id,ThreadXMLContent,[Year],Created_Date,Modified_Date)
		select Thread_id,ThreadXMLContent,@Year,Created_Date,Modified_Date from YM_ArchivedThreads 

		UPDATE YM_ArchivedThreadsRepo SET Modified_Date = GETDATE() WHERE Thread_id IN (
		SELECT thread_id FROM YM_ArchivedThreadsBuffer)

		delete from YM_ArchivedThreadsBuffer
		delete from YM_ArchivedThreads
	END
END

