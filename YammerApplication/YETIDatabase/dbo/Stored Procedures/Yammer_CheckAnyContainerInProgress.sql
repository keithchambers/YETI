CREATE PROCEDURE Yammer_CheckAnyContainerInProgress 
(
 @ymyear INT,
 @processedBy NVARCHAR(300)
)
AS
BEGIN	
	SELECT Distinct PackageGUID,ContainerName,ReportingQueueUri,SourceFolderPath,YM.BatchNo,SP.GroupName,SP.ItemCount,SP.GroupType,SPM.FolderPath
	FROM YM_AzureContainer YM
	join YM_SPBatchLists SP on SP.BatchNo = YM.BatchNo
	join YM_SPDirectoryMapping SPM on SP.SPMappingId = SPM.Id
	WHERE YM.ymyear = @ymyear AND YM.processedBy = @processedBy AND SP.status !='Completed' and SP.status !='PartiallyCompleted'
	and SP.status !='Failed'
	--AND (JobStatus in ('Processing','Queued') or (JobStatus = 'Completed' and PackageStage = 'PackageSubmitted'))
	

END
