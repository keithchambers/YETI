CREATE PROCEDURE Yammer_GetOldSharePathList (@isLarge BIT,@PathType NVARCHAR(100))
AS
BEGIN
	SELECT FilePath,SharedFilePath,PathType FROM YM_FileShareLists WHERE isLargePath = @isLarge AND PathType =  @PathType
END
