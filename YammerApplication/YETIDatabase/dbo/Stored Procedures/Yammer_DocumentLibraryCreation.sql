CREATE PROCEDURE Yammer_DocumentLibraryCreation 
(
 @ymyear NVARCHAR(MAX),
 @SPLibraryName NVARCHAR(MAX),
 @groupcount bigint,
 @threadcount bigint,
 @capacity bigint,
 @processedBy NVARCHAR(300)
)
AS
BEGIN	
	IF((SELECT COUNT(1) FROM YM_DocumentLibraries WHERE yammeryear = @ymyear) = 0)
	BEGIN
		IF ((@groupcount + @threadcount ) <= @capacity)
		BEGIN
			INSERT INTO YM_DocumentLibraries (yammeryear,SPLibraryName,capacity,processedBy,groupCount,threadcount) 
			VALUES(@ymyear,@SPLibraryName,@capacity,@processedBy,@groupcount,@threadcount)
			SELECT @SPLibraryName
		END
		ELSE
		BEGIN
			DECLARE @PARTS INT = ((@groupcount + @threadcount ) / @capacity + (@groupcount + @threadcount ) % @capacity )
			WHILE(@PARTS > 0 )
			BEGIN
				INSERT INTO YM_DocumentLibraries (yammeryear,SPLibraryName,capacity,processedBy,groupCount,threadcount) 
				VALUES(@ymyear,@SPLibraryName + '-' + @PARTS,@capacity,@processedBy,0,0)
				SET @PARTS = @PARTS - 1
			END
			SELECT SPLibraryName FROM YM_DocumentLibraries WHERE yammeryear = @ymyear
		END
	END	
	ELSE
	BEGIN
		SELECT SPLibraryName FROM YM_DocumentLibraries WHERE yammeryear = @ymyear
	END
END
