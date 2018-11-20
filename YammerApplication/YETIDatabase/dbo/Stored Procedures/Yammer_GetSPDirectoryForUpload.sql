CREATE PROCEDURE [dbo].[Yammer_GetSPDirectoryForUpload] (@Year INT)
AS
BEGIN
	;with cte
	as
	(
		select top 1 FolderPath from YM_SPDirectoryMapping WHERE ymYear = @Year AND isnull(status,'') !='Completed' and isnull(status,'') !='PartiallyCompleted' order by newid()
	)
	SELECT UploadStartDate,UploadEndDate,FolderPath,ThreadCount,Id FROM YM_SPDirectoryMapping where FolderPath in (select FolderPath from cte) order by UploadStartDate
END
