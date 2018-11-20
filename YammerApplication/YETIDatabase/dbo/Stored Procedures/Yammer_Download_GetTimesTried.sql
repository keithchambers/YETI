Create PROCEDURE [dbo].[Yammer_Download_GetTimesTried] 
(@in_varchar_year nvarchar(5), 
@processedBy nvarchar(100))
AS
BEGIN
	
	SELECT StartDate, EndDate,ISNULL(TimesTried,0) TimesTried 
	FROM YM_ExportDetails 
	WHERE DATEPART(YYYY, EndDate) = @in_varchar_year
	and ProcessedBy = @processedBy
	AND (Status = 'Stopped' or Status = 'In Progress')  order by NEWID()
END




