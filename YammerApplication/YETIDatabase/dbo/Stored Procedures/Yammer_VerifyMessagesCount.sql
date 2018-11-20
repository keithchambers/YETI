
CREATE PROCEDURE [dbo].[Yammer_VerifyMessagesCount] (@CSVFILE NVARCHAR(MAX),@RowCount bigint,@FilesCount bigint,@PagesCount bigint,@GroupsCount bigint,@TopicsCount bigint,@UsersCount bigint, @IsFailed BIT OUT)
AS
BEGIN

--;WITH tblTemp as
--(
--SELECT 
--  ROW_NUMBER() OVER(PARTITION BY id ORDER BY id ASC) 
--    AS RowNumber,*
--  FROM ym_messages
--)
--DELETE FROM tblTemp where RowNumber >1 ;

	DECLARE @FailedOutPut BIT = 0
	IF(@RowCount > 0)
	BEGIN
		IF((SELECT count(distinct id) MessageCount FROM YM_Messages WHERE csvfilename = @CSVFILE) = @RowCount)
		BEGIN
			UPDATE YM_ExportDetails SET IsVerified = 1,Status ='DBLoaded' , ProcessStage = 'CSVProcessing', MessagesCount = @RowCount,FilesCount=@FilesCount,PagesCount=@PagesCount,GroupsCount=@GroupsCount,TopicsCount=@TopicsCount,UsersCount=@UsersCount,ModifiedDate = GETDATE() WHERE FileName = @CSVFILE
		END
		ELSE 
		BEGIN
			SET @FailedOutPut = 1
			UPDATE YM_ExportDetails SET IsVerified = 0,Status ='DBLoadFailed' , ProcessStage = 'CSVProcessFailed',MessagesCount = @RowCount,FilesCount=@FilesCount,PagesCount=@PagesCount,GroupsCount=@GroupsCount,TopicsCount=@TopicsCount,UsersCount=@UsersCount,ModifiedDate = GETDATE() WHERE FileName = @CSVFILE
		END
	
		IF( (@FailedOutPut = 1) AND ((SELECT count(distinct id) MessageCount FROM YM_Messages WHERE csvfilename = @CSVFILE) + (SELECT count(distinct id) MessageCount FROM YM_Messages WHERE process_status like '%' + @CSVFILE + '%')) = @RowCount) 
		BEGIN
			UPDATE [dbo].[YM_Messages]
			   SET [body] = [body] + [api_url]
				  ,[api_url] = [attachments]
				  ,[attachments] = [deleted_by_id]
				  ,[deleted_by_id] = [deleted_by_type]
				  ,[deleted_by_type] = [created_at]
				  ,[created_at] = [deleted_at]
				  ,[deleted_at] = [processed_by]
				  ,[processed_by] = [csvfilename]
				  ,[csvfilename] = @CSVFILE
				  ,[process_status] = NULL
			 WHERE process_status like '%' + @CSVFILE + '%'
			 IF((SELECT count(distinct id) MessageCount FROM YM_Messages WHERE csvfilename = @CSVFILE) = @RowCount)
			 BEGIN
				SET @FailedOutPut = 0
				UPDATE YM_ExportDetails SET IsVerified = 1,Status ='DBLoaded' , ProcessStage = 'CSVProcessing', MessagesCount = @RowCount,FilesCount=@FilesCount,PagesCount=@PagesCount,GroupsCount=@GroupsCount,TopicsCount=@TopicsCount,UsersCount=@UsersCount,ModifiedDate = GETDATE() WHERE FileName = @CSVFILE
			END
		END

		IF(((SELECT COUNT(1) FROM YM_Messages WHERE csvfilename = @CSVFILE AND api_url != 'https://www.yammer.com/api/v1/messages/'+CAST(id as varchar)) > 0) AND (@FailedOutPut = 0))
		BEGIN
			update ym_messages set body = body + substring(api_url,0,charindex('https:',api_url,0)),
			api_url = substring(api_url,charindex('https:',api_url,0),len(api_url))
			where csvfilename = @CSVFILE AND api_url like '%https://www.yammer.com/api/v1/messages/'+CAST(id as varchar)

			IF((SELECT COUNT(1) FROM YM_Messages WHERE csvfilename = @CSVFILE AND api_url != 'https://www.yammer.com/api/v1/messages/'+CAST(id as varchar)) > 0)
			BEGIN
				SET @FailedOutPut = 1
				UPDATE YM_ExportDetails SET IsVerified = 0,Status ='DBLoadFailed' , ProcessStage = 'CSVProcessFailed', MessagesCount = @RowCount,FilesCount=@FilesCount,PagesCount=@PagesCount,GroupsCount=@GroupsCount,TopicsCount=@TopicsCount,UsersCount=@UsersCount,ModifiedDate = GETDATE() WHERE FileName = @CSVFILE
				--UPDATE YM_Messages SET process_status = 'DBLoadFailed' WHERE ID IN (SELECT ID FROM YM_Messages WHERE csvfilename = @CSVFILE AND api_url != 'https://www.yammer.com/api/v1/messages/'+CAST(id as varchar))
			END
		END
	
		SELECT @IsFailed = @FailedOutPut
	
		IF(@IsFailed = 1)
		BEGIN
			DELETE FROM YM_Files WHERE csvfilename = @CSVFILE
			DELETE FROM YM_Pages WHERE csvfilename = @CSVFILE
			DELETE FROM YM_Users WHERE csvfilename = @CSVFILE
			DELETE FROM YM_Topics WHERE csvfilename = @CSVFILE
			DELETE FROM YM_Messages WHERE csvfilename = @CSVFILE
		END
	END
	ELSE
	BEGIN
		SELECT @IsFailed = 0
		UPDATE YM_ExportDetails SET IsVerified = 1,Status ='NoThreads' , ProcessStage = 'CSVProcessed',MessagesCount = @RowCount,FilesCount=@FilesCount,PagesCount=@PagesCount,GroupsCount=@GroupsCount,TopicsCount=@TopicsCount,UsersCount=@UsersCount,ModifiedDate = GETDATE() WHERE FileName = @CSVFILE
	END
END
