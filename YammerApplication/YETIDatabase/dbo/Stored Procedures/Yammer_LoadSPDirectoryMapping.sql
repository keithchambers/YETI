CREATE PROCEDURE  [dbo].Yammer_LoadSPDirectoryMapping 
(@uploadStartDate NVARCHAR(300), @uploadEndDate NVARCHAR(300), @folderPath NVARCHAR(300), @threadCount bigint, @processedBy NVARCHAR(MAX))
AS
BEGIN
	IF((SELECT COUNT(1) FROM YM_SPDirectoryMapping WHERE UploadStartDate >= @uploadStartDate and UploadEndDate <= @uploadEndDate) = 0 )
	BEGIN
		;WITH    AllDays
			AS ( SELECT   cast(@uploadStartDate as datetime) AS startdate, DATEADD(second,-1, DATEADD(DAY, 1, @uploadStartDate)) enddate
				UNION ALL
				SELECT   DATEADD(DAY, 1, startdate),DATEADD(DAY, 1, enddate)
				FROM     AllDays
				WHERE    enddate < cast(@uploadEndDate as datetime) )
		INSERT INTO YM_SPDirectoryMapping (UploadStartDate,UploadEndDate,FolderPath,ymYear,ThreadCount,ProcessedBy)
		SELECT startdate,enddate,@folderPath,YEAR(@uploadEndDate),@threadCount,@processedBy FROM   AllDays OPTION (MAXRECURSION 0)		
	END
END
