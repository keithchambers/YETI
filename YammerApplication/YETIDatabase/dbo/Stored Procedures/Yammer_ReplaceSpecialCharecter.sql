
CREATE PROC DBO.Yammer_ReplaceSpecialCharecter
As

BEGIN
UPDATE [YETIDB].[DBO].[ym_messages] SET attachments =   REPLACE(attachments,'&','&amp') where attachments like '%&%' 
END
