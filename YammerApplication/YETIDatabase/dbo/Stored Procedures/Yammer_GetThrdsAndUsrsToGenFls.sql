	Create Procedure [dbo].[Yammer_GetThrdsAndUsrsToGenFls]
	(
	@processedBy nvarchar(300)
	)
	as
	BEGIN
	 select count(id) MessageCount,thread_id from YM_Messages (NOLOCK) where processed_by = @processedBy AND  process_status = 'PageDownloaded' group by thread_id;
     --SELECT DISTINCT UserId,FullName,EmailAlias FROM YM_UsersList (NOLOCK) WHERE FullName is not null
     SELECT UserId,FullName,EmailAlias from (
		select ROW_NUMBER() OVER (PARTITION BY userid ORDER BY EmailAlias desc) VerCount,
		UserId,FullName,EmailAlias from (
			select distinct UserId,FullName,EmailAlias
			FROM YM_UsersList (NOLOCK) WHERE FullName is not null) A
		)B
	 where VerCount=1;
	END

