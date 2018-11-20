-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[DupThreads_CheckDeleteThreadStatus]
	(@DocLibName nvarchar(100))
	AS
BEGIN


	if NOT EXISTS(SELECT 1 FROM dupthreads_doclibThreaddetails 
	WHERE TobeDeleted=1 and deleted=0 and NeedMoreAnalysis=0 and DocLibName=@DocLibName)
	begin
		return 1
	end
	else
	begin
		return 0
	end

	
END
