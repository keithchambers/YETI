
CREATE procedure [dbo].[Yammer_DeleteJobIds] (@jobId nvarchar(max))
as begin
delete from ym_AzureContainer where jobstatus !='Completed' and  packageGUID = @jobId
end