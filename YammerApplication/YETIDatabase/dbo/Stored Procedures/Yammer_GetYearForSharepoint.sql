CREATE PROCEDURE Yammer_GetYearForSharepoint (@ProcessedBy nvarchar(max))
AS
BEGIN	
	select top 1 ymyear from ym_yammeryears where ToSPUpload = 1 and ToRun = 1 and processedBy = @ProcessedBy order by sequence
END
