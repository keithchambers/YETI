CREATE PROCEDURE [Yammer_Update_ProcessCompletion_old] (@Year NVARCHAR(10), @isRangeInDays BIT, @ToCompress INT)
AS
if(@isRangeInDays = 1)
begin
	if((select count(1) from ym_messages) = 0)
	begin
		update ym_yammeryears set ToProcess = 1 where ymyear = @Year
	end
	update ym_yammeryears set ToCompress = @ToCompress where ymyear = @Year
end
else
begin
	IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE  STARTDATE >= @Year +'-01-01 00:00:00.000' and enddate <= @Year +'-12-31 23:59:59.000' AND ((Status !='FilesVerified') AND (Status !='NoThreads'))) = 0 )
	BEGIN
		IF((SELECT COUNT(1) FROM YM_EXPORTDETAILS WHERE ENDDATE = @Year +'-12-31 23:59:59.000' ) > 0)
		BEGIN
			update ym_yammeryears set ToProcess = 0, ToCompress = @ToCompress where ymyear = @Year
		END
	END
end
