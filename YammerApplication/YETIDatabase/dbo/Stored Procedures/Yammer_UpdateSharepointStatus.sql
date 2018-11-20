CREATE PROCEDURE Yammer_UpdateSharepointStatus 
( @JobId nvarchar(max), @status nvarchar(300) )
AS
BEGIN	
	UPDATE YM_AzureContainer SET JobStatus = @status,PackageStage='Submitted' WHERE PackageGUID = @JobId	
END
