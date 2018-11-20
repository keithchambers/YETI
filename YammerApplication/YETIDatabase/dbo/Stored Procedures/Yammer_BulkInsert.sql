CREATE PROCEDURE [dbo].[Yammer_BulkInsert] (@FilePath NVARCHAR(max),@TableName NVARCHAR(MAX))    
AS    
BEGIN    
 EXEC('    
 BULK    
 INSERT YM_'+@TableName+'    
 FROM '''+@FilePath+'''    
 WITH    
 (    
 CODEPAGE = ''65001'',    
 FIELDTERMINATOR = ''\r\t'',    
 ROWTERMINATOR = ''\n''    
 )')    
 --exec dbo.Yammer_UpdateColumnData_YMMessagesSample  
END 