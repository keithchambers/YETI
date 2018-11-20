CREATE FUNCTION [dbo].[ListUserIds] 
(	
	@Body NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX) 

AS
begin

	DECLARE @USER_TABLE TABLE
	(
	Userid NVARCHAR(MAX)
	)

	--DECLARE @BODY NVARCHAR(MAX) = '[User:1487868785:mattwolodarsky] this is the partner I was talking about.[User:12487868785:mattwolodarsky] We''ve met a few years ago, and back then they were not able to bring their on-premises solution for analytics/adoption to Office 365. Glad to see they have a solution for Yammer now, and so glad to know that they''ve worked their way into Office 365!! :-) cc: User:1503290471:lawrencechiu'
	DECLARE @TEMP_BODY NVARCHAR(MAX) = @BODY

	WHILE (CHARINDEX('[[User:',@TEMP_BODY,0) > 0)
	BEGIN
		DECLARE @ID NVARCHAR(MAX)
		SELECT @TEMP_BODY = SUBSTRING(@TEMP_BODY,CHARINDEX('[[User:',@TEMP_BODY,0)+7,LEN(@TEMP_BODY))				
		SELECT @ID = SUBSTRING(@TEMP_BODY,0, CHARINDEX(']]',@TEMP_BODY,0))
		SET @BODY =  REPLACE(@BODY, (SELECT '[[User:' + SUBSTRING(@TEMP_BODY,0, CHARINDEX(']]',@TEMP_BODY,0))),'@')	
		--IF ((SELECT COUNT(Userid) FROM @USER_TABLE WHERE Userid = @ID) = 0)
		if ((SELECT COUNT(Userid) FROM @USER_TABLE WHERE Userid = @ID) = 0 and (TRY_CONVERT(BIGINT,@ID) IS NOT NULL))
		BEGIN
			INSERT INTO @USER_TABLE VALUES (@ID)
		END
	END
	SET @TEMP_BODY = @BODY
	WHILE (CHARINDEX('[User:',@TEMP_BODY,0) > 0)
	BEGIN
		SELECT @TEMP_BODY = SUBSTRING(@TEMP_BODY,CHARINDEX('[User:',@TEMP_BODY,0)+6,LEN(@TEMP_BODY))	 
		SELECT @ID = SUBSTRING(@TEMP_BODY,0, CHARINDEX(':',@TEMP_BODY,0))
		SET @BODY =  REPLACE(@BODY, (SELECT '[User:' + SUBSTRING(@TEMP_BODY,0, CHARINDEX(':',@TEMP_BODY,0))),'@')	
		--IF ((SELECT COUNT(Userid) FROM @USER_TABLE WHERE Userid = @ID) = 0)
		IF ((SELECT COUNT(Userid) FROM @USER_TABLE WHERE Userid = @ID) = 0 and (TRY_CONVERT(BIGINT,@ID) IS NOT NULL))
		BEGIN
			INSERT INTO @USER_TABLE VALUES (@ID)
		END
	END
	WHILE (CHARINDEX('User:',@BODY,0) > 0)
	BEGIN
		SELECT @BODY = SUBSTRING(@BODY,CHARINDEX('User:',@BODY,0)+5,LEN(@BODY))	 
		SELECT @ID = SUBSTRING(@BODY,0, CHARINDEX(':',@BODY,0))
		--begin modified 20170222
		--IF ((SELECT COUNT(Userid) FROM @USER_TABLE WHERE Userid = @ID) = 0)
		IF ((SELECT COUNT(Userid) FROM @USER_TABLE WHERE Userid = @ID) = 0 and (TRY_CONVERT(BIGINT,@ID) IS NOT NULL))
		--END MODIFIED 20170222
		BEGIN
			INSERT INTO @USER_TABLE VALUES (@ID)
		END
	END
	
	DECLARE @UserIds NVARCHAR(MAX) 
	SELECT @UserIds = COALESCE(@UserIds + ', ', '') + Userid 
	FROM @USER_TABLE
	SELECT @UserIds = REPLACE(@UserIds,'&','&amp;')
	SELECT @UserIds = REPLACE(@UserIds,'<','&lt;')
	SELECT @UserIds = REPLACE(@UserIds,'>','&gt;')
	SELECT @UserIds = REPLACE(@UserIds,'"','&quot;')
	SELECT @UserIds = REPLACE(@UserIds,'''','&#39;')

	RETURN @UserIds
END
