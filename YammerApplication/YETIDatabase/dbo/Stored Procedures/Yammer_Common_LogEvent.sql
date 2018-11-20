Create Procedure [dbo].[Yammer_Common_LogEvent]
(
	@in_nvarchar_Source nvarchar(100),
	@in_nvarchar_EventType nvarchar(100),
	@in_nvarchar_FileName nvarchar(500),
	@in_nvarchar_ErrorDescription nvarchar(max),
	@processedBy nvarchar(300)
)
as
BEGIN
	Insert into YM_EventLogs(
		[Source]
      ,[EventType]
      ,[FileName]
      ,[ErrorDescription]
      ,[CreatedDate]
      ,[ProcessedBy])
	VALUES(
	@in_nvarchar_Source,
	@in_nvarchar_EventType,
	@in_nvarchar_FileName,
	@in_nvarchar_ErrorDescription,
	getdate(),
	@processedBy);
END
