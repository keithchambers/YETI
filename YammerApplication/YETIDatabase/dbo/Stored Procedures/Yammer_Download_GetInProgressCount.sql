CREATE PROCEDURE [dbo].[Yammer_Download_GetInProgressCount] 
( 
@in_varchar_year nvarchar(5),
@processedBy nvarchar(100)
)
AS
BEGIN
	
	SELECT COUNT(ID) Count FROM YM_ExportDetails 
	WHERE Status in ('Stopped', 'In Progress', 'Not Started')
	AND ProcessedBy = @processedBy;
END

