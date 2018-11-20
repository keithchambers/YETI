

CREATE PROCEDURE [dbo].[Yammer_RemoveURLFromAttachments]  
AS  
BEGIN   
  
SET NOCOUNT ON  
DECLARE @ID BIGINT  
DECLARE @attachments NVARCHAR(max)  
DECLARE @Output NVARCHAR(max)
DECLARE @csvfilename nvarchar(500)
  
DECLARE YM_Messages_CURSOR CURSOR FOR  
SELECT id, attachments, csvfilename FROM YM_Messages WHERE attachments like '%http%' ORDER BY id  
  
OPEN YM_Messages_CURSOR  
FETCH NEXT FROM YM_Messages_CURSOR INTO @ID , @attachments , @csvfilename  
  
WHILE (@@FETCH_STATUS = 0)  
BEGIN  
   
  SELECT @Output =  
  COALESCE(@Output + ',' ,' ') + case when CHARINDEX('http', value) > 0 then    
  REPLACE( REPLACE(value, SUBSTRING(value, CHARINDEX(': http', value) , CHARINDEX(']', value) - CHARINDEX('http', value) + 3), '') ,'[','')  
  ELSE value  
  END    
  FROM STRING_SPLIT (@attachments ,',' )   
      
  SET @Output= substring(@Output,2,len(@Output))  
  UPDATE YM_Messages SET attachments = @Output WHERE id=@ID and csvfilename=@csvfilename  
      
  SET @Output=''     
  FETCH NEXT FROM YM_Messages_CURSOR INTO @ID , @attachments , @csvfilename 
    
END  
  
CLOSE YM_Messages_CURSOR  
DEALLOCATE YM_Messages_CURSOR  
  
SET NOCOUNT OFF  
  
END   
  

