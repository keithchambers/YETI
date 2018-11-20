CREATE PROCEDURE Yammer_CheckBatchInProgress (@Year int, @processedBy nvarchar(300))
AS
BEGIN	
	SELECT count(1) BatchCount FROM YM_SPBatchLists WHERE status not in ('Completed','PartiallyCompleted','Failed') and [YmYear] = @Year and [processedBy] =@processedBy
	
END 
