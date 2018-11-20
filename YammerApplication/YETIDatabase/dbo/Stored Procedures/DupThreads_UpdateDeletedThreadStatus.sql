-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
create PROCEDURE [dbo].[DupThreads_UpdateDeletedThreadStatus]
	-- Add the parameters for the stored procedure here
	(@ThreadID_Filename nvarchar(100),@ThreadID_Path nvarchar(500),@ThreadID_Size bigint,@DocLibname nvarchar(100))
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	UPDATE Dupthreads_DocLibThreadDetails set deleted = 1 where tobedeleted = 1 and Threadid_Filename = @ThreadID_Filename AND ThreadID_Path = @ThreadID_Path AND ThreadID_Size = @ThreadID_Size AND DocLibName = @DocLibname

END
