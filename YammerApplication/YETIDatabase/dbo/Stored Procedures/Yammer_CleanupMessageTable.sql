
  
CREATE PROCEDURE [dbo].[Yammer_CleanupMessageTable]  
AS  
BEGIN  
 IF(((SELECT COUNT(1) FROM YM_MessagesSample WHERE id like  CHAR(13)+'%') > 0))  
 BEGIN  
 update YM_MessagesSample set id = REPLACE(id,CHAR(13),'') where id like  CHAR(13)+'%'  
 end  
   
 IF(((SELECT COUNT(1) FROM YM_MessagesSample WHERE id like  CHAR(10)+'%') > 0))  
 BEGIN  
 update YM_MessagesSample set id = REPLACE(id,CHAR(10),'') where id like  CHAR(10)+'%'  
 end  
  
 IF(((SELECT COUNT(1) FROM YM_MessagesSample WHERE attachments = 'https://www.yammer.com/api/v1/messages/'+id) > 0))  
 BEGIN  
   
  DECLARE @BTTable Table  
  (  
  [id] [nvarchar](max) NULL,  
  [body] [nvarchar](max) NULL,  
  [api_url] [nvarchar](max) NULL,  
  [attachments] [nvarchar](max) NULL,  
  [deleted_by_id] [nvarchar](max) NULL,  
  [deleted_by_type] [nvarchar](max) NULL,  
  [created_at] [nvarchar](max) NULL,  
  [deleted_at] [nvarchar](max) NULL,  
  [processed_by] [nvarchar](max) NULL,  
  [csvfilename] [nvarchar](max) NULL,  
  [process_status] [nvarchar](max) NULL  
  )  
  insert into @BTTable  
  select  id, body+api_url body,attachments api_url,deleted_by_id attachments,deleted_by_type deleted_by_id,created_at deleted_by_type,deleted_at created_at,  
  processed_by deleted_at, csvfilename processed_by,process_status csvfilename, NULL process_status  
  FROM YM_MessagesSample WHERE attachments = 'https://www.yammer.com/api/v1/messages/'+id   
    
  update YMS set YMS.body = TB.body,YMS.api_url = TB.api_url,  
  YMS.attachments = TB.attachments,YMS.deleted_by_id = TB.deleted_by_id,  
  YMS.deleted_by_type = TB.deleted_by_type,YMS.created_at = TB.created_at,  
  YMS.deleted_at = TB.deleted_at,YMS.processed_by = TB.processed_by,YMS.csvfilename = replace(replace(TB.csvfilename,CHAR(9) ,''),char(13),''),  
  YMS.process_status = TB.process_status  
  from YM_MessagesSample YMS join @BTTable TB on TB.id = YMS.id  
  
  IF @@ROWCOUNT > 0  
  BEGIN  
      update YM_MessagesSample  
   set deleted_at=nullif(deleted_at,char(10)),  
   created_at=nullif(created_at,char(10))    
  
   INSERT INTO YM_Messages   
   SELECT distinct * FROM YM_MessagesSample     
  
  END  
  DELETE FROM YM_MessagesSample  
  exec Yammer_RemoveURLFromAttachments  
 END  
END  
