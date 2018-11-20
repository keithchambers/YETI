
CREATE PROCEDURE [dbo].[Yammer_Common_UpdateStatus] 
(	@in_nvarchar_startDate nvarchar(50),
	@in_nvarchar_endDate nvarchar(50),
	@in_nvarchar_status nvarchar(50),
	@in_nvarchar_stage nvarchar(50),
	@in_int_timeTried int,
	@in_bit_toMove bit

)
AS
BEGIN	
	--@in_bit_toMove is for download only
	IF @in_bit_toMove is not null
	begin
		if @in_bit_toMove=0
		begin
			UPDATE YM_ExportDetails SET Status = @in_nvarchar_status, 
			ProcessStage = @in_nvarchar_stage, 
			ModifiedDate = GETDATE() ,                       
            TimesTried= @in_int_timeTried
            WHERE StartDate = @in_nvarchar_startDate
			 AND EndDate = @in_nvarchar_endDate
		end
		ELSE
		BEGIN
			UPDATE YM_ExportDetails SET Status = @in_nvarchar_status, 
			ProcessStage = @in_nvarchar_stage, 
			ModifiedDate = GETDATE() ,                       
            TimesTried= TimesTried + 1
            WHERE StartDate >= @in_nvarchar_startDate
			 AND EndDate <= @in_nvarchar_endDate
		END
	end
	--for compress only
	else
	BEGIN
		UPDATE YM_ExportDetails 
		SET Status =@in_nvarchar_status, ProcessStage =@in_nvarchar_stage where StartDate >= @in_nvarchar_startDate
		AND EndDate <= @in_nvarchar_endDate AND status in ('FilesVerified','NoThreads')

		--UPDATE ym_yammeryears SET ToCompress = 0, ToRun = 0 where ymyear = YEAR(@in_nvarchar_startDate)

	END
END 



