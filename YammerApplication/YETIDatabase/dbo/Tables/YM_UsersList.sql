CREATE TABLE [dbo].[YM_UsersList] (
    [UserId]     BIGINT         NULL,
    [FullName]   NVARCHAR (MAX) NULL,
    [EmailAlias] NVARCHAR (MAX) NULL
);


GO
CREATE CLUSTERED INDEX [indx_um_Userlist_Userid]
    ON [dbo].[YM_UsersList]([UserId] ASC);

