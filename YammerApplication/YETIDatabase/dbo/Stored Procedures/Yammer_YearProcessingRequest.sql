Create PROCEDURE [dbo].[Yammer_YearProcessingRequest] (@ProcessedBy NVARCHAR(100),@isRangeInDays BIT)
AS
BEGIN
	IF(@isRangeInDays = 1)
	BEGIN

	    -- 517 removed condition on AND ToCompress = 1 as Toprocess =1 is enough
		IF ((SELECT COUNT(ID) FROM YM_YAMMERYEARS WHERE ISNULL(processedBy,'') = @ProcessedBy and ToProcess = 1 ) > 0)
		BEGIN	
			SELECT YMYEAR,0 ToGenerate FROM YM_YAMMERYEARS WHERE PROCESSEDBY = @ProcessedBy and ToProcess = 1
		END
		ELSE
		BEGIN
			IF ((SELECT COUNT(ID) FROM YM_YAMMERYEARS WHERE ISNULL(processedBy,'') = @ProcessedBy and ToProcess = 1) > 0)
			BEGIN	
				SELECT YMYEAR,1 ToGenerate FROM YM_YAMMERYEARS WHERE PROCESSEDBY = @ProcessedBy and ToProcess = 1
			END
			ELSE
			BEGIN
				SELECT 1900 as YMYEAR,0 ToGenerate
			END
		END
	END
	ELSE
	BEGIN
		IF ((SELECT COUNT(ID) FROM YM_YAMMERYEARS WHERE ISNULL(processedBy,'') = @ProcessedBy and ToProcess = 1) > 0)
		BEGIN	
			SELECT YMYEAR,1 ToGenerate FROM YM_YAMMERYEARS WHERE PROCESSEDBY = @ProcessedBy and ToProcess = 1
		END
		ELSE
		BEGIN
			SELECT 1900 as YMYEAR,0 ToGenerate
		END
	END
END
