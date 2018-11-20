CREATE PROCEDURE [dbo].[Yammer_Update_ProcessCompletion] (@Year NVARCHAR(10), @isRangeInDays BIT, @ToCompress INT, @RangeInMonths INT)
AS
if(@isRangeInDays = 1)
begin
	if((select count(1) from ym_messages) = 0)
	begin
	-- updating toProcess to 0 to wait or other steps to complete in daily processing
		update ym_yammeryears set ToProcess = 0 where ymyear = @Year
	end
	--update ym_yammeryears set ToCompress = @ToCompress where ymyear = @Year
	
	
	--if exists(select 1 from YM_ExportDetails where Status='PageDownloaded')
	----if exists(select 1 from ym_messages )
	--begin
	--	update ym_yammeryears set ToCompress = 0 where ymyear = @Year
	--end 
	--else
	--begin
	--	update ym_yammeryears set ToCompress = @ToCompress where ymyear = @Year
	--end

	--Commented on 517
	--if not exists(select 1 from YM_ExportDetails where Status='PageDownloaded')
	----if exists(select 1 from ym_messages )
	--begin
	--	update ym_yammeryears set ToCompress = @ToCompress where ymyear = @Year
	--end 
		--Commented on 517
	
end