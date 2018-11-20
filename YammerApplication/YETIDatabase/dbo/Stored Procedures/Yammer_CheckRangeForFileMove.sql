CREATE PROCEDURE [dbo].[Yammer_CheckRangeForFileMove] 
(
 @ProcessedBy NVARCHAR(MAX),@RangeInMonths INT, @RangeInDays INT
)
AS
BEGIN
	DECLARE @StartDate datetime
	DECLARE @EndDate datetime
	DECLARE @ActualEndDate datetime
	DECLARE @Year NVARCHAR(MAX)

	SELECT @Year = ymyear FROM ym_yammeryears WHERE ProcessedBy = @ProcessedBy AND ToProcess = 1
	--IF(@Year IS NULL)
	--BEGIN
	--	SELECT TOP 1 @Year = ymyear FROM ym_yammeryears WHERE ProcessedBy = @ProcessedBy AND ToProcess = 0 AND ToCompress = 0 AND ToRun = 1  ORDER BY sequence
	--END
	IF(@Year IS NOT NULL and not exists(select 1 from YM_Messages))
	--IF(@Year IS NOT NULL)
	BEGIN
		IF(@RangeInMonths != 0)
		BEGIN
			SELECT @StartDate = MIN(StartDate),@ActualEndDate = MAX(EndDate) ,  @EndDate = DATEADD(SS,-1,(DATEADD(MM,@RangeInMonths,MIN(StartDate)))) FROM YM_ExportDetails 
			WHERE IsUploaded = 0 AND (ProcessStage = 'DownloadCompleted') AND DATEPART(YYYY,EndDate) = @Year	
	
			IF( @ActualEndDate >= @EndDate)
			BEGIN
				IF( (SELECT COUNT(ID) FROM YM_ExportDetails WHERE EndDate <= DATEADD(SS,-1,@StartDate) AND StartDate > = DATEADD(MM,-@RangeInMonths,@StartDate) AND ((Status !='FilesVerified' and Status !='FilesCompressed' and Status !='SharePointUploaded' and Status !='NoThreads' ) OR IsVerified !=1 )) = 0)
				BEGIN 
					IF ((SELECT COUNT(ID) FROM YM_MESSAGES WHERE CSVFILENAME IN (SELECT FILENAME FROM YM_ExportDetails WHERE EndDate <= DATEADD(SS,-1,@StartDate) AND StartDate > = DATEADD(MM,-@RangeInMonths,@StartDate)) AND (process_status !='FilesVerified' and process_status !='FilesCompressed' and process_status !='NoThreads' )) = 0 )
					BEGIN
						IF((SELECT Count(id) FROM YM_ExportDetails WHERE StartDate >=@StartDate AND EndDate <= @EndDate AND Status !='Completed') = 0 )
						BEGIN
							SELECT @StartDate StartDate, @EndDate EndDate, DATEPART(MM,@StartDate) FirstFolder, DATEPART(MM,@EndDate) LastFolder, @Year ProcessYear 
							--UPDATE ym_yammeryears SET ToProcess = 1 WHERE ymyear = @Year
						END
					END
				END
			END
		END
		ELSE
		BEGIN
			SELECT @StartDate = MIN(StartDate),@ActualEndDate = MAX(EndDate) ,  @EndDate = DATEADD(SS,-1,(DATEADD(DD,@RangeInDays,MIN(StartDate)))) FROM YM_ExportDetails 
			WHERE IsUploaded = 0 AND (ProcessStage = 'DownloadCompleted') AND DATEPART(YYYY,EndDate) = @Year	
	
			IF( @ActualEndDate >= @EndDate)
			BEGIN
				IF( (SELECT COUNT(ID) FROM YM_ExportDetails WHERE EndDate <= DATEADD(SS,-1,@StartDate) AND StartDate > = DATEADD(DD,-@RangeInDays,@StartDate) AND ((Status !='FilesVerified' and Status !='FilesCompressed' and Status !='SharePointUploaded'  and Status !='NoThreads' ) OR IsVerified !=1 )) = 0)
				BEGIN 
					IF ((SELECT COUNT(ID) FROM YM_MESSAGES WHERE CSVFILENAME IN (SELECT FILENAME FROM YM_ExportDetails WHERE EndDate <= DATEADD(SS,-1,@StartDate) AND StartDate > = DATEADD(DD,-@RangeInDays,@StartDate)) AND (process_status !='FilesVerified'  and process_status !='FilesCompressed' and process_status !='NoThreads' )) = 0 )
					BEGIN
						IF((SELECT Count(id) FROM YM_ExportDetails WHERE StartDate >=@StartDate AND EndDate <= @EndDate AND Status !='Completed') = 0 )
						BEGIN
							SELECT StartDate,EndDate,FileName, DATEPART(MM,EndDate)'Folder',@Year ProcessYear  FROM YM_ExportDetails WHERE StartDate >=@StartDate and EndDate <=@EndDate ORDER BY StartDate
							--SELECT StartDate,EndDate,FileName 'FirstFolder', DATEPART(MM,EndDate)'LastFolder',@Year ProcessYear  FROM YM_ExportDetails WHERE StartDate >=@StartDate and EndDate <=@EndDate ORDER BY StartDate
							
							
							--UPDATE ym_yammeryears SET ToProcess = 1 WHERE ymyear = @Year
						END
					END
				END
			END
		END
	END

END
