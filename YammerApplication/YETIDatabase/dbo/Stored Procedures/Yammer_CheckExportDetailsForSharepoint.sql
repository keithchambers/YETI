CREATE PROCEDURE Yammer_CheckExportDetailsForSharepoint (@startDate datetime, @EndDate datetime)
AS
BEGIN
 SELECT COUNT(1) FROM YM_ExportDetails WHERE StartDate >= @startDate  AND EndDate <= @EndDate AND status != 'FilesCompressed' AND ProcessStage != 'CompressionCompleted' and IsUploaded = 0
END
