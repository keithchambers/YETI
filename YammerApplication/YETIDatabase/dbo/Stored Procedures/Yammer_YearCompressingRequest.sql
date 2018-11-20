create PROCEDURE [dbo].[Yammer_YearCompressingRequest] (@ProcessedBy NVARCHAR(100),@isRangeInDays BIT)
AS
BEGIN
	if(@isRangeInDays = 1)
	BEGIN
		SELECT TOP 1 YMYEAR FROM YM_YAMMERYEARS WHERE PROCESSEDBY = @ProcessedBy and ToCompress = 1 ORDER BY SEQUENCE
	END
	ELSE
	BEGIN
		IF ((SELECT COUNT(ID) FROM YM_YAMMERYEARS WHERE ISNULL(processedBy,'') = @ProcessedBy and ToDownload = 0 and ToProcess = 0 and ToCompress = 1) = 0)
		BEGIN	
			update YM_YAMMERYEARS set processedBy = @ProcessedBy where ymyear = (select top 1 ymyear from YM_YAMMERYEARS where ToDownload = 0 and ToProcess = 0 and ToCompress = 1 ORDER BY SEQUENCE)
			SELECT TOP 1 YMYEAR FROM YM_YAMMERYEARS WHERE PROCESSEDBY = @ProcessedBy AND ToDownload = 0 and ToProcess = 0 and ToCompress = 1  ORDER BY SEQUENCE
		END
		ELSE
		BEGIN
			SELECT TOP 1 YMYEAR FROM YM_YAMMERYEARS WHERE PROCESSEDBY = @ProcessedBy AND ToDownload = 0 and ToProcess = 0 and ToCompress = 1 ORDER BY SEQUENCE
		END
	END
END
