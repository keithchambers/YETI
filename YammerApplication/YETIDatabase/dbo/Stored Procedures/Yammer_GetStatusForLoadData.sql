	Create Procedure [dbo].[Yammer_GetStatusForLoadData]
	(	
	@in_nvarchar_tempFileDate nvarchar(100)
	)
	as
	BEGIN
	SELECT Status FROM YM_ExportDetails WHERE (Status ='DBLoaded' OR Status ='FileLoaded' OR Status ='UsersLoaded') AND EndDate = @in_nvarchar_tempFileDate

	END

