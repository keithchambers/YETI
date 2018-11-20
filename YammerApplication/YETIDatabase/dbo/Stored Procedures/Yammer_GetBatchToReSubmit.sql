CREATE PROCEDURE Yammer_GetBatchToReSubmit (@YmYear int, @processedBy nvarchar(300), @batchCount int)
AS
BEGIN	
	SELECT Top (@batchCount) BatchNo,FolderPath,GroupName,SPMappingId,ItemCount FROM YM_SPBatchLists WHERE isnull(status,'') = '' 
	and [YmYear] = @YmYear and [processedBy] =@processedBy and ISNULL([parentBatchNo],0)! = 0
	order by rand()	
END 
