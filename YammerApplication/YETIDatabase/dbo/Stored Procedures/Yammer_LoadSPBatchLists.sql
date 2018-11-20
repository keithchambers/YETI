CREATE PROCEDURE Yammer_LoadSPBatchLists (@Year int, @processedBy nvarchar(300), @folderPathCount dbo.SPBatchTableType READONLY, @ParentBatchNo int = null, @SPMappingId INT)
AS
BEGIN
	if(ISNULL(@ParentBatchNo,0) != 0)
	begin
		select @SPMappingId = SPMappingId from  YM_SPBatchLists where batchNo = @ParentBatchNo
	end	
	INSERT INTO YM_SPBatchLists ([YmYear],[FolderPath],[ItemCount],[GroupName],[GroupType],[processedBy],[parentBatchNo],[SPMappingId])
	SELECT DISTINCT @Year,T.FolderPath,(SELECT SUM(ItemCount) FROM @folderPathCount fpc WHERE fpc.FolderPath = T.FolderPath)[ItemCount] ,
	GroupName,GroupType,@processedBy,@ParentBatchNo,@SPMappingId FROM @folderPathCount T WHERE FolderPath IN (
	SELECT FolderPath FROM @folderPathCount
	EXCEPT
	SELECT FolderPath FROM YM_SPBatchLists WHERE FolderPath IN (SELECT FolderPath FROM @folderPathCount))
END 
