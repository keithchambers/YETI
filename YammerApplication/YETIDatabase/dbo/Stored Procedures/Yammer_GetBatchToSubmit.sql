CREATE PROCEDURE Yammer_GetBatchToSubmit (@YmYear int, @processedBy nvarchar(300), @batchCount int)
AS
BEGIN	
	SELECT Top (@batchCount) BatchNo,FolderPath,GroupName,ItemCount FROM YM_SPBatchLists WHERE isnull(status,'') = '' and [YmYear] = @YmYear and [processedBy] =@processedBy
	order by rand()	
END 
