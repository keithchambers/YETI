CREATE PROCEDURE Yammer_Update_DownloadCompletion (@Year NVARCHAR(10))
AS
BEGIN
	IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE  STARTDATE >= @Year +'-01-01 00:00:00.000' and enddate <= @Year +'-12-31 23:59:59.000' AND ((Status ='In Progress' or Status ='Stopped'))) = 0 )
	BEGIN
		IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE ENDDATE = @Year +'-12-31 23:59:59.000' ) > 0)
		BEGIN
			UPDATE ym_yammeryears SET ToDownload = 0 where ymyear = @Year
		END
	END
END
