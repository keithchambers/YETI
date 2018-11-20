  
CREATE PROCEDURE [dbo].[Yammer_RemoveURLFromAttachments_Test]    
AS    
BEGIN 

SET NOCOUNT ON    
DECLARE @ID BIGINT    
DECLARE @attachments NVARCHAR(max)    
DECLARE @Output NVARCHAR(max)  
DECLARE @RowNumber nvarchar(max)  
    
DECLARE YM_Messages_CURSOR_T CURSOR LOCAL FOR    
SELECT ROW_NUMBER() over(order by id) as RowId,id, attachments FROM YM_Messages_09082018_Test WHERE attachments like '%http%' ORDER BY id
    
OPEN YM_Messages_CURSOR_T    
FETCH NEXT FROM YM_Messages_CURSOR_T INTO  @RowNumber, @ID , @attachments --,@csvfilename     
    
WHILE (@@FETCH_STATUS = 0)    
BEGIN    
     
  SELECT @Output =    
  COALESCE(@Output + ',' ,' ') + case when CHARINDEX('http', value) > 0 then      
  REPLACE( REPLACE(value, SUBSTRING(value, CHARINDEX(': http', value) , CHARINDEX(']', value) - CHARINDEX('http', value) + 3), '') ,'[','')    
  ELSE value    
  END      
  FROM STRING_SPLIT (@attachments ,',' )     
        
  SET @Output= substring(@Output,2,len(@Output))    
  UPDATE YM_Messages_09082018_Test SET attachments = @Output WHERE (SELECT ROW_NUMBER() over(order by id) as RowId)=@RowNumber -- and id=@ID  
        
  SET @Output=''       
  FETCH NEXT FROM YM_Messages_CURSOR_T INTO  @RowNumber, @ID , @attachments --@csvfilename   
      
END    
    
CLOSE YM_Messages_CURSOR_T    
DEALLOCATE YM_Messages_CURSOR_T    
    
SET NOCOUNT OFF  

END