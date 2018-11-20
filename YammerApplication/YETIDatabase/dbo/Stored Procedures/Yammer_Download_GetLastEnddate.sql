Create PROCEDURE [dbo].[Yammer_Download_GetLastEnddate] 
(@in_varchar_year nvarchar(5)) 
AS
BEGIN
	SELECT TOP 1 DATEADD(SS,1,EndDate)EndDate FROM YM_ExportDetails 
	WHERE DATEPART(YYYY,EndDate) = @in_varchar_year ORDER BY EndDate DESC
END

