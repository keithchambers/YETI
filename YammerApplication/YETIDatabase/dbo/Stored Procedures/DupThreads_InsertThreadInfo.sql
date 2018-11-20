CREATE Procedure [dbo].[DupThreads_InsertThreadInfo]
(@SPFileInfo dbo.SPFileInfoTableType READONLY)
as 
Begin
	--added for daily insert new file list
	--update thread  set [TobeDeleted]=1,[Deleted]=0 
	--from DupThreads_DocLibThreadDetails thread join
	--@SPFileInfo temp on thread.[ThreadID_FileName]=temp.Thread_FileName 
	--and thread.[DocLibName]=temp.[DocLibName] 
	----where thread.[NeedMoreAnalysis]<>1 and thread.[ThreadID_Size]<=temp.Thread_FileSize 

	--update all old files to be deleted
	update DupThreads_DocLibThreadDetails
	set [TobeDeleted]=1,[Deleted]=0 
	where ThreadID_FileName in (select Thread_FileName from @SPFileInfo) 
	and TobeDeleted=0 and (deleted=0 or deleted is null)
	
	--end added

	insert into DupThreads_DocLibThreadDetails(
	DocLibName,TopLevelFolderSP,ThreadID_FileName,ThreadID_Size,ThreadID_CreatedDate,ThreadID_Path,
	tobeDeleted,Deleted,processedBy)
	select DocLibName,TopLevelSubFolder,Thread_FileName, Thread_FileSize,Thread_CreatedDate,Thread_FilePath
	,0,0,'AZWU2YETIPFS04'
	 from @SPFileInfo

	update DupThreads_TopLevelFolderStatus set ProcessStage = 'ThreadInfoInserted' where DocLibName =(select Top 1 DocLibName from @SPFileInfo) and TopLevelFolderSP= (select Top 1 TopLevelSubFolder from @SPFileInfo)


end