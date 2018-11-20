	Create Procedure [dbo].[Yammer_GetUserIds]
	as
	BEGIN
	SELECT DISTINCT UserId FROM YM_UsersList WHERE FullName IS NULL

	END
