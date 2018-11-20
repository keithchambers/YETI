Create Procedure [dbo].[Yammer_UpdateThreadStatus]
 (@in_nvarchar_newStatus nvarchar(100),
	@in_nvarchar_prevStatus nvarchar(100),
	@processedBy nvarchar(300),
	@in_datatable_ThreadIds dbo.IDTableType READONLY)
	as
begin
	update YM_Messages set process_status=@in_nvarchar_newStatus
	where (thread_id in (select ID from @in_datatable_ThreadIds) or id in (select ID from @in_datatable_ThreadIds))
	and ISNULL(process_status,'')=@in_nvarchar_prevStatus and processed_by=@processedBy
end
