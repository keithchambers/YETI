	Create Procedure [dbo].[Yammer_UpdateExpDetailsForVer]
	(	
	@in_datatable_ThreadIds dbo.IDTableType READONLY,
	@processedBy Nvarchar(200)	
	)
	as
	BEGIN
	 
	UPDATE YM_ExportDetails 
	SET Status = 'FilesVerified', 
	ProcessStage = 'FileGenerated', 
	ModifiedDate = GETDATE() 
    WHERE FileName in ( SELECT csvfilename 
	FROM YM_MESSAGES 
	WHERE thread_id in (select ID from @in_datatable_ThreadIds) and processedBy = @processedBy ) and processedBy = @processedBy

	END

