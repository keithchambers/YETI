Create PROCEDURE [dbo].[Yammer_Update_YearStatus] (@Year NVARCHAR(10), @ToProcess int,@ToCompress INT,@ToSPUpload int,@ToDedupe int)
AS

begin
	update	ym_yammeryears set ToProcess = @ToProcess,ToCompress = @ToCompress,ToSPUpload =@ToSPUpload,ToDeduplicate = @ToDedupe 
	where ymyear = @Year
end