// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Threading.Tasks;
using YammerLibrary;

namespace ConsoleSharePointApplication
{
    class Program
    {
        static string uploadStartDate = string.Empty;
        static string uploadEndDate = string.Empty;
        static string Year = string.Empty;
        static long ThreadCount = 0;
        static string SPDirPath = string.Empty;
        private static string conn = string.Empty;
        private static string serviceAccountName = string.Empty;
        private static string serviceAccountPassword = string.Empty;
        static void Main(string[] args)
        {

            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
           

            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["DBNameURL"];
            conn = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerAcctNameURL"];
            SharePointClassLibrary.Configuration.UserName = await getSecretCmdlet.GetSecretAsync();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerAcctPWDURL"];
            SharePointClassLibrary.Configuration.PassWord = await getSecretCmdlet.GetSecretAsync();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerSPAzAccountKey"];
            Configuration.azureAccountKey = await getSecretCmdlet.GetSecretAsync();
            LogEvents("Information", "YM_SharepointUpload", "SP Upload Process started");

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_GetYearForSharepoint(Environment.MachineName);
            }

            
            if (string.IsNullOrEmpty(Year))
                Environment.Exit(0);
            Configuration.targetDocLib = Configuration.targetDocLib + Year;
            LogEvents("Information", "YM_SharepointUpload", "Resubmit Process started");
            ResubmitFailedBatches();
            LogEvents("Information", "YM_SharepointUpload", "Resubmit Process completed");
            LogEvents("Information", "YM_SharepointUpload", "Submit New Process started");
            SubmitNewBatchs();
            LogEvents("Information", "YM_SharepointUpload", "Submit New Process completed");
            LogEvents("Information", "YM_SharepointUpload", "Check Job status Process started");
            CheckRunningJobs();
            LogEvents("Information", "YM_SharepointUpload", "Check Job status Process completed");
        }

        private static void SubmitNewBatchs()
        {
            try
            {
               

                List<Yammer_GetSPDirectoryForUpload_Result> SPDirectories = new List<Yammer_GetSPDirectoryForUpload_Result>();
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    SPDirectories=  yeticontext.Yammer_GetSPDirectoryForUpload(Convert.ToInt32(Year)).ToList();
                }

              
                
                    if (SPDirectories.Count > 0)
                    {
                        var isoDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
                    SPDirPath = SPDirectories[0].FolderPath;
                        ThreadCount = Convert.ToInt64(SPDirectories[0].ThreadCount);
                        uploadStartDate = Convert.ToDateTime(SPDirectories[0].UploadStartDate).ToString(isoDateTimeFormat.SortableDateTimePattern); //);
                        uploadEndDate = Convert.ToDateTime(SPDirectories[SPDirectories.Count-1].UploadEndDate).ToString(isoDateTimeFormat.SortableDateTimePattern);
                        Configuration.SPMappingId = Convert.ToInt32(SPDirectories[SPDirectories.Count-1].Id);
                    }
                
                

                if (!string.IsNullOrEmpty(uploadEndDate))
                {
                    LogEvents("Information", "YM_SharepointUpload", "Splitting folders");
                    SplitFolders();
                    LogEvents("Information", "YM_SharepointUpload", "ArrangeGroupsForBatches");
                    ArrangeGroupsForBatches();
                    LogEvents("Information", "YM_SharepointUpload", "SplitFoldersForBatches");
                    SplitFoldersForBatches();
                    LogEvents("Information", "YM_SharepointUpload", "Year " + Year + " is ready to upload to sharepoint");
                    DataSet JobIdDs = new DataSet();
                    LogEvents("Information", "YM_SharepointUpload", "Checking if any Job is in progress");
                    if (!CheckAnyBatchInProgress(true))
                    {
                        LogEvents("Information", "YM_SharepointUpload", "Submitting batch");
                        bool islibraryCreated = false;
                        try
                        {
                            islibraryCreated = SharePointClassLibrary.AzureSharePointHelper.CreateDocumentLibrary(Configuration.targetDocLib);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "A list, survey, discussion board, or document library with the specified title already exists in this Web site.  Please choose another title.")
                                islibraryCreated = true;
                            else
                            {
                                islibraryCreated = false;
                                LogEvents("Error", "YM_SharepointUpload", "LibraryCreation Failed. " + ex.ToString());
                            }
                        }
                        if (islibraryCreated)
                            SubmitBatchToExchange(Configuration.targetDocLib, true);
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "SubmitNew", ex.ToString());
            }
        }

        private static void ResubmitFailedBatches()
        {
            if (!CheckAnyBatchInProgress(false))
            {
                SubmitBatchToExchange(Configuration.targetDocLib, false);
            }
        }
        private static void UpdateSPMapping(int BatchNo)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                
                yeticontext.Yammer_UpdateSPMapping(BatchNo, Convert.ToInt32(Year), Convert.ToDateTime(uploadStartDate), Convert.ToDateTime(uploadEndDate), Environment.MachineName);
            }

        }

        private static void CheckRunningJobs()
        {
            DataSet JobIds = new DataSet();
            CheckAnyContainerInProgress(out JobIds);
            foreach (DataRow JobIdDr in JobIds.Tables[0].Rows)
            {
                if (GetJobStatus(JobIdDr[0].ToString()) == "Completed")
                {
                    if (VerifyGroupsFiles(JobIdDr[0].ToString(), JobIdDr[1].ToString(), JobIdDr[2].ToString(), JobIdDr["GroupName"].ToString(), JobIdDr["GroupType"].ToString(), Convert.ToInt32(JobIdDr["ItemCount"]), Convert.ToInt32(JobIdDr["BatchNo"]), JobIdDr["FolderPath"].ToString()))
                    {
                        //Uncomment this part to delete the files once they are uploaded to sharepoint
                        //if (Directory.Exists(JobIdDr["SourceFolderPath"].ToString()))
                        //{
                        //    Directory.EnumerateFiles(JobIdDr["SourceFolderPath"].ToString(), "*.*", SearchOption.AllDirectories).ToList().ForEach(File.Delete);
                        //    Thread.Sleep(500);
                        //    Directory.EnumerateDirectories(JobIdDr["SourceFolderPath"].ToString(), "*.*", SearchOption.AllDirectories).ToList().ForEach(Directory.Delete);
                        //    Thread.Sleep(500);
                        //    Directory.Delete(JobIdDr["SourceFolderPath"].ToString());
                        //    try
                        //    {
                        //        Directory.Delete(JobIdDr["FolderPath"].ToString());
                        //    }
                        //    catch(Exception ex)
                        //    {

                        //    }
                        //}
                    }
                }
            }
        }
        private static void CheckAnyContainerInProgress(out DataSet JobIdListDs)
        {
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(conn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Yammer_CheckAnyContainerInProgress";
                    cmd.Parameters.AddWithValue("ymyear", Year);
                    cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                }
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count == 0)
                    {
                        JobIdListDs = ds;
                    }
                }
                JobIdListDs = ds;
            }
            catch (Exception ex)
            {
                JobIdListDs = new DataSet();
                LogEvents("Error", "YM_SharepointUpload", ex.ToString());
                Environment.Exit(0);
            }
        }

        private static bool CheckAnyBatchInProgress(bool isNew)
        {
            try
            {
                int returnValue = 0;

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    returnValue = Convert.ToInt32(yeticontext.Yammer_CheckBatchInProgress(Convert.ToInt32(Year),Environment.MachineName));
                }
                if (isNew)
                {
                    if (returnValue >= Configuration.totalNewBatchCount)
                        return true;
                    else
                    {
                        Configuration.totalNewBatchCount = Configuration.totalNewBatchCount - returnValue;
                        return false;
                    }
                }
                else
                {
                    if (returnValue == 0)
                        return true;
                    if (returnValue >= Configuration.totalResubmitBatchCount)
                        return true;
                    else
                    {
                        Configuration.totalResubmitBatchCount = Configuration.totalResubmitBatchCount - returnValue;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "YM_SharepointUpload", ex.ToString());
                Environment.Exit(0);
                return true;
            }
        }

        static void SplitFoldersForBatches()
        {
            DataTable folderList = new DataTable("Table_ID");
            folderList.Columns.Add("FolderPath", System.Type.GetType("System.String"));
            folderList.Columns.Add("ItemCount", System.Type.GetType("System.Int32"));
            folderList.Columns.Add("GroupName", System.Type.GetType("System.String"));
            folderList.Columns.Add("GroupType", System.Type.GetType("System.String"));
            folderList.Columns.Add("FileName", System.Type.GetType("System.String"));

            List<string> GroupNameDirLists = Directory.EnumerateDirectories(SPDirPath).ToList<string>();
            foreach (string GroupNameDirList in GroupNameDirLists)
            {
                DirectoryInfo dir = new DirectoryInfo(GroupNameDirList);
                List<string> GroupSubDirLists = Directory.EnumerateDirectories(dir.FullName).ToList<string>();
                foreach (string GroupSubDirList in GroupSubDirLists)
                {
                    DirectoryInfo subdir = new DirectoryInfo(GroupSubDirList);
                    if (dir.Name == SharePointClassLibrary.CommonHelper.FrameFolderName(uploadStartDate, uploadEndDate, false))
                    {
                        folderList.Rows.Add(GroupSubDirList, Directory.EnumerateFiles(GroupSubDirList, "*,*", SearchOption.AllDirectories).Count(), dir.Name.ToString() + "/" + subdir.Name.ToString(), "Multi", null);
                    }
                    else
                    {
                        folderList.Rows.Add(GroupSubDirList, Directory.EnumerateFiles(GroupSubDirList, "*,*", SearchOption.AllDirectories).Count(), dir.Name.ToString() + "/" + subdir.Name.ToString(), "Single", null);
                    }
                }
            }



            using (SqlConnection con = new SqlConnection(conn))
            {
                con.Open();
                SqlCommand sqlcmd = new SqlCommand();
                sqlcmd.Connection = con;
                sqlcmd.CommandType = CommandType.StoredProcedure;
                sqlcmd.CommandText = "Yammer_LoadSPBatchLists";
                sqlcmd.Parameters.AddWithValue("YEAR", Convert.ToInt32(Year));
                sqlcmd.Parameters.AddWithValue("folderPathCount", folderList);
                sqlcmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                sqlcmd.Parameters.AddWithValue("SPMappingId", Configuration.SPMappingId);
                sqlcmd.ExecuteNonQuery();
                con.Close();
            }
        }

        static void SubmitBatchToExchange(string libraryName, bool isNew)
        {
            try
            {
                DataSet ds = new DataSet();
                PSCredential cred = GetCredential(SharePointClassLibrary.Configuration.UserName, SharePointClassLibrary.Configuration.PassWord);
                using (SqlConnection con = new SqlConnection(conn))
                {
                    con.Open();
                    SqlCommand sqlcmd = new SqlCommand();
                    sqlcmd.Connection = con;
                    sqlcmd.CommandType = CommandType.StoredProcedure;
                    sqlcmd.CommandText = (isNew) ? "Yammer_GetBatchToSubmit" : "Yammer_GetBatchToReSubmit";
                    sqlcmd.Parameters.AddWithValue("ymyear", Convert.ToInt32(Year));
                    sqlcmd.Parameters.AddWithValue("batchCount", (isNew) ? Configuration.totalNewBatchCount : Configuration.totalResubmitBatchCount);
                    sqlcmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                    SqlDataAdapter da = new SqlDataAdapter(sqlcmd);
                    da.Fill(ds);
                    con.Close();
                }
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        LogEvents("Information", "YM_SharepointUpload", "Batch creation started");
                        foreach (DataRow batchDrow in ds.Tables[0].Rows)
                        {
                            LogEvents("Information", "YM_SharepointUpload", batchDrow["BatchNo"] + " started");
                            if (!isNew)
                                Configuration.SPMappingId = Convert.ToInt32(batchDrow["SPMappingId"]);
                            int BatchNo = Convert.ToInt32(batchDrow["BatchNo"]);
                            string sourceFilePath = batchDrow["FolderPath"].ToString();
                            string GroupName = batchDrow["GroupName"].ToString();
                            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "SourcePackage\\SourcePackage" + BatchNo))
                                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "SourcePackage\\SourcePackage" + BatchNo);

                            string sourcePackage = AppDomain.CurrentDomain.BaseDirectory + "SourcePackage\\SourcePackage" + BatchNo;

                            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "TargetPackage\\TargetPackage" + BatchNo))
                                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "TargetPackage\\TargetPackage" + BatchNo);

                            string targetPackage = AppDomain.CurrentDomain.BaseDirectory + "TargetPackage\\TargetPackage" + BatchNo;
                            LogEvents("Information", "YM_SharepointUpload", "Package folder created");
                            ExecuteBatch(cred, sourceFilePath, GroupName, sourcePackage, targetPackage, BatchNo, Convert.ToInt32(batchDrow["ItemCount"]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "SubmitBatch", ex.ToString());
            }
        }

        static bool ExecuteBatch(PSCredential cred, string sourceFilePath, string groupFolderName, string sourcePackage, string targetPackage, int BatchNo, int ItemCount)
        {
            try
            {
                LogEvents("Information", "YM_SharepointUpload", "New bacth creation");
                List<Command> cmds = new List<Command>();
                List<ErrorRecord> errors = new List<ErrorRecord>();
                PSCommand cmd = new PSCommand();
                cmd.AddCommand("New-SPOMigrationPackage");
                cmd.AddParameter("-SourceFilesPath", sourceFilePath);
                cmd.AddParameter("-OutputPackagePath", sourcePackage);
                cmd.AddParameter("-IncludeFileSharePermissions");
                cmd.AddParameter("-TargetWebUrl", Configuration.Url);
                cmd.AddParameter("-TargetDocumentLibraryPath", Configuration.targetDocLib);
                cmd.AddParameter("-TargetDocumentLibrarySubFolderPath", groupFolderName);
                Results result = RunComand(cmd);

                var directory = new DirectoryInfo(sourcePackage);
                var logFile = directory.GetFiles("CreateMigrationPackage*.log")
                 .OrderByDescending(f => f.LastWriteTime)
                 .First();
                StreamReader reader = System.IO.File.OpenText(logFile.FullName);
                string line;
                List<string> errorList = new List<string>();
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split('\t');
                    if (items.Count() > 1)
                    {
                        if (items[1].Contains("Error"))
                        {
                            errorList.Add(items[2]);
                        }
                    }
                }

                if (errorList.Count > 0)
                {
                    LogEvents("Error", logFile.Name, "Issue with upload check the log file @: " + logFile.FullName);
                    return false;
                }
                LogEvents("Information", "SharePoint", "Package Created");
                PSCommand cmd1 = new PSCommand();
                cmd1.AddCommand("ConvertTo-SPOMigrationTargetedPackage");
                cmd1.AddParameter("-SourceFilesPath", sourceFilePath);
                cmd1.AddParameter("-SourcePackagePath", sourcePackage);
                cmd1.AddParameter("-OutputPackagePath", targetPackage);
                cmd1.AddParameter("-TargetWebUrl", Configuration.Url);
                cmd1.AddParameter("-TargetDocumentLibraryPath", Configuration.targetDocLib);
                cmd1.AddParameter("-TargetDocumentLibrarySubFolderPath", groupFolderName);
                cmd1.AddParameter("-Credentials", cred);
                result = RunComand(cmd1);

                directory = new DirectoryInfo(targetPackage);
                logFile = directory.GetFiles("ConvertMigrationPackage*.log")
                 .OrderByDescending(f => f.LastWriteTime)
                 .First();
                reader = System.IO.File.OpenText(logFile.FullName);
                line = string.Empty;
                errorList = new List<string>();
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split('\t');
                    if (items.Count() > 1)
                    {
                        if (items[1].Contains("Error"))
                        {
                            errorList.Add(items[2]);
                        }
                    }
                }

                if (errorList.Count > 0)
                {
                    LogEvents("Error", logFile.Name, "Issue with upload check the log file @: " + logFile.FullName);
                    return false;
                }
                LogEvents("Information", "SharePoint", "Package Convertion Completed");
                PSCommand cmd2 = new PSCommand();
                cmd2.AddCommand("Set-SPOMigrationPackageAzureSource");
                cmd2.AddParameter("-SourceFilesPath", sourceFilePath);
                cmd2.AddParameter("-SourcePackagePath", targetPackage);
                cmd2.AddParameter("-AccountName", Configuration.azureAccountName);
                cmd2.AddParameter("-AccountKey", Configuration.azureAccountKey);
                result = RunComand(cmd2);

                errors = result.Errors;

                directory = new DirectoryInfo(targetPackage);
                logFile = directory.GetFiles("CopyMigrationPackage*.log")
                 .OrderByDescending(f => f.LastWriteTime)
                 .First();
                reader = System.IO.File.OpenText(logFile.FullName);
                line = string.Empty;
                errorList = new List<string>();
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split('\t');
                    if (items.Count() > 1)
                    {
                        if (items[1].Contains("Error") && !items[2].Contains("file timed out (max time"))
                        {
                            errorList.Add(items[2]);
                        }
                    }
                }

                if (errorList.Count > 0)
                {
                    LogEvents("Error", logFile.Name, "Issue with upload check the log file @: " + logFile.FullName);
                    return false;
                }

                LogEvents("Information", "SharePoint", "Azure upload completed");
                Collection<PSObject> ObjReturned = result.ObjReturned;
                string FileContainerUri = string.Empty;
                string PackageContainerUri = string.Empty;
                string ReportingQueueUri = string.Empty;
                dynamic obj = ObjReturned[0].BaseObject;
                try
                {
                    FileContainerUri = ((dynamic)obj).FileContainerUri.ToString();
                    PackageContainerUri = ((dynamic)obj).PackageContainerUri.ToString();
                    ReportingQueueUri = ((dynamic)obj).ReportingQueueUri.ToString();
                }
                catch (Exception ex)
                {
                    return false;
                }
                LogEvents("Information", "ContainerName", FileContainerUri);
                if (errors.Count == 0)
                {
                    cmds = new List<Command>();
                    PSCommand cmd3 = new PSCommand();
                    cmd3.AddCommand("Submit-SPOMigrationJob");
                    cmd3.AddParameter("-FileContainerUri", FileContainerUri.Trim());
                    cmd3.AddParameter("-PackageContainerUri", PackageContainerUri.Trim());
                    cmd3.AddParameter("-TargetWebUrl", Configuration.Url.Substring(0, Configuration.Url.Length - 1));
                    cmd3.AddParameter("-Credentials", cred);

                    result = RunComand(cmd3);
                    errors = result.Errors;
                    string configGUID = result.ObjReturned[0].ToString();
                    LogEvents("Information", "ContainerName", "Package submitted to SharePoint.");

                    if (errors.Count == 0)
                    {
                        string containerName = FileContainerUri.Split('?')[0];
                        containerName = containerName.Substring(containerName.LastIndexOf("/") + 1);
                        containerName = containerName.Substring(0, containerName.LastIndexOf("-"));
                        UpdateBatchStatus(BatchNo, "Processing");

                        using (YETIDBEntities yeticontext = new YETIDBEntities())
                        {
                            yeticontext.Yammer_ContainerMapping(containerName,configGUID, Convert.ToInt32(Year),Configuration.targetDocLib, "Processing", "PackageSubmitted",Environment.MachineName,sourceFilePath,ItemCount,FileContainerUri,PackageContainerUri,ReportingQueueUri,BatchNo);
                        }

                        
                        
                    }
                    else
                        return false;
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogEvents("Error", "SubmitBatchToExchange", ex.ToString());
                return false;
            }
        }

        private static void UpdateBatchStatus(int batchNo, string status, int count = 0)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_UpdateBatchStatus(batchNo, status, Convert.ToInt32(ConfigurationManager.AppSettings["TimesTried"]), count);
            }

            
        }

        static string GetJobStatus(string jobId)
        {
            PSCredential cred = GetCredential(SharePointClassLibrary.Configuration.UserName, SharePointClassLibrary.Configuration.PassWord);
            List<Command> cmds = new List<Command>();
            List<ErrorRecord> errors = new List<ErrorRecord>();
            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-SPOMigrationJobStatus");
            cmd.AddParameter("-JobId", jobId);
            cmd.AddParameter("-Credentials", cred);
            cmd.AddParameter("-TargetWebUrl", Configuration.Url);
            Results result = RunComand(cmd);
            Collection<PSObject> ObjReturned = result.ObjReturned;
            return (ObjReturned[0].BaseObject.ToString() != "None") ? ObjReturned[0].BaseObject.ToString() : "Completed";
        }

        static Results RunComand(PSCommand cmds)
        {
            Results results = new Results();
            try
            {
                Runspace rs;
                rs = RunspaceFactory.CreateRunspace();
                rs.Open();
                List<ErrorRecord> errors = new List<ErrorRecord>();
                Collection<PSObject> retrval = new Collection<PSObject>();
                using (PowerShell ps = PowerShell.Create())
                {
                    ps.Runspace = rs;
                    ps.Commands = cmds;
                    retrval = ps.Invoke();

                    PSDataCollection<ErrorRecord> errorRecords = ps.Streams.Error;
                    var errorList = errorRecords.ToList();

                    results.ObjReturned = retrval;
                    results.Errors = errorList;

                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "RunCommand", ex.ToString());
            }
            return results;
        }

        static PSCredential GetCredential(string userName, string passWord)
        {

            SecureString pwd = new SecureString();
            foreach (char c in passWord.ToCharArray())
            {
                pwd.AppendChar(c);
            }
            PSCredential credential = new PSCredential(userName, pwd);
            return credential;
        }
        public class Results
        {
            public Collection<PSObject> ObjReturned { get; set; }
            public List<ErrorRecord> Errors { get; set; }
        }
        private static void SplitFolders()
        {
            try
            {
                long itemCount = Convert.ToInt64(ConfigurationManager.AppSettings["ItemCount"]);
                long folderCount = 1;

                foreach (var dir in Directory.GetDirectories(SPDirPath))
                {
                    folderCount = Directory.GetDirectories(dir, "*.*", SearchOption.TopDirectoryOnly).Length + 1;
                    if (Directory.EnumerateFiles(dir).Count() > itemCount)
                    {
                        while (Directory.EnumerateFiles(dir).Count() != 0)
                        {
                            DirectoryCopy(dir, dir + "\\" + folderCount.ToString(), itemCount);
                            folderCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "YM_SharepointUpload", ex.Message);
                Environment.Exit(0);
            }
        }

        public static void DirectoryCopy(string strSource, string Copy_dest, long itemCount)
        {
            int i = 0;
            DirectoryInfo dirInfo = new DirectoryInfo(strSource);

            if (!Directory.Exists(Copy_dest))
                Directory.CreateDirectory(Copy_dest);// creating the destination Directory   

            foreach (string tempfile in Directory.EnumerateFiles(strSource))
            {
                if (!File.Exists(Copy_dest + "/" + tempfile))
                {
                    FileInfo f = new FileInfo(tempfile);
                    f.MoveTo(Path.Combine(strSource + "/" + tempfile, Copy_dest + "/" + f.Name));
                    i++;
                }
                if (i == itemCount)
                    return;
            }
        }

        private static void ArrangeGroupsForBatches()
        {
            try
            {
                long itemCount = Convert.ToInt64(ConfigurationManager.AppSettings["ItemCount"]);
                string groupsName = SharePointClassLibrary.CommonHelper.FrameFolderName(uploadStartDate, uploadEndDate, false);
                if (!Directory.Exists(SPDirPath + "\\" + groupsName))
                    Directory.CreateDirectory(SPDirPath + "\\" + groupsName);
                long folderCount = Directory.GetDirectories(SPDirPath + "\\" + groupsName, "*.*", SearchOption.TopDirectoryOnly).Length + 1;
                int i = 1;
                if (Directory.GetDirectories(SPDirPath, "*.*", SearchOption.TopDirectoryOnly).Length > 1)
                {
                    foreach (var dir in Directory.GetDirectories(SPDirPath))
                    {
                        DirectoryInfo dirinfo = new DirectoryInfo(dir);
                        if (dirinfo.Name != groupsName)
                        {

                            long fileCount = Convert.ToInt64(Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Length);

                            if (fileCount < itemCount)
                            {

                                if (!Directory.Exists(SPDirPath + @"\" + groupsName + @"\" + folderCount))
                                    Directory.CreateDirectory(SPDirPath + @"\" + groupsName + @"\" + folderCount);

                                if (Directory.Exists(SPDirPath + "\\" + groupsName))
                                    if (Directory.Exists(SPDirPath + @"\" + groupsName + @"\" + folderCount))
                                    {
                                        long fileCount1 = Convert.ToInt64(Directory.GetFiles(SPDirPath + @"\" + groupsName + @"\" + folderCount, "*.*", SearchOption.AllDirectories).Length);
                                        if ((fileCount1 + fileCount) > itemCount)
                                        {
                                            folderCount++;
                                            if (!Directory.Exists(SPDirPath + @"\" + groupsName + @"\" + folderCount))
                                                Directory.CreateDirectory(SPDirPath + @"\" + groupsName + @"\" + folderCount);
                                        }
                                    }

                                DirectoryCopyGroups(dir, SPDirPath + @"\" + groupsName + @"\" + folderCount.ToString());
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "YM_SharepointUpload", ex.Message);
                Environment.Exit(0);
            }
        }

        public static void DirectoryCopyGroups(string strSource, string Copy_dest)
        {
            DirectoryInfo dir = new DirectoryInfo(strSource);
            dir.MoveTo(Path.Combine(Copy_dest, dir.Name));
        }

        static bool VerifyGroupsFiles(string JobId, string containerName, string reportingQueueUri, string GroupName, string GroupType, int threadcount, int BatchNo, string FolderPath)
        {
            try
            {
                if (GroupType == "ReSubmitSingle")
                {
                    LogEvents("Information", "Verification", "ResubmitProcessStarted");
                    bool isUploaded = false;
                    string ReSubmitPath = ConfigurationManager.AppSettings["ReSubmitPath"];
                    string groupFolderName = GroupName.Replace("/", "\\");
                    List<string> FileNames = Directory.EnumerateFiles(Path.Combine(ReSubmitPath, groupFolderName), "*.*").Select(Path.GetFileName).ToList();
                    foreach (string FileName in FileNames)
                    {
                        if (SharePointClassLibrary.AzureSharePointHelper.CheckFileUploadedToSharePOint(Configuration.targetDocLib, GroupName, FileName))
                        {
                            bool isErrorExists = false;
                            try
                            {
                                SharePointClassLibrary.AzureSharePointHelper.GetLogFileFromContainer(JobId, containerName, out isErrorExists);
                            }
                            catch (Exception ex)
                            {
                                isErrorExists = true;
                                if (!ex.ToString().Contains("The remote server returned an error: (404) Not Found"))
                                    LogEvents("Error", "Verification", ex.ToString());
                            }
                            if (!isErrorExists)
                            {
                                isUploaded = true;
                                string destFileName = Path.Combine(ReSubmitPath, groupFolderName, FileName);
                                if (File.Exists(destFileName))
                                    File.Delete(destFileName);
                            }
                            else
                            {
                                isUploaded = false;
                                LogEvents("Error", "Verification", "Error log available for container : " + ConfigurationManager.AppSettings["LogPath"] + "\\" + containerName + "\\" + JobId + ".err");
                            }
                        }
                        SharePointClassLibrary.AzureSharePointHelper.DeleteBlobContainer(containerName);
                        SharePointClassLibrary.AzureSharePointHelper.DeleteQueue(reportingQueueUri);
                    }
                    if (isUploaded)
                    {
                        updateStatus(JobId, "Completed");
                        UpdateBatchStatus(BatchNo, "Completed");
                        UpdateSPMapping(BatchNo);
                        if (Directory.EnumerateFiles(Path.Combine(ReSubmitPath, groupFolderName)).ToList().Count() == 0)
                        {
                            Directory.Delete(Path.Combine(ReSubmitPath, groupFolderName));
                        }
                    }
                    else
                    {
                        updateStatus(JobId, "Failed");
                        UpdateBatchStatus(BatchNo, "Failed", FileNames.Count());
                        UpdateSPMapping(BatchNo);
                    }
                    return false;
                }
                else
                {
                    LogEvents("Information", "Verification", "ProcessStarted");
                    List<string> fileSPList = new List<string>();
                    int fileCount = SharePointClassLibrary.AzureSharePointHelper.GetThreadCount(Configuration.targetDocLib, GroupName, FolderPath, Convert.ToInt32(Year), out fileSPList);
                    if (fileCount != threadcount)
                    {
                        DataTable folderList = new DataTable("Table_ID");
                        folderList.Columns.Add("FolderPath", System.Type.GetType("System.String"));
                        folderList.Columns.Add("ItemCount", System.Type.GetType("System.Int32"));
                        folderList.Columns.Add("GroupName", System.Type.GetType("System.String"));
                        folderList.Columns.Add("GroupType", System.Type.GetType("System.String"));
                        folderList.Columns.Add("FileName", System.Type.GetType("System.String"));

                        List<string> dirFiles = Directory.EnumerateFiles(Path.Combine(FolderPath, GroupName.Replace("/", "\\")), "*.*", SearchOption.AllDirectories).Select(Path.GetFullPath).ToList();
                        List<string> diffs = dirFiles.Except(fileSPList).Distinct().ToList();
                        if (diffs.Count() > 0)
                        {
                            string ReSubmitPath = ConfigurationManager.AppSettings["ReSubmitPath"];
                            foreach (string file in diffs)
                            {
                                string groupFolderName = file.Replace(FolderPath, "");
                                string fileName = groupFolderName.Substring(groupFolderName.LastIndexOf("\\") + 1);
                                groupFolderName = groupFolderName.Substring(1, groupFolderName.LastIndexOf("\\") - 1);

                                if (!Directory.Exists(Path.Combine(ReSubmitPath, groupFolderName)))
                                    Directory.CreateDirectory(Path.Combine(ReSubmitPath, groupFolderName));
                                string destFileName = Path.Combine(ReSubmitPath, groupFolderName, fileName);
                                File.Copy(file, destFileName, true);
                                folderList.Rows.Add(Path.Combine(ReSubmitPath, groupFolderName), 1, groupFolderName.Replace("\\", "/"), "ReSubmitSingle", fileName);
                            }

                          


                            using (SqlConnection con = new SqlConnection(conn))
                            {
                                con.Open();
                                SqlCommand sqlcmd = new SqlCommand();
                                sqlcmd.Connection = con;
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.CommandText = "Yammer_LoadSPBatchLists";
                                sqlcmd.Parameters.AddWithValue("YEAR", Convert.ToInt32(Year));
                                sqlcmd.Parameters.AddWithValue("folderPathCount", folderList);
                                sqlcmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                                sqlcmd.Parameters.AddWithValue("parentBatchNo", BatchNo);
                                sqlcmd.Parameters.AddWithValue("SPMappingId", Configuration.SPMappingId);
                                sqlcmd.ExecuteNonQuery();
                                con.Close();
                            }
                            //LogEvents("Error", "YM_SharepointUpload", " All files uploaded with some error. Refer error log path:" + ConfigurationManager.AppSettings["LogPath"] + "\\" + containerName);
                            updateStatus(JobId, "PartiallyCompleted");
                            UpdateBatchStatus(BatchNo, "PartiallyCompleted");
                        }
                        else
                        {
                            updateStatus(JobId, "Completed");
                            UpdateBatchStatus(BatchNo, "Completed");
                            UpdateSPMapping(BatchNo);
                        }
                    }
                    else
                    {
                        updateStatus(JobId, "Completed");
                        UpdateBatchStatus(BatchNo, "Completed");
                        UpdateSPMapping(BatchNo);
                    }
                    SharePointClassLibrary.AzureSharePointHelper.DeleteBlobContainer(containerName);
                    SharePointClassLibrary.AzureSharePointHelper.DeleteQueue(reportingQueueUri);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "The remote server returned an error: (404) Not Found.")
                {
                    try
                    {
                        SharePointClassLibrary.AzureSharePointHelper.DeleteQueue(reportingQueueUri);
                        return true;
                    }
                    catch (Exception ex1)
                    {
                        return true;
                    }
                }
                else
                    LogEvents("Error", "ContainerDeletion", ex.ToString());
                return false;
            }
        }

        static void updateStatus(string JobId, string status)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_UpdateSharepointStatus(JobId, status);
            }
            
        }

        public static void LogEvents(string evenType, string fileName, string errorDescription)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Common_LogEvent("YM_Sharepoint", evenType.ToString(), fileName,errorDescription,Environment.MachineName);
            }

            
        }
    }

    class Configuration
    {
        public static string Url = ConfigurationManager.AppSettings["SP_Url"];
        public static string targetDocLib = ConfigurationManager.AppSettings["SP_targetLibrary"];
        public static string azureAccountName = ConfigurationManager.AppSettings["SP_AzAccountName"];
        public static string azureAccountKey = ConfigurationManager.AppSettings["SP_AzAccountKey"];
        public static string capacity = ConfigurationManager.AppSettings["SP_Capacity"];
        public static string sourcePackage = string.Empty;
        public static string targetPackage = string.Empty;
        public static int totalNewBatchCount = Convert.ToInt32(ConfigurationManager.AppSettings["newbatchcount"]);
        public static int totalResubmitBatchCount = Convert.ToInt32(ConfigurationManager.AppSettings["resubmitbatchcount"]);
        public static int SPMappingId = 0;
    }
}
