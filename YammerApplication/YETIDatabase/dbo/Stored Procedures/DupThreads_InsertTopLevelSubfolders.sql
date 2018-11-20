

CREATE procedure [dbo].[DupThreads_InsertTopLevelSubfolders] 
(@TopLevelSubfolders dbo.TopLevelFoldersList ReadOnly,@DocLibName nvarchar(100)) 
as 
Begin

--modified for daily get toplevelfolders

-- fetch max Orderid 
declare @MAXOderID int
select @MAXOderID = max(orderid) from DupThreads_TopLevelFolderStatus 

	Insert into DupThreads_TopLevelFolderStatus(DocLibName,TopLevelFolderSP,ProcessStage,OrderID)
	select @DocLibName,newTop,'Not started',@MAXOderID+1 from (
	select temp.TopLevelFolders newTop,topLevel.TopLevelFolderSP from @TopLevelSubfolders temp 
	left join DupThreads_TopLevelFolderStatus topLevel
	on temp.TopLevelFolders=topLevel.TopLevelFolderSP and topLevel.DocLibName=@DocLibName
	)t where TopLevelFolderSP is null
	--end modified

----Insert Top Level Folders.
--Insert into DupThreads_TopLevelFolderStatus(TopLevelFolderSP)
--select TopLevelFolders from @TopLevelSubfolders

---- Update status as TopLevelFoldersInserted
--Update DupThreads_DocLibStatus set ProcessStage = 'TopLevelFoldersInserted'
---- DocLibName in Dupthreads_TopLevelFolderStatus table
--update DupThreads_TopLevelFolderStatus set DocLibName = @DocLibName where TopLevelFolderSP in (select TopLevelFolderSP from @TopLevelSubfolders)

End 
