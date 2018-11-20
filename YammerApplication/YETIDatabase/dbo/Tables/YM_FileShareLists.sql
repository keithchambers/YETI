CREATE TABLE [dbo].[YM_FileShareLists] (
    [ShareId]        INT            IDENTITY (1, 1) NOT NULL,
    [FilePath]       NVARCHAR (150) NOT NULL,
    [SharedFilePath] NVARCHAR (150) NULL,
    [PathType]       NVARCHAR (50)  NOT NULL,
    [isLargePath]    BIT            DEFAULT ((0)) NULL
);

