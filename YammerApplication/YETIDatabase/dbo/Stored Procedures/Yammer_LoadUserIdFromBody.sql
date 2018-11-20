CREATE PROCEDURE Yammer_LoadUserIdFromBody (@processedBy NVARCHAR(150))
AS
BEGIN
	UPDATE UL SET UL.FullName = U.name, UL.EmailAlias = SUBSTRING(U.email,0,charindex('@',U.email,0))
	FROM YM_UsersList UL JOIN YM_Users U ON UL.UserId = U.id
	WHERE FullName is null
	
	UPDATE YM SET FullName = sender_name, EmailAlias = SUBSTRING(sender_email,0,CHARINDEX('@',sender_email,0))
	FROM YM_UsersList YM JOIN YM_Messages M
	ON YM.UserId = M.sender_id
	WHERE FullName IS NULL

	INSERT INTO YM_UsersList
	SELECT DISTINCT id,name,SUBSTRING(email,0,charindex('@',email,0))alias FROM YM_Users where name is not null
	EXCEPT
	SELECT * FROM YM_UsersList

	DECLARE @TABLE TABLE
	(
	MessageId  NVARCHAR(MAX),
	UserId NVARCHAR(MAX),
	Body NVARCHAR(MAX)
	)

	DECLARE @TEMP_TABLE TABLE
	(
	MessageId  NVARCHAR(MAX),
	UserId NVARCHAR(MAX),
	Body NVARCHAR(MAX)
	)
	;WITH CTE
	AS
	(
	SELECT dbo.ListUserIds(body) UserIds,id,body FROM YM_Messages WHERE processed_by = @processedBy AND process_status = 'FileLoaded'
	)
	INSERT INTO @TEMP_TABLE
	SELECT  id,UserIds,body FROM CTE WHERE UserIds IS NOT NULL AND UserIds != ''

	INSERT INTO @TABLE
	select * from (
	SELECT MessageId,UserId,body FROM @TEMP_TABLE WHERE UserId = CASE WHEN (CHARINDEX(',',UserId,0) =  0 ) THEN UserId ELSE '1' END
	) b where ISNUMERIC(UserId) = 1 
	
	union
	select * from (
	SELECT A.MessageId,  
		 LTRIM(Split.a.value('.', 'NVARCHAR(MAX)')) AS UserId ,A.Body 
	 FROM  (SELECT MessageId, Body, 
			 CAST ('<M>' + REPLACE(UserId, ',', '</M><M>') + '</M>' AS XML) AS UserId  
		 FROM  @TEMP_TABLE WHERE UserId = CASE WHEN (CHARINDEX(',',UserId,0) >  0 ) THEN UserId ELSE '1' END) AS A CROSS APPLY UserId.nodes ('/M') AS Split(a)
		 ) a where ISNUMERIC(UserId) = 1 

	INSERT INTO YM_UsersList (UserId) 
	select distinct UserId from @TABLE
	EXCEPT
	SELECT DISTINCT UserId FROM YM_UsersList
END
