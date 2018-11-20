CREATE PROCEDURE [dbo].[Yammer_Download_NewExportDetails] 
(	@in_nvarchar_startDate nvarchar(50),
	@in_nvarchar_endDate nvarchar(50),
	@in_nvarchar_status nvarchar(50),
	@processedBy nvarchar(100)
)
AS
BEGIN
	INSERT INTO YM_ExportDetails (StartDate,EndDate,ProcessedBy,FileName,Status,ModifiedDate) 
	VALUES ( @in_nvarchar_startDate ,@in_nvarchar_endDate , @processedBy,
	'export-' +  REPLACE(@in_nvarchar_endDate,':','-') + '.zip',@in_nvarchar_status, GETDATE())
END

