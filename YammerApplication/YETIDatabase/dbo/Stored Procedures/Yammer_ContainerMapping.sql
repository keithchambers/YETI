CREATE PROCEDURE Yammer_ContainerMapping 
(
 @ContainerName NVARCHAR(MAX),
 @GUID NVARCHAR(MAX),
 @ymyear INT,
 @SPLibraryName NVARCHAR(MAX),
 @status NVARCHAR(100),
 @packageStage NVARCHAR(100),
 @processedBy NVARCHAR(300),
 @SourceFolderPath NVARCHAR(MAX),
 @threadcount bigint,
 @FileContainerUri NVARCHAR(MAX),
 @PackageContainerUri NVARCHAR(MAX),
 @ReportingQueueUri NVARCHAR(MAX),
 @BatchNo int
)
AS
BEGIN	
	IF((SELECT COUNT(1) FROM YM_AzureContainer WHERE ymyear = @ymyear AND SourceFolderPath = @SourceFolderPath and jobstatus!='Failed') = 0)
	BEGIN
		INSERT INTO YM_AzureContainer (ymyear,SPLibraryName,ContainerName,PackageGUID,PackageStage,processedBy,SourceFolderPath,threadcount,BatchNo,
		FileContainerUri,PackageContainerUri,ReportingQueueUri,JobStatus) 
		VALUES(@ymyear,@SPLibraryName,@ContainerName,@GUID,@packageStage,@processedBy,@SourceFolderPath,@threadcount,@BatchNo,
		@FileContainerUri,@PackageContainerUri,@ReportingQueueUri,@status)
	END
	ELSE
	BEGIN
		UPDATE YM_AzureContainer SET PackageGUID = @GUID, JobStatus = @status,PackageStage = @packageStage,
				ContainerName = @ContainerName,
				FileContainerUri = ISNULL(NULLIF(@FileContainerUri, ''),FileContainerUri), 
				PackageContainerUri = ISNULL(NULLIF(@PackageContainerUri, ''),PackageContainerUri),
				ReportingQueueUri = ISNULL(NULLIF(@ReportingQueueUri,''),ReportingQueueUri),
				SPLibraryName = @SPLibraryName,processedBy = @processedBy,threadcount=@threadcount,BatchNo=@BatchNo
			WHERE ymyear = @ymyear AND SourceFolderPath = @SourceFolderPath
	END
END
