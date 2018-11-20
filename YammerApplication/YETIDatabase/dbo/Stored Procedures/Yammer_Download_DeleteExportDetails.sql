
Create PROCEDURE [dbo].[Yammer_Download_DeleteExportDetails] 
(	@in_nvarchar_startDate nvarchar(50),
	@in_nvarchar_endDate nvarchar(50)
)
AS
BEGIN
	delete from YM_ExportDetails
	 WHERE StartDate =@in_nvarchar_startDate AND EndDate = @in_nvarchar_endDate
END         

