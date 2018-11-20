create view V_SPThreadDetails
as
SELECT        Del.DocLibName, Del.TopLevelFolderSP, Del.ThreadID, Del.ThreadID_FileName, Del.ThreadID_Size, Del.ThreadID_CreatedDate, Del.ThreadID_Path, Del.TobeDeleted, Del.Deleted, Del.ID, Del.ProcessedBy, 
                         Del.NeedMoreAnalysis, T.OrderID
FROM            Dupthreads_DocLibThreadDetails Del INNER JOIN
                         DupThreads_TopLevelFolderStatus T ON Del.TopLevelFolderSP = T.TopLevelFolderSP AND Del.DocLibName = T.DocLibName