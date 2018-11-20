	Create Procedure [dbo].[Yammer_UpdateUserDetails]
	(	
	@in_nvarchar_FullName nvarchar(300),
	@in_nvarchar_EmailAlias nvarchar(300),
	@in_nvarchar_UserId bigint
	)
	as
	BEGIN
	 
	UPDATE YM_UsersList 
	WITH (UPDLOCK) 
	SET 
	FullName = @in_nvarchar_FullName , 
	EmailAlias = @in_nvarchar_EmailAlias
	 WHERE UserId = @in_nvarchar_UserId;

	END
