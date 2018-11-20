CREATE PROCEDURE [dbo].[Yammer_PurgeProcessedData] (@Processed_By NVARCHAR(100))
AS
BEGIN
	
	IF((SELECT COUNT(1) FROM YM_Messages WHERE processed_by = @Processed_By AND ISNULL(process_status,'') != 'FilesVerified') = 0)
	BEGIN
		DELETE FROM YM_Files WHERE processed_by = @Processed_By AND csvfilename IN ( SELECT csvfilename FROM YM_Messages WHERE processed_by = @Processed_By AND process_status = 'FilesVerified')
		DELETE FROM YM_Pages WHERE processed_by = @Processed_By AND csvfilename IN ( SELECT csvfilename FROM YM_Messages WHERE processed_by = @Processed_By AND process_status = 'FilesVerified')
		DELETE FROM YM_Users WHERE processed_by = @Processed_By AND csvfilename IN ( SELECT csvfilename FROM YM_Messages WHERE processed_by = @Processed_By AND process_status = 'FilesVerified')
		DELETE FROM YM_Topics WHERE processed_by = @Processed_By AND csvfilename IN ( SELECT csvfilename FROM YM_Messages WHERE processed_by = @Processed_By AND process_status = 'FilesVerified')
		DELETE FROM YM_Messages WHERE processed_by = @Processed_By AND process_status = 'FilesVerified'
	END

	if not exists(select * from YM_Messages )
	begin 
		update	ym_yammeryears set ToProcess = 0,ToCompress = 1,ToSPUpload =0,ToDeduplicate = 0 
		where ToProcess=1
	end
END
