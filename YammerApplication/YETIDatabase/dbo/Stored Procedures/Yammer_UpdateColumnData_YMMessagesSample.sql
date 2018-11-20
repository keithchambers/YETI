-- =============================================
-- Author:		<v-gotraj,,Rajender Gottipamula>
-- Create date: <09/28/2018>
-- Description:	<Update the Ym_MessagesSample Table by inserting data in desired column >
-- =============================================
CREATE PROCEDURE dbo.Yammer_UpdateColumnData_YMMessagesSample
	AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

   update YM_MessagesSample set api_url=attachments ,attachments=deleted_by_id, deleted_by_id=deleted_by_type,deleted_by_type=created_at,created_at=deleted_at,deleted_at=processed_by,processed_by=csvfilename,csvfilename=process_status, process_status=Null 
   where csvfilename='AZWU2YETIPFS04' 


END
