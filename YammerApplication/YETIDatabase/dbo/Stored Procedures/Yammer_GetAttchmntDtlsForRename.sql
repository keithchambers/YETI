	Create Procedure [dbo].[Yammer_GetAttchmntDtlsForRename]
	(	
	@filter nvarchar(100),
	@processedBy nvarchar(100)	
	)
	as
	BEGIN
	 
	  if @filter = 'uploadedfile'
                    
       select ROW_NUMBER() OVER (PARTITION BY file_id ORDER BY id desc) VerCount,id,file_id,name from  (
        select distinct versionId as id,fileId as file_id,FileName  as name from YM_FileThreadList 
        where csvfilename in (select CsvFileName from YM_Messages where process_status = 'PageVersionsLoaded' 
        and attachments is not null and processed_by = @processedBy ) ) A ;
                    
      else
                  
       select ROW_NUMBER() OVER (PARTITION BY file_id ORDER BY id desc) VerCount,id,file_id,name from  (
        select distinct versionId as id,PageId as file_id,PageName as name from YM_PageThreadList 
        where csvfilename in (select CsvFileName from YM_Messages where process_status = 'FilesRenamed' 
        and attachments is not null and processed_by = @processedBy ) ) A;
                    

	END
