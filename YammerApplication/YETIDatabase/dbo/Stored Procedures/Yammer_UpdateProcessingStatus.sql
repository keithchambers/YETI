Create Procedure [dbo].[Yammer_UpdateProcessingStatus]
(
@in_nvarchar_newStatus nvarchar(100),	
@in_nvarchar_prevStatus nvarchar(100),
@processedBy nvarchar(300)
)
as
BEGIN
if @in_nvarchar_newStatus = 'FilesGenerated'  
	     
	UPDATE YM_ExportDetails 
		SET ProcessStage = 'CSVProcessed', 
		Status = @in_nvarchar_newStatus,
		ModifiedDate = GETDATE() 
	WHERE ISNULL(Status,'') = @in_nvarchar_prevStatus 
	and (ProcessStage = 'CSVProcessing' or ProcessStage = 'FileGeneratedFailed') 
	and FileName in 
		(select distinct csvfilename 
				from YM_Messages 
			where ISNULL(process_status,'') =@in_nvarchar_newStatus 
			and processed_by = @processedBy);
            
else
      
	UPDATE YM_ExportDetails  
		SET Status = @in_nvarchar_newStatus, 
		    ModifiedDate = GETDATE() 
	WHERE ISNULL(Status,'') = ISNULL(@in_nvarchar_prevStatus,'Moved') 
	and (ProcessStage = 'CSVProcessing' or ProcessStage = 'FileGeneratedFailed') 
	and FileName in  
	(select distinct csvfilename 
		    from YM_Messages 
	    where ISNULL(process_status,'') =@in_nvarchar_prevStatus 
		and processed_by = @processedBy);

	UPDATE YM_Messages 
		SET process_status = @in_nvarchar_newStatus 
	where ISNULL(process_status,'') =@in_nvarchar_prevStatus
	and processed_by = @processedBy               
            
END

