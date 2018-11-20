CREATE PROCEDURE [dbo].[Yammer_ToDownloadUpdate] (@Year NVARCHAR(10))
AS
BEGIN
	UPDATE ym_yammeryears SET ToDownload = 1 , ToProcess = 1, ProcessedBy = NULL where ymyear = @Year
	IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE  STARTDATE >= @Year +'-01-01 00:00:00.000' and enddate <= @Year +'-12-31 23:59:59.000' AND ((Status ='Completed' or Status ='In Progress' or Status ='Stopped' or Status = 'Failed' ))) = 0 )
	BEGIN
		UPDATE ym_yammeryears SET ToDownload = 0 where ymyear = @Year
	END
END
