

Create Procedure [dbo].[Yammer_Compress_GetProcessingCount]
(
	@Year Int,
	@RangeInMonths int,	
	@RangeInDays int
)
as
BEGIN

	DECLARE @StartDate datetime
	DECLARE @EndDate datetime
	IF(@RangeInMonths!=0)
	BEGIN
	DECLARE @TempEndDate datetime
	select @StartDate = MIN(StartDate),@EndDate = MAX(EndDate),@TempEndDate = DATEADD(SS,-1,(DATEADD(MM,@RangeInMonths,MIN(StartDate)))) FROM YM_ExportDetails 
				WHERE IsUploaded = 0 AND status = 'FilesVerified' AND DATEPART(YYYY,EndDate) = @Year
		IF(@EndDate > @TempEndDate)
		begin
			SELECT @EndDate = @TempEndDate
		end
	END
	ELSE
	BEGIN
	SELECT @StartDate = MIN(StartDate), @EndDate = DATEADD(SS,-1,(DATEADD(DD,@RangeInDays,MIN(StartDate)))) FROM YM_ExportDetails 
				WHERE IsUploaded = 0 AND status = 'FilesVerified' AND DATEPART(YYYY,EndDate) = @Year	
	END

	IF((SELECT COUNT(1) FROM YM_ExportDetails 
		WHERE StartDate >= @StartDate AND EndDate <= @EndDate AND (status != 'FilesVerified' and status !='Nothreads')) = 0)
	BEGIN
		select @StartDate StartDate, @EndDate EndDate
	END	
END
