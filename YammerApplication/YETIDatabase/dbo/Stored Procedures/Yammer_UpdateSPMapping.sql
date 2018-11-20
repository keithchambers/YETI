CREATE PROCEDURE [dbo].[Yammer_UpdateSPMapping] (@BatchNo int,@Year int,  @startDate datetime, @endDate datetime, @processedBy nvarchar(100))
AS
BEGIN
	DECLARE @ParentBatchNo INT 
	select @ParentBatchNo = parentBatchNo from YM_SPBatchLists where batchno = @BatchNo
	if(@ParentBatchNo is null)
	begin
		UPDATE YM_SPBatchLists SET status = 'Completed' where BatchNo = @BatchNo and status ! ='Failed'
		--UPDATE YM_AzureContainer set JobStatus = 'Completed' where BatchNo = @BatchNo
		
	end
	else
	begin
		if((select count(1) from YM_SPBatchLists where parentBatchNo = @ParentBatchNo and ISNULL(status,'') not in ('Completed','PartiallyCompleted','Failed')) = 0)
		begin
			UPDATE YM_SPBatchLists SET status = 'Completed' where BatchNo = @ParentBatchNo
			--UPDATE YM_AzureContainer set JobStatus = 'Completed' where BatchNo = @ParentBatchNo
		end
	end
	if((select count(1) from YM_SPBatchLists where ISNULL(status,'') not in ('Completed','PartiallyCompleted','Failed') and  [SPMappingId] = (select [SPMappingId] from YM_SPBatchLists where batchno = @BatchNo)) = 0)
	begin
		update YM_SPDirectoryMapping set status ='Completed',ModifiedDate=GETDATE() where id = (select [SPMappingId] from YM_SPBatchLists where batchno = @BatchNo)
		UPDATE ym_yammeryears SET ToSPUpload = 0, ToProcess = 0,ToCompress =0 , TODEDUPLICATE = 1 WHERE ymyear =@Year

	end
	IF((SELECT count(1) FROM YM_SPDirectoryMapping WHERE ISNULL(Status,'') !='Completed' AND UploadStartDate >= @startDate AND UploadEndDate <= @endDate) = 0)
	BEGIN
		UPDATE YM_ExportDetails SET IsUploaded = 1 , Status = 'SharePointUploaded',ProcessStage ='Completed',
		ProcessedBy = @processedBy WHERE StartDate >= @startDate AND EndDate <= @endDate

		-- UPDATING THE STATUS OF ToSPUpload to 0 and ToDeduplicate to 1 -- 517
		UPDATE ym_yammeryears SET ToSPUpload = 0, ToProcess = 0,ToCompress =0 , TODEDUPLICATE = 1 WHERE ymyear =@Year


	END
	if((select count(1) from YM_ExportDetails where STARTDATE >= cast(@Year as nvarchar) +'-01-01 00:00:00.000' and enddate <= cast(@Year as nvarchar) +'-12-31 23:59:59.000' and IsUploaded != 1) = 0)
	begin
		update ym_yammeryears set ToRun = 0, ToSPUpload = 0, processedBy = NULL  where ymyear = @Year
	end
END