-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[DupThreads_FetchThreadstobeDeleted] 
	(@ProcessedBy nvarchar(100))
	AS
BEGIN
	SET NOCOUNT ON;

	--To be deleted records
	select DocLibName,ThreadID_Filename,ThreadID_Size,ThreadID_Path,ThreadID_CreatedDate 
	FROM dupthreads_doclibThreaddetails WHERE TOBEDELETED = 1 and Deleted =0 
	and ProcessedBy =@ProcessedBy
	order by DocLibName,ThreadID_FileName asc
	--To be retained records
	--select DocLibname,ThreadID_Filename,ThreadID_Size,ThreadID_Path,ThreadID_CreatedDate 
	--FROM dupthreads_doclibThreaddetails WHERE TOBEDELETED = 0 
	   
	
END
