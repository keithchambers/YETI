CREATE TABLE [dbo].[YM_Messages] (
    [id]                      BIGINT         NULL,
    [replied_to_id]           BIGINT         NULL,
    [thread_id]               BIGINT         NULL,
    [conversation_id]         BIGINT         NULL,
    [group_id]                BIGINT         NULL,
    [group_name]              NVARCHAR (MAX) NULL,
    [participants]            NVARCHAR (MAX) NULL,
    [in_private_group]        NVARCHAR (10)  NULL,
    [in_private_conversation] NVARCHAR (10)  NULL,
    [sender_id]               BIGINT         NULL,
    [sender_type]             NVARCHAR (100) NULL,
    [sender_name]             NVARCHAR (300) NULL,
    [sender_email]            NVARCHAR (300) NULL,
    [body]                    NVARCHAR (MAX) NULL,
    [api_url]                 NVARCHAR (MAX) NULL,
    [attachments]             NVARCHAR (MAX) NULL,
    [deleted_by_id]           BIGINT         NULL,
    [deleted_by_type]         NVARCHAR (MAX) NULL,
    [created_at]              DATETIME       DEFAULT (NULL) NULL,
    [deleted_at]              DATETIME       DEFAULT (NULL) NULL,
    [processed_by]            NVARCHAR (150) NULL,
    [csvfilename]             NVARCHAR (250) NULL,
    [process_status]          NVARCHAR (100) NULL
);


GO
CREATE CLUSTERED INDEX [IX_ymmessages_id]
    ON [dbo].[YM_Messages]([id] ASC);

