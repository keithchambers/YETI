CREATE PROCEDURE [dbo].[Yammer_Update_CompressCompletion] (@Year NVARCHAR(10), @isRangeInDays BIT, @RangeInMonths INT)
AS
IF(@isRangeInDays = 1)
BEGIN
	update ym_yammeryears set ToProcess = 0, ToCompress = 0, ToSPUpload = 1  where ymyear = @Year
END
ELSE IF(@RangeInMonths > 0)
BEGIN
	DECLARE @StartDate datetime
	DECLARE @EndDate datetime
	SELECT @StartDate = MIN(StartDate), @EndDate = DATEADD(SS,-1,(DATEADD(MM,@RangeInMonths,MIN(StartDate)))) FROM YM_ExportDetails 
			WHERE IsUploaded = 0 AND (ProcessStage = 'CompressionCompleted') AND DATEPART(YYYY,EndDate) = @Year	
	IF( (SELECT COUNT(ID) FROM YM_ExportDetails WHERE EndDate <= DATEADD(SS,-1,@StartDate) AND StartDate > = DATEADD(MM,-@RangeInMonths,@StartDate) AND (Status !='FilesCompressed' and Status !='NoThreads' )) = 0)
	BEGIN 
		update ym_yammeryears set ToProcess = 1, ToCompress = 0, ToSPUpload = 1  where ymyear = @Year
	END
END
IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE  STARTDATE >= @Year +'-01-01 00:00:00.000' and enddate <= @Year +'-12-31 23:59:59.000' AND ((Status !='FilesCompressed')AND (Status !='NoThreads')) AND IsUploaded = 0 ) = 0 )
BEGIN
	IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE ENDDATE = @Year +'-12-31 23:59:59.000' ) > 0)
	BEGIN
		update ym_yammeryears set ToDownload = 0 , ToProcess = 0, ToCompress = 0, ToSPUpload = 1 where ymyear = @Year
	END
END
