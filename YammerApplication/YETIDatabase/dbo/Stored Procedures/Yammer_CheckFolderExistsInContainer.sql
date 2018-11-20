CREATE PROCEDURE Yammer_CheckFolderExistsInContainer 
(
 @ymyear INT,
 @sourceFolderpath nvarchar(max)
)
AS
BEGIN	
	SELECT COUNT(1) FROM YM_AzureContainer WHERE ymyear = @ymyear and SourceFolderpath = @sourceFolderpath 
END
