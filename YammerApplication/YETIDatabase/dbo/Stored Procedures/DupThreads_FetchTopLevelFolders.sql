-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[DupThreads_FetchTopLevelFolders]
	-- Add the parameters for the stored procedure here
(@DocLibName nvarchar(100))
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	-- Insert statements for procedure here
	SELECT TopLevelFolderSP from DupThreads_TopLevelFolderStatus WHERE (ProcessStage IS NULL or ProcessStage ='Not started')
	 AND DocLibName = @DocLibName ORDER BY OrderID 
END
