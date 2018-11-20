CREATE PROCEDURE Yammer_ToProcessUpdate (@Year NVARCHAR(10))
AS
	UPDATE ym_yammeryears SET ToProcess = 1, ProcessedBy = NULL where ymyear = @Year
	IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE  STARTDATE >= @Year +'-01-01 00:00:00.000' and enddate <= @Year +'-12-31 23:59:59.000' AND ((Status !='FilesVerified'))) = 0 )
	BEGIN
		update ym_yammeryears set ToProcess = 0, ToCompress = 1 where ymyear = @Year
	END
