CREATE procedure [dbo].[Yammer_GetJobIdsToRemove]
as begin
select packageGUID from ym_AzureContainer where jobstatus !='Completed'
end
