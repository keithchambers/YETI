	Create Procedure [dbo].[Yammer_UpdateExpDtlsForFailedThrd]
	(	
	@in_nvarchar_ThreadCSVNames dbo.IDTableType READONLY
	)
	as
	BEGIN
	 
	 UPDATE YM_ExportDetails 
	 SET Status = 'PageDownloaded', 
	 ProcessStage = 'FileGeneratedFailed', 
	 ModifiedDate = GETDATE() 
	 WHERE FileName in (select ID from @in_nvarchar_ThreadCSVNames)

	END

