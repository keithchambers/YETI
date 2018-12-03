// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using System.IO.Compression;
using System.Xml;
using System.Globalization;
using System.Xml.Xsl;
using System.Numerics;
using YammerLibrary;
using System.Web.Security.AntiXss;
using System.Threading.Tasks;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace ConsoleYammerApplication
{
    class Program
    {
        private static string strtdStart = "<td>";
        private static string strtdEnd = "</td>";

        private static string rangeInMonths = string.Empty;
        private static string rangeInDays = string.Empty;
        private static string dirPath = string.Empty;
        private static string dirPath2 = string.Empty;
        private static string LogPath = string.Empty;
        private static string BackupDirPath = string.Empty;
        private static string YammerdirPath = string.Empty;
        private static string YammerCmpPath = string.Empty;
        private static string LargerfilesPath = string.Empty;
        private static string filedirPath = string.Empty;
        private static string pagedirPath = string.Empty;
        private static string RobocopyMoveCommandTemplate = string.Empty;
        private static string RobocopyFileCommandTemplate = string.Empty;
        private static string SP_Url = string.Empty;
        private static string SP_targetLibrary = string.Empty;
        private static bool RangeInDays = false;
        private static bool toGenerate = true;
        private static string Year = string.Empty;
        private static bool isArchived = false;
        private static bool isUsersLoaded = false;
        private static string conn = string.Empty;
        private static string restToken = string.Empty;
        private static string SPDirPath = string.Empty;


        static void Main(string[] args)
        {

            MainAsync().Wait();
        }

        private static async Task FetchConfigValuesFromKeyvaultAsync()
        {
            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["DBNameURL"];
            conn = await getSecretCmdlet.GetSecretAsync();

            //Console.WriteLine(conn);

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerRestTokenURL"];
            restToken = await getSecretCmdlet.GetSecretAsync();


            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["SPdirPath"];
            SPDirPath = await getSecretCmdlet.GetSecretAsync();



            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["rangeInMonths"];
            rangeInMonths = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["rangeInDays"];
            rangeInDays = await getSecretCmdlet.GetSecretAsync();
           RangeInDays=  (Convert.ToInt32(rangeInDays) == 0) ? true : false;

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["dirPath"];
            dirPath2 = await getSecretCmdlet.GetSecretAsync();
            dirPath = dirPath2 + "\\ExportCSV\\";

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["LogPath"];
            LogPath = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["BackupDirPath"];
            BackupDirPath = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerdirPath"];
            YammerdirPath = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerCmpPath"];
            YammerCmpPath = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["LargerfilesPath"];
            LargerfilesPath = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["filesPath"];
            filedirPath = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["pagesPath"];
            pagedirPath = await getSecretCmdlet.GetSecretAsync();

        getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["RobocopyMoveCommandTemplate"];
            RobocopyMoveCommandTemplate = await getSecretCmdlet.GetSecretAsync();

        getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["RobocopyFileCommandTemplate"];
            RobocopyFileCommandTemplate = await getSecretCmdlet.GetSecretAsync();

       
    }

    static async Task MainAsync()
        {
            try
            {
                //PafHelper obj = new PafHelper(); // to get all the config from paf   
                Console.WriteLine("Start");


                // Fetch Configuration parameters from Keyvault
                await FetchConfigValuesFromKeyvaultAsync();



                LogEvents("Information", "ProcessLog", "Processing started ");
                LogEvents("Information", "ProcessLog", "Movement of file started ");
                MoveFileForProcessing();
                LogEvents("Information", "ProcessLog", "Movement of file completed ");
                LogEvents("Information", "ProcessLog", "Getting year ");
                DataSet dsYear = new DataSet();

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    Yammer_YearProcessingRequest_Result res = yeticontext.Yammer_YearProcessingRequest(Environment.MachineName, RangeInDays).FirstOrDefault();

                    Year = res.YMYEAR.ToString();
                    toGenerate = Convert.ToBoolean(res.ToGenerate);
                }

                LogEvents("Information", "ProcessLog", "Got year - " + Year);
                if (!string.IsNullOrEmpty(Year) && Year != "1900")
                {
                    dirPath = dirPath + Year;
                    LogEvents("Information", "ProcessLog", "Got dirpath - " + dirPath);
                    LogEvents("Information", "ProcessLog", "Started");
                    LoadDataFromCSV();
                    LogEvents("Information", "ProcessLog", "LoadDataFromCSV");

                    LogEvents("Information", "ProcessLog", "ArchiveAsyncStarted");

                    Thread FirstThread = new Thread(new ThreadStart(GetCopyFromArchiveRepo));
                    FirstThread.IsBackground = true;
                    FirstThread.Start();

                    LogEvents("Information", "ProcessLog", "UserListLoad started");
                    LoadUsersFromBody();
                    LogEvents("Information", "ProcessLog", "UserListLoad Completed");
                    LogEvents("Information", "ProcessLog", "UserAsyncStarted");

                   
                    await LoadUsersListFromBodyAsync();

                    Thread ThirdThread = new Thread(new ThreadStart(OtherProcesses));
                    ThirdThread.IsBackground = true;
                    ThirdThread.Start();
                   
                    while (FirstThread.IsAlive || ThirdThread.IsAlive)
                    {
                        Thread.Sleep(300);
                    }
                    LogEvents("Information", "ProcessLog", "DownloadFileStarted");
                    DownloadAttachments("uploadedfile");
                    LogEvents("Information", "ProcessLog", "uplfile");
                    LogEvents("Information", "ProcessLog", "DownloadPageStarted");
                    DownloadAttachments("page");
                    LogEvents("Information", "ProcessLog", "uplpage");
                    LogEvents("Information", "ProcessLog", "Waiting for Threads to Complete");
                   
                    LogEvents("Information", "ProcessLog", "Generate HTML started");
                    await GenerateHTMLByThreads();
                    Thread.Sleep(300);
                    LogEvents("Information", "ProcessLog", "GenerateHTMLByThreads");
                    VerifyGeneratedFiles();
                    LogEvents("Information", "ProcessLog", "VerifyGeneratedFiles");
                    ArchiveThreadInDB();
                    LogEvents("Information", "ProcessLog", "Archived");
                    Thread.Sleep(300);
                    if (!Directory.Exists(dirPath + "\\XMLNodes"))
                    {
                        MoveArchivalToRepo();

                        PurgeCSVContentsFromDB();
                        LogEvents("Information", "ProcessLog", "DataPurged");
                        
                        LogEvents("Information", "ProcessLog", "Completed");
                    }
                    else
                    {
                        ResetArchievedThreadStatus();
                    }
                    Year = string.Empty;

                }
                LogEvents("Information", "ProcessLog", "Processing completed " + Year);
            }
            catch (Exception ex)
            {
                
                LogEvents("Error", "Main", ex.ToString());
            }
        }


        private static void OtherProcesses()
        {
            MapNewVersionFilesToExistingThreads("uploadedfile");
            LogEvents("Information", "ProcessLog", "MapFiles");

            MapNewVersionFilesToExistingThreads("page");
            LogEvents("Information", "ProcessLog", "MapPage");

            LoadFilesVersion("uploadedfile");
            LogEvents("Information", "ProcessLog", "Loadfile");

            LoadFilesVersion("page");
            LogEvents("Information", "ProcessLog", "Loadpage");

            RenameDownloadedFiles("uploadedfile");
            LogEvents("Information", "ProcessLog", "RenameFile");

            RenameDownloadedFiles("page");
            LogEvents("Information", "ProcessLog", "Renamepage");
        }

        private static void MoveArchivalToRepo()
        {
            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_MoveArchivalToRepo(Environment.MachineName, Convert.ToInt32(Year));
            }

        }


        private static async Task LoadUsersListFromBodyAsync()
        {
            await UpdateUserDetailsFromYammerAsync();
            isUsersLoaded = true;
            LogEvents("Information", "ProcessLog", "userload async completed");

        }

        private static void GetCopyFromArchiveRepo()
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Database.CommandTimeout = 0;
                yeticontext.Yammer_GetCopyFromArchiveRepo();
            }

            isArchived = true;
            LogEvents("Information", "ProcessLog", "ArchiveRepo completed");
        }

        private static void ResetArchievedThreadStatus()
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {

                yeticontext.Yammer_ResetArchievedThreadStatus();
            }

        }

     
        private static void MoveFileForProcessing()
        {
            string startDate = string.Empty;
            string endDate = string.Empty;
            try
            {
                if (Directory.Exists(dirPath2))
                {
                    LogEvents("Information", "ProcessLog", "Directory exists");
                    int rangeInMon = Convert.ToInt32(rangeInMonths);
                    int rangeInD = Convert.ToInt32(rangeInDays);
                    List<Yammer_CheckRangeForFileMove_Result> res = new List<Yammer_CheckRangeForFileMove_Result>();
                    using (YETIDBEntities yeticontext = new YETIDBEntities())
                    {

                        res =
                            yeticontext.Yammer_CheckRangeForFileMove(Environment.MachineName,
                            rangeInMon,
                            rangeInD
                            ).ToList();



                    }



                    if (rangeInMonths != "0")
                    {
                        int[] csvFolders = new int[Convert.ToInt32(rangeInMonths)];
                        foreach (Yammer_CheckRangeForFileMove_Result dr in res)
                        {
                            startDate = dr.StartDate.ToString();
                            endDate = dr.EndDate.ToString();
                            csvFolders[0] = Convert.ToInt32(dr.FirstFolder);
                            Year = dr.ProcessYear.ToString();
                        }
                        for (int i = 1; i < csvFolders.Count(); i++)
                        {
                            csvFolders[i] = csvFolders[i - 1] + 1;
                        }
                        if (!Directory.Exists(LogPath + "\\RobocopyLogs"))
                            Directory.CreateDirectory(LogPath+ "\\RobocopyLogs");
                        for (int i = 0; i < csvFolders.Count(); i++)
                        {
                            if (Directory.Exists(dirPath2 + "\\" + Year + "\\" + csvFolders[i]))
                            {
                                bool isMoved = false;
                                string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Processing", Year + "_" + csvFolders[i], DateTime.Now.Ticks);
                                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                                {
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.CreateNoWindow = true;
                                    process.StartInfo.RedirectStandardOutput = false;
                                    process.StartInfo.FileName = "ROBOCOPY";
                                    process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyMoveCommandTemplate, dirPath2 + "\\" + Year + "\\" + csvFolders[i], dirPath2 + "\\ExportCSV\\" + Year, logName);
                                    process.Start();
                                    process.WaitForExit(2400 * 60 * 1000);
                                    if (process.HasExited)
                                    {
                                        if (process.ExitCode <= 8)
                                        {
                                            isMoved = true;
                                            File.Delete(logName);
                                        }
                                        else
                                        {
                                            LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                            Environment.Exit(0);
                                        }
                                    }
                                }
                                if (isMoved)
                                {
                                    logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Backup", Year + "_" + csvFolders[i], DateTime.Now.Ticks);
                                    using (System.Diagnostics.Process moveProcess = new System.Diagnostics.Process())
                                    {
                                        moveProcess.StartInfo.UseShellExecute = false;
                                        moveProcess.StartInfo.CreateNoWindow = true;
                                        moveProcess.StartInfo.RedirectStandardOutput = false;
                                        moveProcess.StartInfo.FileName = "ROBOCOPY";
                                        moveProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyMoveCommandTemplate, dirPath2 + "\\" + Year + "\\" + csvFolders[i], BackupDirPath + "\\" + Year, logName);
                                        moveProcess.Start();
                                        moveProcess.WaitForExit(2400 * 60 * 1000);
                                        if (moveProcess.HasExited)
                                        {
                                            if (moveProcess.ExitCode <= 8)
                                            {
                                                IEnumerable<string> fileList = Directory.EnumerateFiles(dirPath2 + "\\" + Year + "\\" + csvFolders[i]);
                                                foreach (string filename in fileList)
                                                {
                                                    File.Delete(filename);
                                                }
                                                Directory.Delete(dirPath2 + "\\" + Year + "\\" + csvFolders[i]);
                                                File.Delete(logName);
                                            }
                                            else
                                            {
                                                LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                                Environment.Exit(0);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        UpdateStatus(startDate, endDate, "Moved", "MovedForProcessing", 0, true);
                    }
                    else
                    {
                        if (!Directory.Exists(LogPath + "\\RobocopyLogs"))
                            Directory.CreateDirectory(LogPath + "\\RobocopyLogs");
                        foreach (Yammer_CheckRangeForFileMove_Result dr in res)
                        {
                            int csvFolder = 0;
                            string fileName = string.Empty;
                            startDate = dr.StartDate.ToString();
                            endDate = dr.EndDate.ToString();
                            //csvFolder = Convert.ToInt32(dr["Folder"]);
                            csvFolder = Convert.ToInt32(dr.LastFolder);
                            Year = dr.ProcessYear.ToString();
                            //fileName = dr["FileName"].ToString();
                            fileName = dr.FirstFolder.ToString();
                            bool isMoved = false;
                            string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Processing", Year + "_" + fileName.Split('.')[0].Replace("-", "_"), DateTime.Now.Ticks);
                            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                            {
                                process.StartInfo.UseShellExecute = false;
                                process.StartInfo.CreateNoWindow = true;
                                process.StartInfo.RedirectStandardOutput = false;
                                process.StartInfo.FileName = "ROBOCOPY";
                                process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyFileCommandTemplate, dirPath2 + "\\" + Year + "\\" + csvFolder, dirPath2 + "\\ExportCSV\\" + Year, fileName, logName);
                                process.Start();
                                process.WaitForExit(2400 * 60 * 1000);
                                if (process.HasExited)
                                {
                                    if (process.ExitCode <= 8)
                                    {
                                        isMoved = true;
                                        File.Delete(logName);
                                    }
                                    else
                                    {
                                        LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                        Environment.Exit(0);
                                    }
                                }
                            }
                            if (isMoved)
                            {
                                logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Backup", Year + "_" + fileName.Split('.')[0], DateTime.Now.Ticks);
                                using (System.Diagnostics.Process moveProcess = new System.Diagnostics.Process())
                                {
                                    moveProcess.StartInfo.UseShellExecute = false;
                                    moveProcess.StartInfo.CreateNoWindow = true;
                                    moveProcess.StartInfo.RedirectStandardOutput = false;
                                    moveProcess.StartInfo.FileName = "ROBOCOPY";
                                    moveProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyFileCommandTemplate, dirPath2 + "\\" + Year + "\\" + csvFolder, BackupDirPath + "\\" + Year, fileName, logName);
                                    moveProcess.Start();
                                    moveProcess.WaitForExit(2400 * 60 * 1000);
                                    if (moveProcess.HasExited)
                                    {
                                        if (moveProcess.ExitCode <= 8)
                                        {
                                            File.Delete(dirPath2 + "\\" + Year + "\\" + csvFolder + "\\" + fileName);
                                            if (Directory.EnumerateFiles(dirPath2 + "\\" + Year + "\\" + csvFolder).Count() == 0)
                                                Directory.Delete(dirPath2 + "\\" + Year + "\\" + csvFolder);
                                            File.Delete(logName);
                                        }
                                        else
                                        {
                                            LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                            Environment.Exit(0);
                                        }
                                    }
                                }
                            }
                            UpdateStatus(startDate, endDate, "Moved", "MovedForProcessing", 0, true);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());
            }
        }
        private static void UpdateStatus(string startDateTime, string endDateTime, string status, string stage, int timesTried, bool toMove = false)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {

                yeticontext.Yammer_Common_UpdateStatus(startDateTime, endDateTime, status, stage, timesTried, toMove);

            }

        }
        private static void UpdateYearStatus(string Year)
        {
            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {

                yeticontext.Yammer_Update_YearStatus(Year, 0, 1, 0, 0);
            }

        }
        private static void RenameDownloadedFiles(string filter)
        {
            string extension = string.Empty;
            string newFileName = string.Empty;
            string combineNewFileName = string.Empty;
            try
            {
                List<Yammer_GetAttchmntDtlsForRename_Result> res = new List<Yammer_GetAttchmntDtlsForRename_Result>();
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    res = yeticontext.Yammer_GetAttchmntDtlsForRename(filter, Environment.MachineName).ToList();
                }


                foreach (Yammer_GetAttchmntDtlsForRename_Result dr in res)
                {
                    newFileName = string.Empty;
                    string[] split = dr.name.ToString().Split('.');
                    string firstPart = string.Join(".", split.Take(split.Length - 1));
                    string lastPart = split.Last();
                    extension = (filter == "uploadedfile") ? (dr.name.ToString().Contains('.')) ? "." + RemoveUnsupportedFileNameChars(lastPart) : "" : ".html";
                    string pathToSearch = Path.Combine(((filter == "uploadedfile") ? filedirPath : pagedirPath), dr.id + extension);

                    if (dr.VerCount.ToString() != "1")
                        newFileName = dr.file_id + "-" + dr.id + extension;
                    else
                    {
                        string unsupportedCharRemoved = RemoveUnsupportedFileNameChars((filter == "uploadedfile") ? firstPart : dr.name.ToString());
                        newFileName = CheckFilePathLength(((filter == "uploadedfile") ? filedirPath : pagedirPath), dr.file_id.ToString(), unsupportedCharRemoved) + extension;
                        
                    }
                    if (File.Exists(pathToSearch))
                    {
                       
                        combineNewFileName = Path.Combine(((filter == "uploadedfile") ? filedirPath : pagedirPath), newFileName);
                        if (File.Exists(combineNewFileName))
                            File.Delete(combineNewFileName);
                        File.Move(pathToSearch, combineNewFileName);

                    }
                }
                if (filter == "uploadedfile")
                    updateStatus("PageVersionsLoaded", "FilesRenamed");
                else
                    updateStatus("FilesRenamed", "PagesRenamed");
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(combineNewFileName))
                    combineNewFileName = newFileName;
                LogEvents("Error", "RenameFiles_" + combineNewFileName, ex.ToString());
                Environment.Exit(0);
            }
        }
     
        private static void LoadFilesVersion(string filter)
        {
            try
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_LoadFilesVersionList(filter, Environment.MachineName);
                }


                if (filter == "uploadedfile")
                    updateStatus("PageMapped", "FileVersionsLoaded");
                else
                    updateStatus("FileVersionsLoaded", "PageVersionsLoaded");
            }
            catch (Exception ex)
            {
                LogEvents("Error", "LoadFilesVersion" + filter, ex.ToString());
                Environment.Exit(0);
            }

        }

        private static void PurgeCSVContentsFromDB()
        {
            try
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_PurgeProcessedData(Environment.MachineName);
                }

            }
            catch (Exception ex)
            {
                LogEvents("Error", "PurgeProcessedData", ex.ToString());
                Environment.Exit(0);
            }
        }
      
        private static void MapNewVersionFilesToExistingThreads(string filter)
        {
            try
            {
                List<Yammer_GetListToMapNewVersionFiles_Result> res = new List<Yammer_GetListToMapNewVersionFiles_Result>();
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    res = yeticontext.Yammer_GetListToMapNewVersionFiles(Environment.MachineName, filter).ToList();
                }


                string filesPath = (filter == "uploadedfile") ? filedirPath : pagedirPath;

                foreach (Yammer_GetListToMapNewVersionFiles_Result dr in res)
                {
                    string pathtoSearch = Path.Combine(YammerdirPath, Year);

                    string groupFolderName = string.Empty;

                    if (Convert.ToString(dr.GroupId) == string.Empty)
                        groupFolderName = "Private_Conversations";
                    else
                        groupFolderName = string.Concat(dr.GroupId, " ", RemoveUnsupportedFileNameChars(dr.GroupName.ToString().Replace("/", " ")));


                    if (groupFolderName.Length > 65) // SharePoint folder lenght limit
                        groupFolderName = groupFolderName.Substring(0, 65);

                    pathtoSearch = Path.Combine(pathtoSearch, groupFolderName, Convert.ToString(dr.ThreadId), "Attachments");

                    string FileName = string.Empty;
                    string FileExtension = string.Empty;
                    if (filter == "uploadedfile")
                    {
                        if (dr.filename.ToString().Contains('.'))
                        {
                            string[] split = dr.filename.ToString().Split('.');
                            string firstPart = string.Join(".", split.Take(split.Length - 1));
                            string lastPart = split.Last();
                            FileName = RemoveUnsupportedFileNameChars(firstPart);
                            FileExtension = "." + lastPart;
                        }
                        else
                        {
                            FileName = RemoveUnsupportedFileNameChars(dr.filename.ToString());
                            FileExtension = "";
                        }
                    }
                    else
                    {
                        FileName = RemoveUnsupportedFileNameChars(dr.filename.ToString());
                        FileExtension = ".html";
                    }

                    if (Convert.ToInt32(dr.VerCount) == 1)
                    {
                        string sourceFileName = dr.versionid.ToString() + FileExtension;
                        string destFileName = dr.file_id.ToString() + "-" + FileName + FileExtension;
                        //begin added 20170314
                        destFileName = CheckFilePathLength(pathtoSearch, dr.file_id.ToString(), FileName) + FileExtension;
                        //end added 20170314
                        destFileName = destFileName.Replace(pathtoSearch + "\\", "");

                        if (Directory.Exists(pathtoSearch))
                        {
                            if (File.Exists(Path.Combine(filesPath, sourceFileName)))
                            {
                                if (!Directory.Exists(LogPath + "\\RobocopyLogs"))
                                    Directory.CreateDirectory(LogPath + "\\RobocopyLogs");
                                string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Processing", Year + "_" + dr.versionid.ToString(), DateTime.Now.Ticks);
                                Directory.EnumerateFiles(pathtoSearch, destFileName).ToList().ForEach(File.Delete);
                                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                                {
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.CreateNoWindow = true;
                                    process.StartInfo.RedirectStandardOutput = false;
                                    process.StartInfo.FileName = "ROBOCOPY";
                                    process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture,
                                        RobocopyFileCommandTemplate, filesPath, pathtoSearch, sourceFileName, logName);
                                    process.Start();
                                    process.WaitForExit(2400 * 60 * 1000);
                                    if (process.HasExited)
                                    {
                                        if (process.ExitCode > 8)
                                        {
                                            LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                            Environment.Exit(0);
                                        }
                                        else
                                            File.Delete(logName);
                                        if (!File.Exists(Path.Combine(pathtoSearch, destFileName)) && File.Exists(Path.Combine(pathtoSearch, sourceFileName)))
                                            File.Move(Path.Combine(pathtoSearch, sourceFileName), Path.Combine(pathtoSearch, destFileName));

                                        if (!File.Exists(Path.Combine(filesPath, destFileName)) && File.Exists(Path.Combine(filesPath, sourceFileName)))
                                            File.Move(Path.Combine(filesPath, sourceFileName), Path.Combine(filesPath, destFileName));
                                    }
                                }
                            }
                            else
                            {
                                sourceFileName = destFileName;
                                if (File.Exists(Path.Combine(filesPath, sourceFileName)))
                                {
                                    Directory.EnumerateFiles(pathtoSearch, dr.file_id.ToString() + "-*").ToList().ForEach(File.Delete);
                                    if (!Directory.Exists(LogPath + "\\RobocopyLogs"))
                                        Directory.CreateDirectory(LogPath + "\\RobocopyLogs");
                                    string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Processing", Year + "_" + dr.versionid.ToString(), DateTime.Now.Ticks);
                                    using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                                    {
                                        process.StartInfo.UseShellExecute = false;
                                        process.StartInfo.CreateNoWindow = true;
                                        process.StartInfo.RedirectStandardOutput = false;
                                        process.StartInfo.FileName = "ROBOCOPY";
                                        process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyFileCommandTemplate, filesPath, pathtoSearch, sourceFileName, logName);
                                        process.Start();
                                        process.WaitForExit(2400 * 60 * 1000);
                                        if (process.HasExited)
                                        {
                                            if (process.ExitCode > 8)
                                            {
                                                LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                                Environment.Exit(0);
                                            }
                                            else
                                                File.Delete(logName);
                                            if (!File.Exists(Path.Combine(pathtoSearch, destFileName)) && File.Exists(Path.Combine(pathtoSearch, sourceFileName)))
                                                File.Move(Path.Combine(pathtoSearch, sourceFileName), Path.Combine(pathtoSearch, destFileName));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string sourceFileName = dr.versionid.ToString() + FileExtension;
                        string destFileName = dr.file_id.ToString() + "-" + dr.versionid.ToString() + FileExtension;
                        pathtoSearch = Path.Combine(pathtoSearch, Convert.ToString(dr.file_id));

                       
                        destFileName = CheckFilePathLength(pathtoSearch, dr.file_id.ToString(), dr.versionid.ToString()) + FileExtension;
                       
                        destFileName = destFileName.Replace(pathtoSearch + "\\", "");

                        if (!Directory.Exists(pathtoSearch))
                            Directory.CreateDirectory(pathtoSearch);

                        if (File.Exists(Path.Combine(filesPath, sourceFileName)))
                        {
                            if (!Directory.Exists(LogPath + "\\RobocopyLogs"))
                                Directory.CreateDirectory(LogPath + "\\RobocopyLogs");
                            string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Processing", Year + "_" + dr.versionid.ToString(), DateTime.Now.Ticks);
                            Directory.EnumerateFiles(pathtoSearch, dr.file_id.ToString() + "-*").ToList().ForEach(File.Delete);
                            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                            {
                                process.StartInfo.UseShellExecute = false;
                                process.StartInfo.CreateNoWindow = true;
                                process.StartInfo.RedirectStandardOutput = false;
                                process.StartInfo.FileName = "ROBOCOPY";
                                process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyFileCommandTemplate, filesPath, pathtoSearch, sourceFileName, logName);
                                process.Start();
                                process.WaitForExit(2400 * 60 * 1000);
                                if (process.HasExited)
                                {
                                    if (process.ExitCode > 8)
                                    {
                                        LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                        Environment.Exit(0);
                                    }
                                    else
                                        File.Delete(logName);
                                    if (!File.Exists(Path.Combine(pathtoSearch, destFileName)) && File.Exists(Path.Combine(pathtoSearch, sourceFileName)))
                                        File.Move(Path.Combine(pathtoSearch, sourceFileName), Path.Combine(pathtoSearch, destFileName));

                                    if (!File.Exists(Path.Combine(filesPath, destFileName)) && File.Exists(Path.Combine(filesPath, sourceFileName)))
                                        File.Move(Path.Combine(filesPath, sourceFileName), Path.Combine(filesPath, destFileName));
                                }
                            }
                        }
                        else
                        {
                            sourceFileName = dr.file_id.ToString() + "-" + dr.versionid.ToString() + FileExtension;
                            if (File.Exists(Path.Combine(filesPath, sourceFileName)))
                            {
                                if (!Directory.Exists(LogPath + "\\RobocopyLogs"))
                                    Directory.CreateDirectory(LogPath + "\\RobocopyLogs");
                                string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", LogPath + "\\RobocopyLogs", "Processing", Year + "_" + dr.versionid.ToString(), DateTime.Now.Ticks);
                                Directory.EnumerateFiles(pathtoSearch, dr.file_id.ToString() + "-*").ToList().ForEach(File.Delete);
                                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                                {
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.CreateNoWindow = true;
                                    process.StartInfo.RedirectStandardOutput = false;
                                    process.StartInfo.FileName = "ROBOCOPY";
                                    process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyFileCommandTemplate, filesPath, pathtoSearch, sourceFileName, logName);
                                    process.Start();
                                    process.WaitForExit(2400 * 60 * 1000);
                                    if (process.HasExited)
                                    {
                                        if (process.ExitCode > 8)
                                        {
                                            LogEvents("Error", "RoboCopyLog", "check this path: " + logName);
                                            Environment.Exit(0);
                                        }
                                        else
                                            File.Delete(logName);
                                        if (!File.Exists(Path.Combine(pathtoSearch, destFileName)) && File.Exists(Path.Combine(pathtoSearch, sourceFileName)))
                                            File.Move(Path.Combine(pathtoSearch, sourceFileName), Path.Combine(pathtoSearch, destFileName));
                                    }
                                }
                            }
                        }
                    }
                };
                if (filter == "uploadedfile")
                    updateStatus("UsersLoaded", "FileMapped");
                else
                    updateStatus("FileMapped", "PageMapped");
            }
            catch (Exception ex)
            {
                LogEvents("Error", "MapNewVersion", ex.ToString());
                Environment.Exit(0);
            }
        }
       
        private static void ArchiveThreadInDB()
        {
            try
            {
                LogEvents("Information", "ArchivedThreads", "Archiving threads started");
                string updateQuery = string.Empty;
                string insertQuery = string.Empty;
                bool isFileAvailable = true;
                if (Directory.Exists(dirPath + "\\XMLNodes"))
                {
                    LogEvents("Information", "ArchivedThreads", "XMLNode dir exists");
                    DirectoryInfo ydi = new DirectoryInfo(dirPath + "\\XMLNodes");
                    while (isFileAvailable)
                    {
                        IEnumerable<FileInfo> fileList = ydi.EnumerateFiles("*.xml").Take(100);
                        LogEvents("Information", "ArchivedThreads", "Took " + fileList.Count() + " Files");
                        if (fileList.Count() == 0)
                            isFileAvailable = false;
                        LogEvents("Information", "ArchivedThreads", "Building Query started");
                        foreach (FileInfo fi in fileList)
                        {
                            string whole_file = System.IO.File.ReadAllText(fi.FullName);
                            whole_file = whole_file.Replace("'", "''");
                            updateQuery = updateQuery + "UPDATE YM_ArchivedThreads SET Modified_Date = GETDATE() WHERE Thread_id = " + fi.Name.Split('.')[0] + "; \n";

                            insertQuery = insertQuery + "INSERT INTO YM_ArchivedThreads (Thread_id, ThreadXMLContent)  SELECT * FROM (SELECT " + fi.Name.Split('.')[0] + " Thread_id,CONVERT(XML,N'" + whole_file + "') XMLData) A "
                                           + " WHERE 0 = (SELECT COUNT(THREAD_ID) FROM YM_ArchivedThreads WHERE Thread_id = " + fi.Name.Split('.')[0] + " ); \n";
                        }
                        LogEvents("Information", "ArchivedThreads", "Building Query completed");

                        if (!string.IsNullOrEmpty(insertQuery))
                        {
                            int updateReturnValue = 0;
                            int insertReturnValue = 0;

                            using (YETIDBEntities yeticontext = new YETIDBEntities())
                            {
                                yeticontext.Database.CommandTimeout = 0;
                                LogEvents("Information", "ArchivedThreads", "Execution of update query started");
                                yeticontext.Database.ExecuteSqlCommand(updateQuery);
                                LogEvents("Information", "ArchivedThreads", "Execution of update query completed");
                                LogEvents("Information", "ArchivedThreads", "Execution of Insert query started");
                                insertReturnValue = yeticontext.Database.ExecuteSqlCommand(insertQuery);
                                LogEvents("Information", "ArchivedThreads", "Execution of Insert query completed");
                            }

                           
                            if ((insertReturnValue + updateReturnValue) == fileList.Count())
                            {

                                if (updateReturnValue > 0)
                                {
                                    LogEvents("Information", "ArchivedThreads", "DeleteThreads Not happened for " + "_" + updateReturnValue.ToString());
                                    ResetArchievedThreadStatus();
                                    LogEvents("Information", "ArchivedThreads", updateReturnValue.ToString() + "_Threads deletion handled and updated properly");
                                }
                                LogEvents("Information", "ArchivedThreads", "Deletion of files started");
                                foreach (FileInfo fi in fileList)
                                {
                                    fi.Delete();
                                }
                                LogEvents("Information", "ArchivedThreads", "Deletion of files Completed");
                            }
                            else
                            {
                                LogEvents("Error", "ArchivedThreads", fileList.ToList()[0].Name + "_" + fileList.ToList()[99].Name + "_" + insertReturnValue.ToString());
                            }
                            insertQuery = string.Empty;
                            updateQuery = string.Empty;
                        }
                    }
                    ydi.Delete();
                    LogEvents("Information", "ArchivedThreads", "Directory deleted");
                }
                LogEvents("Information", "ArchivedThreads", "Archive threads method completed");
            }
            catch (Exception ex)
            {
                LogEvents("Error", "ArchivedThreads", ex.ToString());
                Environment.Exit(0);
            }
        }
        private static void VerifyGeneratedFiles()
        {
            LogEvents("Information", "ProcessLog", "Verification started ");
            string fileName = string.Empty;
            //begin added to fix string truncate issue 20170302
            DataTable dtThreadId = new DataTable("Table_ID");
            dtThreadId.Columns.Add("ID", System.Type.GetType("System.String"));
            //end added
            DataTable dtfailedThreadCSVNames = new DataTable("Table_ID");
            dtfailedThreadCSVNames.Columns.Add("ID", System.Type.GetType("System.String"));

            try
            {
                IEnumerable<Yammer_GetMsgDetailsForVer_Result> res;
                //List<Yammer_GetMsgDetailsForVer_Result> res = new List<Yammer_GetMsgDetailsForVer_Result>();

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    //res =  yeticontext.Yammer_GetMsgDetailsForVer(Environment.MachineName).ToList();
                    res = yeticontext.Yammer_GetMsgDetailsForVer(Environment.MachineName).AsEnumerable();
                }

                res.Where(msg => msg.Id == 1);

                
                int cnt = res.Count();
                var countdownEvent = new CountdownEvent(cnt);

                int parts = cnt / 1000;
                if ((cnt % 1000) > 0)
                    parts++;
                int skipCount = 0;
                while (parts > 0)
                {
                    countdownEvent = new CountdownEvent(res.Where(msg => msg.Id == 1).Skip(skipCount * 1000).Take(1000).Count());
                    //countdownEvent = new CountdownEvent(ds.Tables[0].Select("[Id] = 1").Skip(skipCount * 1000).Take(1000).Count());

                    foreach (Yammer_GetMsgDetailsForVer_Result drMessage in res.Where(msg => msg.Id == 1).Skip(skipCount * 1000).Take(1000))
                    {
                        string groupFolderName = string.Empty;


                        if (Convert.ToString(drMessage.group_id) == string.Empty)
                            groupFolderName = "Private_Conversations";
                        else
                            //modify to be the same to generate, RemoveUnsupportedFolderNameChars
                            groupFolderName = string.Concat(drMessage.group_id, " ",
                                RemoveUnsupportedFolderNameChars(drMessage.group_name.ToString().Replace("/", " ")));

                        if (groupFolderName.Length > 65) // SharePoint folder lenght limit
                            groupFolderName = groupFolderName.Substring(0, 65);

                        if (!YammerdirPath.EndsWith(Year))
                            YammerdirPath = Path.Combine(YammerdirPath, Year);

                        string threadFolderName = string.Concat(YammerdirPath, "\\", RemoveUnsupportedFolderNameChars(groupFolderName));
                        fileName = drMessage.csvfilename.ToString();
                        threadFolderName = string.Concat(threadFolderName, "\\", drMessage.thread_id.ToString());

                        //LogEvents("Information", "ProcessLog", "Processing ThreadFolderName=" + threadFolderName);
                        long directoryCount = Directory.GetDirectories(YammerdirPath, "*" + Convert.ToString(drMessage.group_id) + "*",
                            SearchOption.TopDirectoryOnly).Length;


                        if (Directory.Exists(threadFolderName))
                        {
                            if (directoryCount > 1)
                            {
                                //LogEvents("Information", "ProcessLog", "success Threadid as it is in groups with same group id thread id" + drMessage["thread_id"].ToString());
                                dtThreadId.Rows.Add(drMessage.thread_id.ToString());
                            }
                            else if (Directory.EnumerateFiles(threadFolderName, "*" + drMessage.thread_id.ToString() + ".html").Count() == 0)
                            {
                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {
                                    //res =  yeticontext.Yammer_GetMsgDetailsForVer(Environment.MachineName).ToList();
                                    yeticontext.Yammer_UpdateMsgDetailsForVer(drMessage.thread_id.ToString(), Environment.MachineName);
                                }


                                LogEvents("Information", "ProcessLog", "Failed Threadid as no html" + drMessage.thread_id.ToString());
                                dtfailedThreadCSVNames.Rows.Add(fileName);

                            }
                            else
                            {
                                //begin added for fixing string truncate issue 20170302
                                //LogEvents("Information", "ProcessLog", "success Threadid as there is html" + drMessage["thread_id"].ToString());
                                dtThreadId.Rows.Add(drMessage.thread_id.ToString());
                                //end added                                
                            }
                        }
                        else
                        {
                            if (directoryCount > 1)
                            {
                                //LogEvents("Information", "ProcessLog", "success Threadid as it is in groups with same group id thread id" + drMessage["thread_id"].ToString());
                                dtThreadId.Rows.Add(drMessage.thread_id.ToString());
                            }
                            else
                            {
                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {
                                    //res =  yeticontext.Yammer_GetMsgDetailsForVer(Environment.MachineName).ToList();
                                    yeticontext.Yammer_UpdateMsgDetailsForVer(drMessage.thread_id.ToString(), Environment.MachineName);
                                }


                                LogEvents("Information", "ProcessLog", "Failed Threadid as no directory threadFolderName=" + threadFolderName + " thread_id=" + drMessage.thread_id.ToString());
                                dtfailedThreadCSVNames.Rows.Add(fileName);
                            }
                        }
                        countdownEvent.Signal();
                    }
                    countdownEvent.Wait();
                    countdownEvent.Dispose();
                    Thread.Sleep(3000);
                    parts--;
                    skipCount++;
                    if (dtThreadId.Rows.Count > 0)
                    {
                        LogEvents("Information", "ProcessLog", "Updating Fileverified for success threads ");
                        updateThreadStatus("FilesGenerated", "FilesVerified", dtThreadId);
                        using (SqlConnection con = new SqlConnection(conn))
                        {
                            con.Open();
                            SqlCommand cmd = new SqlCommand();
                            cmd.Connection = con;
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "Yammer_UpdateExpDetailsForVer";
                            cmd.Parameters.AddWithValue("in_datatable_ThreadIds", dtThreadId);
                            cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }
                    dtThreadId.Clear();
                    //end modified
                }


                if (dtfailedThreadCSVNames.Rows.Count > 0)
                {
                    LogEvents("Information", "ProcessLog", "Updating pagedownload for failed threads ");
                    using (SqlConnection con = new SqlConnection(conn))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = con;
                        cmd.CommandTimeout = 0;
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "Yammer_UpdateExpDtlsForFailedThrd";
                        cmd.Parameters.AddWithValue("in_nvarchar_ThreadCSVNames", dtfailedThreadCSVNames);
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    LogEvents("Information", "ProcessLog", "Exit and wait for reprocess.");
                    Environment.Exit(0);
                }
                else
                {
                    string cmpBufFolder = string.Concat(YammerCmpPath, "\\", Year);
                    if (Directory.Exists(cmpBufFolder))
                    {
                        if (Directory.EnumerateDirectories(cmpBufFolder, "*.*", SearchOption.TopDirectoryOnly).Count() > 0)
                        {
                            UpdateYearStatus(Year);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", fileName, ex.ToString());
                Environment.Exit(0);
            }
        }
        private static void DownloadAttachments(string filter)
        {
            try
            {
                List<Yammer_DownloadAttachments_Result> res = new List<Yammer_DownloadAttachments_Result>();
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    res = yeticontext.Yammer_DownloadAttachments(filter, Environment.MachineName).ToList();
                }


                var countdownEvent = new CountdownEvent(res.Count);
                DataTable dtSuccessThreadId = new DataTable("Table_ID");
                dtSuccessThreadId.Columns.Add("ID", System.Type.GetType("System.String"));
                DataTable dtFailedThreadId = new DataTable("Table_ID");
                dtFailedThreadId.Columns.Add("ID", System.Type.GetType("System.String"));

                if (filter == "uploadedfile")
                {
                    StringBuilder failedFileThreadIds = new StringBuilder();
                    StringBuilder successFileThreadIds = new StringBuilder();

                    int parts = res.Count / 30;
                    if ((res.Count % 30) > 0)
                        parts++;
                    int skipCount = 0;
                    while (parts > 0)
                    {
                        //countdownEvent = new CountdownEvent(ds.Tables[0].Select().Skip(skipCount * 30).Take(30).Count());
                        countdownEvent = new CountdownEvent(res.Skip(skipCount * 30).Take(30).Count());
                        string prevFileId = string.Empty;
                        foreach (Yammer_DownloadAttachments_Result drFile in res.Skip(skipCount * 30).Take(30))
                        {
                            if (prevFileId != drFile.File_Id.ToString())
                            {
                                new Thread(delegate ()
                                {
                                    try
                                    {
                                        var file = Directory.GetFiles(filedirPath, drFile.File_Id.ToString());
                                        if (file.Length == 1)
                                            File.Delete(Path.Combine(filedirPath, drFile.File_Id.ToString()));
                                        string[] shareFiles = Directory.GetFiles(filedirPath, drFile.File_Id.ToString() + "-*.*");
                                        if (shareFiles.Length == 0)
                                        {
                                            List<Yammer_GetOldSharePathList_Result> resOldSP = new List<Yammer_GetOldSharePathList_Result>();
                                            using (YETIDBEntities yeticontext = new YETIDBEntities())
                                            {

                                                resOldSP = yeticontext.Yammer_GetOldSharePathList(false, "Files").ToList();
                                            }


                                            bool isFileAvailable = false;
                                            foreach (Yammer_GetOldSharePathList_Result dr in resOldSP)
                                            {
                                                string[] othershareFiles = Directory.GetFiles(dr.FilePath.ToString(), drFile.File_Id.ToString() + " - *.*");
                                                if (othershareFiles.Length > 0)
                                                {
                                                    isFileAvailable = true;
                                                    LogEvents("Information", "FS_FileDownload", string.Concat(dr.FilePath.ToString(), "\\", drFile.File_Id.ToString()));
                                                    break;
                                                }
                                            }
                                            if (!isFileAvailable)
                                            {
                                                getAttachment(Convert.ToInt32(drFile.File_Id), filter, drFile.threadId.ToString());
                                                Thread.Sleep(5000);
                                            }
                                        }
                                        lock (dtSuccessThreadId.Rows.SyncRoot)
                                        {
                                            dtSuccessThreadId.Rows.Add(drFile.threadId.ToString());
                                        }
                                        countdownEvent.Signal();
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!string.IsNullOrEmpty(drFile.threadId.ToString()))
                                        {
                                            lock (dtFailedThreadId.Rows.SyncRoot)
                                            {
                                                dtFailedThreadId.Rows.Add(drFile.threadId.ToString());
                                                failedFileThreadIds.Append("," + drFile.threadId.ToString());
                                            }
                                        }
                                        countdownEvent.Signal();
                                        LogEvents("Error", drFile.File_Id.ToString(), ex.ToString());
                                    }
                                }).Start();
                                prevFileId = drFile.File_Id.ToString();
                            }
                            else
                            {
                                lock (dtSuccessThreadId.Rows.SyncRoot)
                                {
                                    dtSuccessThreadId.Rows.Add(drFile.threadId.ToString());
                                }
                                countdownEvent.Signal();

                            }
                        }
                        countdownEvent.Wait();
                        countdownEvent.Dispose();
                        Thread.Sleep(3000);
                        parts--;
                        skipCount++;
                        if (dtSuccessThreadId.Rows.Count > 0)
                        {
                            updateFileDownloadStatus("FilesDownloaded", "PagesRenamed", dtSuccessThreadId);
                            updateFileDownloadStatus("FilesDownloaded", "FilesDownloadedFailed", dtSuccessThreadId);

                            //successFileThreadIds = new StringBuilder();
                            dtSuccessThreadId.Rows.Clear();
                        }
                    }
                    //if (failedFileThreadIds.Length > 1)
                    if (dtFailedThreadId.Rows.Count > 0)
                    {
                        LogEvents("Error", "failedFileIds", failedFileThreadIds.ToString());
                        //updateFileDownloadStatus("FilesDownloadedFailed", "'UsersLoaded','FilesDownloadedFailed','FilesDownloaded'", dtFailedThreadId);
                        updateFileDownloadStatus("FilesDownloadedFailed", "PagesRenamed", dtFailedThreadId);
                        updateFileDownloadStatus("FilesDownloadedFailed", "FilesDownloadedFailed", dtFailedThreadId);
                        updateFileDownloadStatus("FilesDownloadedFailed", "FilesDownloaded", dtFailedThreadId);
                        updateThreadswithNoAttachment("PagesRenamed", "FilesDownloaded");
                        Environment.Exit(0);
                    }
                    LogEvents("Information", "UpdateNoAttach", "FilesDownloaded");
                    updateThreadswithNoAttachment("PagesRenamed", "FilesDownloaded");
                }
                else if (filter == "page")
                {
                    StringBuilder failedPageThreadIds = new StringBuilder();
                    StringBuilder successPageThreadIds = new StringBuilder();

                    int parts = res.Count / 30;
                    if ((res.Count % 30) > 0)
                        parts++;
                    int skipCount = 0;
                    int pageCount = 0;
                    while (parts > 0)
                    {
                        string prevPageId = string.Empty;
                        foreach (Yammer_DownloadAttachments_Result drPage in res.Skip(skipCount * 30).Take(30))
                        {
                            if (prevPageId != drPage.File_Id.ToString())
                            {
                                try
                                {
                                    var file = Directory.GetFiles(pagedirPath, drPage.File_Id.ToString());
                                    if (file.Length == 1)
                                        File.Delete(Path.Combine(pagedirPath, drPage.File_Id.ToString()));
                                    string[] downloadPages = Directory.GetFiles(pagedirPath, drPage.File_Id.ToString() + ".html");
                                    string[] sharePages = Directory.GetFiles(pagedirPath, drPage.File_Id.ToString() + "-*.html");
                                    if (downloadPages.Length == 0 && sharePages.Length == 0)
                                    {
                                        List<Yammer_GetOldSharePathList_Result> resOldSP = new List<Yammer_GetOldSharePathList_Result>();
                                        using (YETIDBEntities yeticontext = new YETIDBEntities())
                                        {

                                            resOldSP = yeticontext.Yammer_GetOldSharePathList(false, "Pages").ToList();
                                        }

                                        DataSet ShareDs = new DataSet();

                                        bool isPageAvailable = false;
                                        foreach (Yammer_GetOldSharePathList_Result dr in resOldSP)
                                        {
                                            string[] othershareFiles = Directory.GetFiles(dr.FilePath.ToString(), drPage.File_Id.ToString() + " - *.html");
                                            if (othershareFiles.Length > 0)
                                            {
                                                isPageAvailable = true;
                                                LogEvents("Information", "FS_PageDownload", string.Concat(dr.FilePath.ToString(), "\\", drPage.File_Id.ToString()));
                                                break;
                                            }
                                        }
                                        if (!isPageAvailable)
                                        {
                                            getAttachment(Convert.ToInt32(drPage.File_Id), filter, drPage.threadId.ToString());
                                            Thread.Sleep(5000);
                                        }
                                    }
                                    pageCount++;
                                    lock (dtSuccessThreadId.Rows.SyncRoot)
                                    {
                                        //successPageThreadIds.Append("," + drPage[1].ToString());
                                        dtSuccessThreadId.Rows.Add(drPage.threadId.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!string.IsNullOrEmpty(drPage.threadId.ToString()))
                                    {
                                        lock (dtFailedThreadId.Rows.SyncRoot)
                                        {
                                            failedPageThreadIds.Append("," + drPage.threadId.ToString());
                                            dtFailedThreadId.Rows.Add(drPage.threadId.ToString());
                                        }
                                    }
                                    LogEvents("Error", failedPageThreadIds.ToString(), ex.ToString());
                                }
                            }
                            else
                            {
                                pageCount++;
                                lock (dtSuccessThreadId.Rows.SyncRoot)
                                {
                                    dtSuccessThreadId.Rows.Add(drPage.threadId.ToString());
                                }
                            }
                        }
                        Thread.Sleep(5000);
                        parts--;
                        skipCount++;
                        if (dtSuccessThreadId.Rows.Count > 0)
                        {
                            //updateFileDownloadStatus("PageDownloaded", "'FilesDownloaded','PageDownloadedFailed'", dtSuccessThreadId);
                            updateFileDownloadStatus("PageDownloaded", "FilesDownloaded", dtSuccessThreadId);
                            updateFileDownloadStatus("PageDownloaded", "PageDownloadedFailed", dtSuccessThreadId);

                            //successPageThreadIds = new StringBuilder();
                            dtSuccessThreadId.Rows.Clear();
                        }
                    }
                    if (dtFailedThreadId.Rows.Count > 0)
                    {
                        LogEvents("Error", "failedPageIds", failedPageThreadIds.ToString());
                        //updateFileDownloadStatus("PageDownloadedFailed", "'FilesDownloaded','PageDownloadedFailed','PageDownloaded'", dtFailedThreadId);
                        updateFileDownloadStatus("PageDownloadedFailed", "FilesDownloaded", dtFailedThreadId);
                        updateFileDownloadStatus("PageDownloadedFailed", "PageDownloadedFailed", dtFailedThreadId);
                        updateFileDownloadStatus("PageDownloadedFailed", "PageDownloaded", dtFailedThreadId);

                        updateThreadswithNoAttachment("FilesDownloaded", "PageDownloaded");
                        Environment.Exit(0);
                    }
                    LogEvents("Information", "UpdateNoAttach", "PageDownloaded");
                    updateThreadswithNoAttachment("FilesDownloaded", "PageDownloaded");
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "DownloadAttachments", ex.ToString());
                Environment.Exit(0);
            }
        }

        private static void updateThreadswithNoAttachment(string prevStatus, string newStatus)
        {
            try
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_updateThreadswithNoAttachment(newStatus, prevStatus, Environment.MachineName);
                }


            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("deadlocked"))
                {
                    Thread.Sleep(3000);
                    updateThreadswithNoAttachment(prevStatus, newStatus);
                }
                else
                    throw ex;
            }
        }

   
        private static void LoadUsersFromBody()
        {
            try
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_LoadUserIdFromBody(Environment.MachineName);
                }


                //UpdateUserDetailsFromYammer();
                updateStatus("FileLoaded", "UsersLoaded");
            }
            catch (Exception ex)
            {
                LogEvents("Error", "LoadingUsers", ex.ToString());
                Environment.Exit(0);
            }
        }

        private static async Task UpdateUserDetailsFromYammerAsync()
        {
            Int64 userId = 0;
            try
            {
                Int64?[] userIDs;
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    userIDs = yeticontext.Yammer_GetUserIds().ToArray();
                }

                

                for (int i = 0; i < userIDs.Length; i++)
                {
                    try
                    {
                        Int64.TryParse(userIDs[i].ToString(), out userId);
                        if (userId > 0)
                        {
                            dynamic feed = await getUserInformation(userId);
                            if (feed != null)
                            {
                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {

                                    yeticontext.Yammer_UpdateUserDetails(Convert.ToString(feed.full_name).Replace("'", "''"), Convert.ToString(feed.name) ?? DBNull.Value, userId);
                                }


                            }
                            else
                            {

                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {

                                    yeticontext.Yammer_UpdateUserDetails(userIDs[i].ToString(), userIDs[i].ToString(), userId);
                                }

                            }
                        }
                        else
                        {

                            using (YETIDBEntities yeticontext = new YETIDBEntities())
                            {

                                yeticontext.Yammer_UpdateUserDetails(userIDs[i].ToString(), userIDs[i].ToString(), Convert.ToInt64(userIDs[i]));
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        LogEvents("Error", "UserId:" + userIDs[i].ToString(), ex.ToString());
                        Environment.Exit(0);
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "UserId:" + userId.ToString(), ex.ToString());
                Environment.Exit(0);
            }
        }
        private static void LoadDataFromCSV()
        {
            string tempFileDate = string.Empty;
            string tempFilePath = string.Empty;
            string tempFileName = string.Empty;
            try
            {
                if (!Directory.Exists(dirPath))
                    Environment.Exit(0);
                DirectoryInfo ydi = new DirectoryInfo(dirPath);
                FileInfo[] fileList = ydi.GetFiles("*.zip");
                string status = string.Empty;
                if (fileList.Count() > 0)
                {
                    foreach (FileInfo yfi in fileList)
                    {
                        #region old_approach
                       
                        #endregion

                        string zipPath = yfi.FullName;
                        string csvfileName = string.Empty;
                        string tempextractPath = LogPath + zipPath.Substring(zipPath.LastIndexOf("\\")).Split('.')[0];
                        tempFileDate = yfi.Name.Substring(yfi.Name.IndexOf('-') + 1).Split('.')[0];
                        tempFileDate = string.Concat(tempFileDate.Split('T')[0], " ", tempFileDate.Split('T')[1].Replace('-', ':'), ".000");
                        string startDate = Convert.ToDateTime(tempFileDate).AddDays(-1).ToString();

                        using (YETIDBEntities yeticontext = new YETIDBEntities())
                        {

                            status = yeticontext.Yammer_GetStatusForLoadData(tempFileDate).ToString();
                        }
                        if (status == "FileLoaded" || status == "UsersLoaded")
                        {
                            try
                            {
                                File.Delete(zipPath);
                            }
                            catch (Exception ex) { }
                            continue;
                        }

                        if (string.IsNullOrEmpty(status))
                        {
                            long recordCount = 0;
                            long filesCount = 0;
                            long pagesCount = 0;
                            long groupsCount = 0;
                            long topicsCount = 0;
                            long usersCount = 0;
                            int exitCount = 6;

                            if (!Directory.Exists(tempextractPath))
                                Directory.CreateDirectory(tempextractPath);
                            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries.Where(a => a.Name == "Messages.csv" || a.Name == "Files.csv" || a.Name == "Users.csv" || a.Name == "Pages.csv" || a.Name == "Topics.csv" || a.Name == "log.txt"))
                                {
                                    if (entry.Name == "log.txt")
                                    {
                                        var logstream = entry.Open();
                                        string log_file = string.Empty;
                                        using (StreamReader reader = new StreamReader(logstream, Encoding.UTF8))
                                        {
                                            log_file = reader.ReadToEnd();
                                        }
                                        string[] records = log_file.Split('\n');
                                        foreach (string record in records)
                                        {
                                            if (record.Contains("Number of exported MESSAGES:") && !record.Contains("BOUND"))
                                            {
                                                recordCount = Convert.ToInt32(record.Split(':')[1]);
                                                exitCount--;
                                            }
                                            else if (record.Contains("Number of exported PAGES:"))
                                            {
                                                pagesCount = Convert.ToInt32(record.Split(':')[1]);
                                                exitCount--;
                                            }
                                            else if (record.Contains("Number of exported GROUPS:"))
                                            {
                                                groupsCount = Convert.ToInt32(record.Split(':')[1]);
                                                exitCount--;
                                            }
                                            else if (record.Contains("Number of exported TOPICS:"))
                                            {
                                                topicsCount = Convert.ToInt32(record.Split(':')[1]);
                                                exitCount--;
                                            }
                                            else if (record.Contains("Number of exported FILES:"))
                                            {
                                                filesCount = Convert.ToInt32(record.Split(':')[1]);
                                                exitCount--;
                                            }
                                            else if (record.Contains("Number of exported USERS:"))
                                            {
                                                usersCount = Convert.ToInt32(record.Split(':')[1]);
                                                exitCount--;
                                            }

                                            if (exitCount <= 0)
                                                break;

                                        }
                                        continue;
                                    }
                                    var stream = entry.Open();
                                    string whole_file = string.Empty;
                                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        whole_file = reader.ReadToEnd();
                                    }

                                    whole_file = whole_file.Replace("\"\",\"\"", "','"); //Some message body has "123","456" in it. So to handle that we first replace " with '. So message body will be like "123','456"
                                                                                         //By the above replace some other column values also modified. For example, if continuously 2 column are empty then it is shown as "","". The above replace method will update these also.
                                    while (whole_file.Contains(",',',")) //After that replaced, other empty columns end up having ,','. To handle this, we are doing the following replaces
                                    {
                                        whole_file = whole_file.Replace(",',',", ",\"\",\"\",");
                                    }
                                    whole_file = whole_file.Replace(",','\n\"", ",\"\",\"\"\n\""); // To handle last row last 2 columns if they are empty
                                    whole_file = whole_file.Replace(",',", ",\"\","); // To handle if only 1 empty column is modified.
                                    whole_file = whole_file.Replace(",'\n", ",\"\"\n");  // To handle last row last column if it is empty
                                    whole_file = whole_file.Replace("\"',\"", "\"\"\",\"");
                                    whole_file = whole_file.Replace(",'\"", ",\"\"\"");
                                    whole_file = whole_file.Replace("\",\"", "\r\t"); //Spliting column with \r\t, so that bulk insert to data can be done 
                                    whole_file = whole_file.Replace("\"\n\"", "\\z");//Spliting row with \z, so that bulk insert to data can be done 
                                    whole_file = whole_file.Replace("\"\\z\"", "\"\n\"");
                                    whole_file = whole_file.Replace(",\"\r\t\",", ",'',");
                                    whole_file = whole_file.Remove(whole_file.Length - 2, 1);
                                    string[] lines = whole_file.Split(new string[] { "\\z" }, StringSplitOptions.None);
                                    lines = lines.Skip(1).ToArray();
                                    string[] updatedLines = new string[lines.Count()];
                                    csvfileName = "export" + yfi.Name.Substring(yfi.Name.IndexOf('-'));
                                    for (int i = 0; i < lines.Count(); i++)
                                    {
                                        if (entry.Name == "Messages.csv")
                                        {
                                            if (i + 1 < lines.Count())
                                            {
                                                int id = 0;
                                                int j = i;
                                                int.TryParse(lines[i + 1].Split(new string[] { "\r\t" }, StringSplitOptions.None)[0], out id);
                                                if (id == 0)
                                                {
                                                    do
                                                    {
                                                        lines[i] = lines[i] + "\n" + lines[j + 1];
                                                        j++;
                                                        int.TryParse(lines[j + 1].Split(new string[] { "\r\t" }, StringSplitOptions.None)[0], out id);
                                                    }
                                                    while (id == 0);
                                                    updatedLines[i] = lines[i] + "\r\t" + Environment.MachineName + "\r\t" + csvfileName + "\r\t" + string.Empty;
                                                    i = j;
                                                }
                                                else
                                                    updatedLines[i] = lines[i] + "\r\t" + Environment.MachineName + "\r\t" + csvfileName + "\r\t" + string.Empty;
                                            }
                                            else
                                                updatedLines[i] = lines[i] + "\r\t" + Environment.MachineName + "\r\t" + csvfileName + "\r\t" + string.Empty;

                                        }
                                        else
                                            updatedLines[i] = lines[i] + "\r\t" + Environment.MachineName + "\r\t" + csvfileName;
                                    }
                                    if (updatedLines.Count() > 0)
                                    {
                                        if (File.Exists(tempextractPath + "\\" + entry.Name.Split('.')[0] + ".txt"))
                                        {
                                            Thread.Sleep(500);
                                            File.Delete(tempextractPath + "\\" + entry.Name.Split('.')[0] + ".txt");
                                        }
                                        System.IO.File.WriteAllLines(tempextractPath + "\\" + entry.Name.Split('.')[0] + ".txt", updatedLines);
                                        tempFilePath = tempextractPath + "\\" + entry.Name.Split('.')[0] + ".txt";
                                        tempFileName = entry.Name.Split('.')[0];
                                        try
                                        {
                                            using (YETIDBEntities yeticontext = new YETIDBEntities())
                                            {

                                                yeticontext.Yammer_BulkInsert(tempextractPath + "\\" + entry.Name.Split('.')[0] + ".txt", entry.Name.Split('.')[0]);
                                            }


                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex.Message.Contains("Bulk load data conversion error ") || ex.Message.Contains("Conversion failed when converting date and/or time from character string"))
                                            {
                                                if (tempFileName.Contains("Messages"))
                                                    BulkInsertValidation(tempFilePath, tempFileName + "Sample");
                                                else
                                                    throw ex;
                                            }
                                            else
                                                throw ex;
                                        }
                                        Thread.Sleep(500);
                                        File.Delete(tempextractPath + "\\" + entry.Name.Split('.')[0] + ".txt");
                                    }
                                }
                                if (!verifyMessagesCount(recordCount, "export" + yfi.Name.Substring(yfi.Name.IndexOf('-')), pagesCount, filesCount, groupsCount, topicsCount, usersCount))
                                    updateStatus("", "DBLoaded");
                                else
                                {
                                    LogEvents("Error", csvfileName, "MessageCount Mismatches");
                                    Environment.Exit(0);
                                }
                            }
                        }
                        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName.StartsWith("files/") && !File.Exists(Path.Combine(filedirPath, entry.Name)))
                                    entry.ExtractToFile(Path.Combine(filedirPath, entry.Name));
                                else if (entry.FullName.StartsWith("pages/") && !File.Exists(Path.Combine(pagedirPath, entry.Name)))
                                    entry.ExtractToFile(Path.Combine(pagedirPath, entry.Name));
                            }
                        }
                        try
                        {
                            File.Delete(zipPath);
                            Directory.Delete(tempextractPath);
                        }
                        catch (Exception ex) { }
                        updateStatus("DBLoaded", "FileLoaded");
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "export - " + tempFileDate.Replace(':', '-') + ".zip", ex.ToString());
                Environment.Exit(0);
            }

        }
        private static void BulkInsertValidation(string filePath, string tablename)
        {
            try
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_BulkInsert(filePath, tablename);

                    yeticontext.Yammer_CleanupMessageTable();
                }

            }
            catch (Exception ex)
            {
                LogEvents("Error", filePath, ex.ToString());
                Environment.Exit(0);
            }
        }
      
        private static string CheckFilePathLength(string dirPath, string fileId, string filename)
        {
            string fileFullPath = Path.Combine(dirPath, fileId + "-" + filename);
            int fixedLength = (dirPath.Length + fileId.Length);
            if (fileFullPath.Length >= 245)
            {
                if (filename.Length > (245 - fixedLength))
                {
                    filename = filename.Substring(0, 100);
                }
            }
            return Path.Combine(dirPath, fileId + "-" + filename);
        }

        private static bool verifyMessagesCount(long recordCount, string csvFilename, long pagesCount = 0, long filesCount = 0, long groupsCount = 0, long topicsCount = 0, long usersCount = 0)
        {
            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                ObjectParameter Output = new ObjectParameter("IsFailed", SqlDbType.Bit);
                yeticontext.Database.CommandTimeout = 0;
                yeticontext.Yammer_VerifyMessagesCount(csvFilename, recordCount, filesCount, pagesCount, groupsCount, topicsCount, usersCount, Output);

                return Convert.ToBoolean(Output.Value);
            }



        }

        private static List<string> ParseArchievedXML(DataTable messageTable, string thread_id, out bool isModified)
        {
            List<string> xmlnodes = new List<string>();
            isModified = false;
            string xmlnode = string.Empty;
            XmlDocument xmlDoc = new XmlDocument();
            //added for xml resovler
            XmlUrlResolver resolver = new XmlUrlResolver();
            resolver.Credentials = CredentialCache.DefaultCredentials;
            xmlDoc.XmlResolver = resolver;
            //end added

            xmlDoc.LoadXml(messageTable.Rows[0]["ThreadXMLContent"].ToString());
            XmlNodeList xmlNodesList = xmlDoc.SelectNodes("/Thread/ParentMessage");
            if (xmlNodesList.Count > 0)
            {
                isModified = true;
                XmlNode parentNode = xmlNodesList.Item(0);
                xmlnode = xmlnode + "`" + "parent";
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[1].InnerText; //ThreadId
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[2].InnerText; //SenderFullname
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[3].InnerText; //SenderEmail
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[4].InnerText; //Timestamp
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[5].InnerText; //Body
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[6].InnerText; //Attachment
                xmlnodes.Add(xmlnode.Remove(0, 1));
                xmlnode = string.Empty;
                if (parentNode.ChildNodes.Count > 7)
                {
                    if (parentNode.ChildNodes[7].HasChildNodes) //check for reply threads
                    {
                        foreach (XmlNode childNode in parentNode.ChildNodes[7].ChildNodes)
                        {
                            xmlnode = xmlnode + "`" + "child";
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[0].InnerText;//ThreadId
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[1].InnerText;//SenderFullname
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[2].InnerText;//SenderEmail
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[3].InnerText;//Timestamp
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[4].InnerText;//Body
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[5].InnerText;//Attachment
                            xmlnodes.Add(xmlnode.Remove(0, 1));
                            xmlnode = string.Empty;
                        }
                    }
                }
            }
            xmlNodesList = xmlDoc.SelectNodes("/Thread/ReplyMessage");
            if (xmlNodesList.Count > 0)
            {
                isModified = true;
                XmlNode parentNode = xmlNodesList.Item(0);
                foreach (XmlNode childNode in parentNode.ChildNodes)
                {
                    xmlnode = xmlnode + "`" + "child";
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[0].InnerText;//ThreadId
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[1].InnerText;//SenderFullname
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[2].InnerText;//SenderEmail
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[3].InnerText;//Timestamp
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[4].InnerText;//Body
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[5].InnerText;//Attachment
                    xmlnodes.Add(xmlnode.Remove(0, 1));
                    xmlnode = string.Empty;
                }
            }
            return xmlnodes;
        }
       

        private static string ApplyUserInfoInBody(string body, List<Yammer_GetUsrsToGenFls_Result> dsUserTable)
        {
            try
            {
                while (body.Contains("[[user:"))
                {
                    string temp_body = body.Substring(body.IndexOf("[[user:") + 7);
                    string userId = string.Empty;
                    if (temp_body.Contains("]]"))
                    {
                        userId = temp_body.Substring(0, temp_body.IndexOf("]]"));
                        body = body.Replace("[[user:" + userId + "]]", "[user:" + userId + "]");
                    }
                    int id = 0;
                    int.TryParse(userId, out id);
                    if (id == 0)
                    {
                        return body;
                    }
                    //string searchExpression = "UserId = '" + userId.ToString().Trim() + "'";
                    List<Yammer_GetUsrsToGenFls_Result> UserRow = dsUserTable.Where(u => u.UserId == Convert.ToInt32(userId.ToString().Trim())).ToList();
                    //DataRow[] UserRow = dsUserTable.Select(searchExpression);
                    if (UserRow.Count() > 0)
                    {
                        foreach (Yammer_GetUsrsToGenFls_Result drUser in UserRow)
                        {
                            if (body.Contains("[user:" + userId + ":" + drUser.EmailAlias.ToString() + "]"))
                                body = body.Replace("[user:" + userId + ":" + drUser.EmailAlias.ToString() + "]", string.Concat("<a href=''>",
                                    drUser.FullName.ToString(), "</a>"));
                            else if (body.Contains("[user:" + userId))
                            {
                                body = body.Replace("[user:" + userId, string.Concat("<", drUser.FullName.ToString(), ">"));
                                string fName = string.Concat("<", drUser.FullName.ToString(), ">");
                                int startIndex = body.IndexOf(fName) + fName.ToString().Length;
                                string tbody = body.Substring(startIndex);
                                int countDelete = tbody.IndexOf("]") + 1;
                                body = body.Remove(startIndex, countDelete);
                                body = body.Replace(fName, string.Concat("<a href=''>", drUser.FullName.ToString(), "</a>"));
                            }
                        }
                    }
                }
                while (body.Contains("[User:"))
                {
                    string temp_body = body.Substring(body.IndexOf("[User:") + 6);

                    if (temp_body.Contains(':'))
                    {
                        string userId = temp_body.Substring(0, temp_body.IndexOf(":"));
                        int id = 0;
                        int.TryParse(userId, out id);
                        if (id == 0)
                        {
                            return body;
                        }
                        //string searchExpression = "UserId = '" + userId.ToString().Trim() + "'";
                        List<Yammer_GetUsrsToGenFls_Result> UserRow = dsUserTable.Where(u => u.UserId == Convert.ToInt32(userId.ToString().Trim())).ToList();

                        if (UserRow.Count() > 0)
                        {
                            foreach (Yammer_GetUsrsToGenFls_Result drUser in UserRow)
                            {
                                if (body.Contains("[User:" + userId + ":" + drUser.EmailAlias.ToString() + "]"))
                                    body = body.Replace("[User:" + userId + ":" + drUser.EmailAlias.ToString() + "]",
                                        string.Concat("<a href=''>", drUser.FullName.ToString(), "</a>"));
                                //begin modified 20170426
                                //else
                                else if (body.Contains("[User:" + userId))
                                //end modified
                                {
                                    body = body.Replace("[User:" + userId, string.Concat("<", drUser.FullName.ToString(), ">"));
                                    string fName = string.Concat("<", drUser.FullName.ToString(), ">");
                                    int startIndex = body.IndexOf(fName) + fName.ToString().Length;
                                    string tbody = body.Substring(startIndex);
                                    int countDelete = tbody.IndexOf("]") + 1;
                                    body = body.Remove(startIndex, countDelete);
                                    body = body.Replace(fName, string.Concat("<a href=''>", drUser.FullName.ToString(), "</a>"));
                                }
                            }
                        }
                        else
                        {
                            if (body.Contains("[User:" + userId + ":"))
                                body = body.Replace("[User:" + userId + ":", "USR");
                            else
                            {
                                body = body.Replace("[User:" + userId, "USR");
                                body = body.Replace("USR]", "");
                            }
                        }
                    }
                    else
                    {
                        string userId = temp_body.Substring(0, temp_body.IndexOf("]"));
                        int id = 0;
                        int.TryParse(userId, out id);
                        if (id == 0)
                        {
                            return body;
                        }
                        //string searchExpression = "UserId = '" + userId.ToString().Trim() + "'";
                        List<Yammer_GetUsrsToGenFls_Result> UserRow = dsUserTable.Where(u => u.UserId == Convert.ToInt32(userId.ToString().Trim())).ToList();
                        if (UserRow.Count() > 0)
                        {
                            foreach (Yammer_GetUsrsToGenFls_Result drUser in UserRow)
                            {
                                if (body.Contains("[User:" + userId + "]"))
                                    body = body.Replace("[User:" + userId + "]", string.Concat("<a href=''>", drUser.FullName.ToString(), "</a>"));
                            }
                        }
                    }
                }


                if (body.Contains("cc: User:"))
                {
                    while (body.Contains("cc: User:") || body.Contains(", User:"))
                    {
                        string temp_body = body.Substring(body.IndexOf("User:") + 5);
                        string userId = temp_body.Substring(0, temp_body.IndexOf(":"));
                        int id = 0;
                        int.TryParse(userId, out id);
                        if (id == 0)
                        {
                            return body;
                        }
                        //string searchExpression = "UserId = '" + userId.ToString().Trim() + "'";
                        List<Yammer_GetUsrsToGenFls_Result> UserRow = dsUserTable.Where(u => u.UserId == Convert.ToInt32(userId.ToString().Trim())).ToList();
                        if (UserRow.Count() > 0)
                        {
                            foreach (Yammer_GetUsrsToGenFls_Result drUser in UserRow)
                            {
                                if (body.Contains("User:" + userId + ":" + drUser.EmailAlias.ToString()))
                                    body = body.Replace("User:" + userId + ":" + drUser.EmailAlias.ToString(), drUser.FullName.ToString());
                                else
                                    body = body.Replace("User:" + userId, drUser.FullName.ToString());
                            }
                        }
                        else
                        {
                            if (body.Contains("User:" + userId + ":"))
                                body = body.Replace("User:" + userId + ":", "");
                            else
                                body = body.Replace("User:" + userId, "");
                        }
                    }
                }
                return body;
            }
            catch (Exception ex)
            {
                LogEvents("Error", "UserInfoApply", body);
                throw ex;
            }
        }
      
        private static void ApplyPathsForAttachments(string attachmentMessage, string groupId, string groupName, string threadId, out string xmlnode)
        {
            bool isGreater = false;
            xmlnode = string.Empty;
            if (attachmentMessage.Contains("uploadedfile:"))
            {
                string attach_url = string.Empty;
                string[] attachments = attachmentMessage.Split(',');
                foreach (string attachment in attachments.Where(a => a.Trim().StartsWith("uploadedfile")))
                {
                    string groupFolderName = string.Empty;

                    if (Convert.ToString(groupId) == string.Empty)
                        groupFolderName = "Private_Conversations";
                    else
                        groupFolderName = string.Concat(groupId, " ", groupName.Replace("/", " "));

                    if (groupFolderName.Length > 65) // SharePoint folder lenght limit
                        groupFolderName = groupFolderName.Substring(0, 65);

                    string threadFolderName = string.Concat(YammerdirPath, "\\", RemoveUnsupportedFolderNameChars(groupFolderName));

                    threadFolderName = string.Concat(threadFolderName, "\\", threadId);
                    bool hasRecentVersion = false;
                    bool isVersioned = false;
                    int file_id = Convert.ToInt32(attachment.Split(':')[1]);
                    string[] shareFiles = Directory.GetFiles(filedirPath, file_id.ToString() + "-*.*");
                    if (shareFiles.Length == 0)
                    {
                        List<Yammer_GetOldSharePathList_Result> res = new List<Yammer_GetOldSharePathList_Result>();
                        using (YETIDBEntities yeticontext = new YETIDBEntities())
                        {
                            yeticontext.Database.CommandTimeout = 0;
                            res = yeticontext.Yammer_GetOldSharePathList(false, "Files").ToList();


                        }



                        foreach (Yammer_GetOldSharePathList_Result dr in res)
                        {
                            shareFiles = Directory.GetFiles(dr.FilePath.ToString(), file_id.ToString() + "-*.*");
                            if (shareFiles.Length > 0)
                                LogEvents("Information", "FS_FileAttachApply", string.Concat(dr.FilePath.ToString(), "\\", file_id.ToString()));
                            break;
                        }
                    }
                    if (shareFiles.Length > 0)
                    {
                        string tempThreadFolderName = threadFolderName;
                        foreach (string shareFile in shareFiles)
                        {
                            if (shareFile.Contains('-'))
                            {
                                string[] fileSplit = shareFile.Split('-');
                                string firstFilePart = fileSplit.First();
                                string lastFilePart = string.Join("-", fileSplit.Skip(1));
                                int result;
                                if (int.TryParse(lastFilePart.Split('.')[0], out result))
                                {
                                    threadFolderName = threadFolderName + "\\Attachments" + "\\" + file_id;
                                    isVersioned = true;
                                }
                                else
                                {
                                    threadFolderName = threadFolderName + "\\Attachments";
                                    isVersioned = false;
                                    hasRecentVersion = true;
                                }
                            }
                            else
                            {
                                threadFolderName = threadFolderName + "\\Attachments";
                                hasRecentVersion = true;
                            }
                            if (!Directory.Exists(threadFolderName))
                                Directory.CreateDirectory(threadFolderName);

                            string fullName = shareFile.ToString().Substring(shareFile.ToString().LastIndexOf("\\") + 1);
                            string fileName = fullName.Substring(fullName.IndexOf('-') + 1);
                            string[] split = fileName.ToString().Split('.');
                            string firstPart = string.Join(".", split.Take(split.Length - 1));
                            string lastPart = split.Last();
                            //FileInfo f = new FileInfo((shareFiles[0].ToString().Substring(shareFiles[0].ToString().LastIndexOf("\\"))));
                            FileInfo f = new FileInfo(shareFile.ToString());

                            if (!File.Exists(threadFolderName + shareFile.ToString().Substring(shareFile.ToString().LastIndexOf("\\"))))
                            {
                                if (f.Length > 2147483648)
                                {
                                    FileStream ostream = File.OpenRead(f.FullName);
                                    Ionic.Zip.ZipEntry oZipEntry = new Ionic.Zip.ZipEntry();
                                    Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile();
                                    oZipEntry = zip.AddEntry(Path.GetFileName(f.ToString()), f.ToString());
                                    Ionic.Zip.ZipOutputStream tempZStream = new Ionic.Zip.ZipOutputStream(File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\test.zip"), false);
                                    tempZStream.PutNextEntry(oZipEntry.FileName);
                                    int tempread;
                                    const int BufferSize = 4096;
                                    byte[] obuffer = new byte[BufferSize];
                                    while ((tempread = ostream.Read(obuffer, 0, obuffer.Length)) > 0)
                                    {
                                        tempZStream.Write(obuffer, 0, tempread);
                                    }
                                    if (tempZStream.Position > 2147483648)
                                    {
                                        string LargeFilePath = Path.Combine(LargerfilesPath, fileName);
                                        if (!isVersioned)
                                        {
                                            List<Yammer_GetOldSharePathList_Result> res = new List<Yammer_GetOldSharePathList_Result>();
                                            using (YETIDBEntities yeticontext = new YETIDBEntities())
                                            {
                                                yeticontext.Database.CommandTimeout = 0;
                                                res = yeticontext.Yammer_GetOldSharePathList(true, "Files").ToList();
                                            }


                                            bool isFileAvailable = false;
                                            foreach (Yammer_GetOldSharePathList_Result dr in res)
                                            {
                                                string[] othershareFiles = Directory.GetFiles(dr.FilePath.ToString(), fileName);
                                                if (othershareFiles.Length > 0)
                                                {
                                                    isFileAvailable = true;
                                                    LargeFilePath = Path.Combine(dr.FilePath.ToString(), fileName);
                                                    LogEvents("Information", "FS_largefilePath", LargeFilePath);
                                                    break;
                                                }
                                            }
                                            if (!isFileAvailable)
                                            {
                                                if (!File.Exists(LargeFilePath))
                                                    File.Move(shareFile.ToString(), LargeFilePath);
                                            }
                                            attach_url += string.Concat("&emsp;&emsp;<a href='", LargeFilePath, "'>", fileName, "</a>");
                                        }
                                        isGreater = true;
                                    }
                                    else
                                    {
                                        if ((threadFolderName + shareFile.ToString().Substring(shareFile.ToString().LastIndexOf("\\"))).Length >= 260)
                                        {
                                            string fId = shareFile.ToString().Substring(0, shareFile.ToString().IndexOf("-"));
                                            File.Copy(shareFile.ToString(), CheckFilePathLength(threadFolderName, fId, firstPart) + lastPart, true);
                                        }
                                        else
                                            File.Copy(shareFile.ToString(), threadFolderName + shareFile.ToString().Substring(shareFile.ToString().LastIndexOf("\\")), true);
                                        isGreater = false;
                                    }
                                    tempZStream.Dispose();
                                    tempZStream.Flush();
                                    tempZStream.Close(); // close the zip stream.
                                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\test.zip");
                                }
                                else
                                {
                                    if ((threadFolderName + shareFile.ToString().Substring(shareFile.ToString().LastIndexOf("\\"))).Length >= 260)
                                    {
                                        string fId = shareFile.ToString().Substring(0, shareFile.ToString().IndexOf("-"));
                                        File.Copy(shareFile.ToString(), CheckFilePathLength(threadFolderName, fId, firstPart) + lastPart, true);
                                    }
                                    else
                                        File.Copy(shareFile.ToString(), threadFolderName + shareFile.ToString().Substring(shareFile.ToString().LastIndexOf("\\")), true);
                                    isGreater = false;
                                }
                            }

                            if (!isGreater)
                            {
                                if (!isVersioned)
                                    //attach_url += string.Concat("&emsp;&emsp;<a href='", threadFolderName, "\\", RemoveUnsupportedFileNameChars(fullName), "'>", fileName, "</a>");
                                    attach_url += string.Concat("&emsp;&emsp;<a href='.\\Attachments\\", RemoveUnsupportedFileNameChars(fullName), "'>", fileName, "</a>");

                            }
                            threadFolderName = tempThreadFolderName;
                        }
                        if (Directory.Exists(threadFolderName + "\\Attachments" + "\\" + file_id))
                        {
                            //modified to update absolute path to relative path in html 20170315
                            if (hasRecentVersion)
                                //attach_url = attach_url + string.Concat("&nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='", threadFolderName + "\\Attachments" + "\\" + file_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                                attach_url = attach_url + string.Concat("&nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='.\\Attachments" + "\\" + file_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                            else
                                //attach_url = attach_url + string.Concat("&emsp;&emsp; Recent Version Not Available &nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='", threadFolderName + "\\Attachments" + "\\" + file_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                                attach_url = attach_url + string.Concat("&emsp;&emsp; Recent Version Not Available &nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='.\\Attachments" + "\\" + file_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                            //end modified 

                        }
                    }
                    else
                        attach_url += "&emsp;&emsp; File Not Available" + "<br/>";

                   
                }
                xmlnode = xmlnode + attach_url;
            }
            if (attachmentMessage.Contains("opengraphobject:"))
            {
                string attach_url = string.Empty;
                string[] attachments = attachmentMessage.Split(',');
                foreach (string attachment in attachments.Where(a => a.Trim().StartsWith("opengraphobject")))
                {
                    attach_url += string.Concat("&emsp;&emsp;<a href='", "https://www.yammer.com/microsoft.com/#/graph/" + attachment.Split(':')[1], "'>", "OG:" + attachment.Split(':')[1], "</a>") + "<br/>";
                }
                xmlnode = xmlnode + attach_url;
            }
            if (attachmentMessage.Contains("page:"))
            {
                string attach_url = string.Empty;
                string[] attachments = attachmentMessage.Split(',');
                foreach (string attachment in attachments.Where(a => a.Trim().StartsWith("page")))
                {
                    bool hasRecentVersion = false;
                    bool isVersion = false;
                    int page_id = Convert.ToInt32(attachment.Split(':')[1]);
                    string groupFolderName = string.Empty;

                    if (Convert.ToString(groupId) == string.Empty)
                        groupFolderName = "Private_Conversations";
                    else
                        groupFolderName = string.Concat(groupId, " ", groupName.Replace("/", " "));

                    if (groupFolderName.Length > 65) // SharePoint folder lenght limit
                        groupFolderName = groupFolderName.Substring(0, 65);

                    string threadFolderName = string.Concat(YammerdirPath, "\\", RemoveUnsupportedFolderNameChars(groupFolderName));

                    threadFolderName = string.Concat(threadFolderName, "\\", threadId);

                    string[] sharePages = Directory.GetFiles(pagedirPath, page_id.ToString() + "-*.html");
                    if (sharePages.Length == 0)
                    {
                        List<Yammer_GetOldSharePathList_Result> res = new List<Yammer_GetOldSharePathList_Result>();
                        using (YETIDBEntities yeticontext = new YETIDBEntities())
                        {
                            yeticontext.Database.CommandTimeout = 0;
                            res = yeticontext.Yammer_GetOldSharePathList(false, "Pages").ToList();


                        }


                        foreach (Yammer_GetOldSharePathList_Result dr in res)
                        {
                            sharePages = Directory.GetFiles(dr.FilePath.ToString(), page_id.ToString() + "-*.html");
                            if (sharePages.Length > 0)
                                LogEvents("Information", "FS_FileAttachApply", string.Concat(dr.FilePath.ToString(), "\\", page_id.ToString()));
                            break;
                        }

                    }
                    if (sharePages.Length > 0)
                    {
                        string tempThreadFolderName = threadFolderName;
                        foreach (string sharePage in sharePages)
                        {
                            if (sharePage.Contains('-'))
                            {
                                int result;
                                if (int.TryParse(sharePage.Split('-')[1].Split('.')[0], out result))
                                {
                                    threadFolderName = threadFolderName + "\\Attachments" + "\\" + page_id;
                                    isVersion = true;
                                }
                                else
                                {
                                    threadFolderName = threadFolderName + "\\Attachments";
                                    isVersion = false;
                                    hasRecentVersion = true;
                                }
                            }
                            else
                            {
                                threadFolderName = threadFolderName + "\\Attachments";
                                hasRecentVersion = true;
                            }
                            if (!Directory.Exists(threadFolderName))
                                Directory.CreateDirectory(threadFolderName);

                            

                            string fullName = sharePage.ToString().Substring(sharePage.ToString().LastIndexOf("\\") + 1);
                            string fileName = fullName.Substring(fullName.IndexOf('-') + 1);
                            string[] split = fileName.ToString().Split('.');
                            string firstPart = string.Join(".", split.Take(split.Length - 1));
                            string lastPart = split.Last();
                            if (!File.Exists(threadFolderName + sharePage.ToString().Substring(sharePage.ToString().LastIndexOf("\\"))))
                            {
                                if ((threadFolderName + sharePage.ToString().Substring(sharePage.ToString().LastIndexOf("\\"))).Length >= 260)
                                {
                                    string fId = sharePage.ToString().Substring(0, sharePage.ToString().IndexOf("-"));
                                    File.Copy(sharePage.ToString(), CheckFilePathLength(threadFolderName, fId, firstPart) + lastPart, true);
                                }
                                else
                                    File.Copy(sharePage.ToString(), threadFolderName + sharePage.ToString().Substring(sharePage.ToString().LastIndexOf("\\")), true);
                            }
                            if (!isVersion)
                                //attach_url += string.Concat("&emsp;&emsp;<a href='", threadFolderName, "\\", RemoveUnsupportedFileNameChars(fullName), "'>", fileName, "</a>") + "<br/>";
                                attach_url += string.Concat("&emsp;&emsp;<a href='.\\Attachments\\", RemoveUnsupportedFileNameChars(fullName), "'>", fileName, "</a>") + "<br/>";

                            threadFolderName = tempThreadFolderName;
                        }
                        if (Directory.Exists(threadFolderName + "\\Attachments" + "\\" + page_id))
                        {
                            if (hasRecentVersion)
                                //attach_url = attach_url + string.Concat("&nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='", threadFolderName + "\\Attachments" + "\\" + page_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                                attach_url = attach_url + string.Concat("&nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='.\\Attachments" + "\\" + page_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                            else
                                //attach_url = attach_url + string.Concat("&emsp;&emsp; Recent Version Not Available &nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='", threadFolderName + "\\Attachments" + "\\" + page_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                                attach_url = attach_url + string.Concat("&emsp;&emsp; Recent Version Not Available &nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='.\\Attachments" + "\\" + page_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                        }
                    }
                    else
                        attach_url += "&emsp;&emsp; Page Not Available" + "<br/>";
                }
                xmlnode = xmlnode + attach_url;
            }
        }

      
        private static async Task GenerateHTMLByThreads()
        {
            string csvFileName = string.Empty;
            StringBuilder threadIds = new StringBuilder();
            //begin added to fix string truncate issue 20170302
            DataTable dtThreadId = new DataTable("Table_ID");
            dtThreadId.Columns.Add("ID", System.Type.GetType("System.String"));
            DataTable dtFailedThreadId = new DataTable("Table_ID");
            dtFailedThreadId.Columns.Add("ID", System.Type.GetType("System.String"));
            //end added
            try
            {
                Yammer_GetThrdsAndUsrsToGenFls_Result res = new Yammer_GetThrdsAndUsrsToGenFls_Result();

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    yeticontext.Database.CommandTimeout = 0;
                    var command = yeticontext.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[Yammer_GetThrdsAndUsrsToGenFls]";
                    command.CommandType = CommandType.StoredProcedure;
                    yeticontext.Database.Connection.Open();
                    var reader = command.ExecuteReader();

                    List<Yammer_GetThrdsToGenFls_Result> _listOfThrds =
                    ((IObjectContextAdapter)yeticontext).ObjectContext.Translate<Yammer_GetThrdsToGenFls_Result>
                    (reader).ToList();
                    reader.NextResult();
                    List<Yammer_GetUsrsToGenFls_Result> _listOfUsrs =
                        ((IObjectContextAdapter)yeticontext).ObjectContext.Translate<Yammer_GetUsrsToGenFls_Result>
                    (reader).ToList();


                    res.ThrdsResult = _listOfThrds;
                    res.UsrsResult = _listOfUsrs;


                }



                var countdownEvent = new CountdownEvent(res.ThrdsResult.Count);

                int parts = res.ThrdsResult.Count / 1000;
                if ((res.ThrdsResult.Count % 1000) > 0)
                    parts++;
                int skipCount = 0;
                GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
               
                List<string> directoryList = Directory.EnumerateDirectories(YammerdirPath).ToList();
                while (parts > 0)
                {
                    countdownEvent = new CountdownEvent(res.ThrdsResult.Skip(skipCount * 1000).Take(1000).Count());
                    foreach (Yammer_GetThrdsToGenFls_Result dr in res.ThrdsResult.Skip(skipCount * 1000).Take(1000))
                    {

                        DataSet dsMes = new DataSet();

                        using (SqlConnection con = new SqlConnection(conn))
                        {
                            con.Open();
                            SqlCommand cmd = new SqlCommand();
                            cmd.Connection = con;
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "Yammer_GetMsgsAndArchThrdsByThrdId";
                            cmd.Parameters.AddWithValue("in_nvarchar_thread_id", dr.thread_id.ToString());
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            da.Fill(dsMes);
                            con.Close();
                        }

                        if (dsMes.Tables[0].Rows.Count > 0)
                        {
                            threadIds.Append("," + dr.thread_id.ToString());
                            //begin added for fixing string truncate issue 20170302
                            dtThreadId.Rows.Add(dr.thread_id.ToString());
                            //end added
                        }
                        new Thread(delegate ()
                        {
                            List<string> xmlnodes = new List<string>();
                            try
                            {
                                bool isNewThread = true;
                                bool isModified = false;
                                DataSet dsMessage = dsMes;
                                if (dsMessage.Tables[0].Rows.Count > 0)
                                {
                                    DataRow lastRow = dsMessage.Tables[0].Rows[dsMessage.Tables[0].Rows.Count - 1];
                                    //LogEvents("Information", "ProcessLog", "New thread started for File Generation" + lastRow["thread_id"].ToString());
                                    string xmlnode = string.Empty;

                                    if (dsMessage.Tables[1].Rows.Count > 0)
                                    {
                                        //LogEvents("Information", "ProcessLog", "ParseeArchivedXML Started");
                                        xmlnodes = ParseArchievedXML(dsMessage.Tables[1], dr.thread_id.ToString(), out isModified);
                                        //LogEvents("Information", "ProcessLog", "ParseeArchivedXML Completed");
                                    }
                                    if (xmlnodes.Count() > 0)
                                        isNewThread = false;
                                    bool isVersioned = false;

                                    foreach (DataRow drMessage in dsMessage.Tables[0].Rows)
                                    {
                                        csvFileName = Convert.ToString(drMessage["csvfilename"]);
                                        if (drMessage["csvfilename"] != null)
                                        {
                                            string fileName = drMessage["csvfilename"].ToString().Substring(drMessage["csvfilename"].ToString().IndexOf('-') + 1);
                                            string year = fileName.Substring(0, fileName.IndexOf('-'));
                                            YammerdirPath = Path.Combine(YammerdirPath, year);
                                        }
                                        else if (drMessage["created_at"] != null)
                                        {
                                            DateTime createdDate = Convert.ToDateTime(drMessage["created_at"]);
                                            string year = createdDate.Year.ToString();
                                            YammerdirPath = Path.Combine(YammerdirPath, year);
                                        }

                                        if (!string.IsNullOrEmpty(Convert.ToString(drMessage["replied_to_id"])))
                                        {
                                            if (!isVersioned)
                                            {
                                                isVersioned = true;
                                            }
                                        }
                                        if (!isVersioned)
                                            xmlnode = xmlnode + "parent" + "`";
                                        else
                                            xmlnode = xmlnode + "child" + "`";

                                        xmlnode = xmlnode + drMessage["id"].ToString() + "`";
                                        xmlnode = xmlnode + drMessage["sender_name"].ToString() + "`";
                                        xmlnode = xmlnode + drMessage["sender_email"].ToString() + "`";
                                        xmlnode = xmlnode + drMessage["created_at"].ToString() + "`";

                                        string body = drMessage["body"].ToString();

                                        //Replace user id by user name in body of the message
                                        //LogEvents("Information", "ProcessLog", "ApplyUserInfoInBody Started");
                                        body = ApplyUserInfoInBody(body, res.UsrsResult);
                                        //LogEvents("Information", "ProcessLog", "ApplyUserInfoInBody Completed");

                                        while (body.Contains("[Tag:"))
                                        {
                                            string tempbody = body;
                                            string tagName = string.Empty;
                                            tempbody = tempbody.Substring(tempbody.IndexOf("[Tag:") + 5);
                                            tempbody = tempbody.Substring(0, tempbody.IndexOf(']'));
                                            tagName = string.Concat("<a href='#'>", "#", tempbody.Substring(tempbody.IndexOf(':') + 1), "</a>");
                                            body = body.Replace("[Tag:" + tempbody + "]", tagName);
                                        }
                                        body = body.Replace("\v", "");
                                        xmlnode = xmlnode + body + "`";
                                        xmlnode.Replace("''''", "").Replace("'''", "''");
                                        //string re = @"[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]";
                                        //xmlnode = System.Text.RegularExpressions.Regex.Replace(xmlnode, re, "");


                                        if (drMessage["attachments"].ToString().Contains("uploadedfile:") || drMessage["attachments"].ToString().Contains("opengraphobject:") || drMessage["attachments"].ToString().Contains("page:"))
                                        {
                                            string attachmentXML = string.Empty;

                                            //LogEvents("Information", "ProcessLog", "ApplyPathsForAttachments started");
                                            //Copy the files to thread attachment folder and apply the paths in html
                                            ApplyPathsForAttachments(drMessage["attachments"].ToString(), drMessage["group_id"].ToString(), drMessage["group_name"].ToString(), dr.thread_id.ToString(), out attachmentXML);
                                            //LogEvents("Information", "ProcessLog", "ApplyPathsForAttachments completed");
                                            xmlnode = xmlnode + attachmentXML;
                                        }
                                        else
                                        {
                                            string attach_url = string.Empty;
                                            string[] attachments = drMessage["attachments"].ToString().Split(',');
                                            foreach (string attachment in attachments)
                                            {
                                                if (!attachment.Trim().StartsWith("uploadedfile") || !attachment.Trim().StartsWith("opengraphobject") || !attachment.Trim().StartsWith("page"))
                                                {
                                                    attach_url = attach_url + attachment;
                                                }
                                            }
                                            xmlnode = xmlnode + attach_url;
                                        }
                                        xmlnodes.Add(xmlnode);
                                        xmlnode = string.Empty;
                                    }
                                    string gfolderName = string.Empty;
                                    if (Convert.ToString(lastRow["group_id"]) == string.Empty)
                                        gfolderName = "Private_Conversations";
                                    else
                                        gfolderName = string.Concat(lastRow["group_id"], " ", RemoveUnsupportedFolderNameChars(lastRow["group_name"].ToString()).Replace("/", " "));

                                    //LogEvents("Information", "ProcessLog", "SaveAsXmlFile started");
                                    SaveAsXmlFile(xmlnodes, lastRow["thread_id"].ToString(), (Convert.ToString(lastRow["group_id"]) == string.Empty) ? "Private_Conversations" : RemoveUnsupportedFolderNameChars(lastRow["group_name"].ToString()).Replace("/", " "));
                                    //LogEvents("Information", "ProcessLog", "This thread saved as xml "+ lastRow["thread_id"].ToString());
                                    //LogEvents("Information", "ProcessLog", "SaveAsXmlFile completed");

                                    if (!isNewThread)
                                    {
                                        //LogEvents("Information", "ProcessLog", "Starting deletion of Existing thread " + lastRow["thread_id"].ToString());
                                        //FileShare deletion
                                        foreach (string direcTory in directoryList)
                                        {
                                            if (Convert.ToString(lastRow["group_id"]) == string.Empty)
                                            {
                                                string grName = Path.Combine(direcTory, "Private_Conversations");
                                                if (Directory.Exists(Path.Combine(grName, lastRow["thread_id"].ToString())))
                                                    Directory.EnumerateFiles(Path.Combine(grName, lastRow["thread_id"].ToString()), string.Concat("*", lastRow["thread_id"].ToString(), ".html")).ToList().ForEach(File.Delete);
                                                if (File.Exists(grName + "\\" + string.Concat(lastRow["thread_id"].ToString(), ".zip")))
                                                    Directory.EnumerateFiles(grName, string.Concat(lastRow["thread_id"].ToString(), ".zip")).ToList().ForEach(File.Delete);
                                            }
                                            else
                                            {
                                                List<string> grFolderName = Directory.EnumerateDirectories(direcTory, lastRow["group_id"] + "*").ToList();
                                                foreach (string grName in grFolderName)
                                                {
                                                    if (Directory.Exists(Path.Combine(grName, lastRow["thread_id"].ToString())))
                                                        Directory.EnumerateFiles(Path.Combine(grName, lastRow["thread_id"].ToString()), string.Concat("*", lastRow["thread_id"].ToString(), ".html")).ToList().ForEach(File.Delete);
                                                    if (File.Exists(grName + "\\" + string.Concat(lastRow["thread_id"].ToString(), ".zip")))
                                                        Directory.EnumerateFiles(grName, string.Concat(lastRow["thread_id"].ToString(), ".zip")).ToList().ForEach(File.Delete);
                                                }
                                            }
                                        }
                                        //SPDir deletion
                                        List<string> spDirList = Directory.EnumerateDirectories(SPDirPath).ToList();
                                        foreach (string direcTory in spDirList)
                                        {
                                            List<string> grFolderName = Directory.EnumerateDirectories(direcTory, (Convert.ToString(lastRow["group_id"]) == string.Empty) ? "Private_Conversations" : lastRow["group_id"] + "*", SearchOption.AllDirectories).ToList();
                                            foreach (string grName in grFolderName)
                                            {
                                                if (File.Exists(grName + "\\" + string.Concat(lastRow["thread_id"].ToString(), ".zip")))
                                                    Directory.EnumerateFiles(grName, string.Concat(lastRow["thread_id"].ToString(), ".zip")).ToList().ForEach(File.Delete);
                                            }
                                        }

                                        //Compression deletion
                                        List<string> cmbDirList = Directory.EnumerateDirectories(YammerCmpPath).ToList();
                                        foreach (string direcTory in cmbDirList)
                                        {
                                            List<string> grFolderName = Directory.EnumerateDirectories(direcTory, (Convert.ToString(lastRow["group_id"]) == string.Empty) ? "Private_Conversations" : lastRow["group_id"] + "*", SearchOption.AllDirectories).ToList();
                                            foreach (string grName in grFolderName)
                                            {
                                                if (File.Exists(grName + "\\" + string.Concat(lastRow["thread_id"].ToString(), ".zip")))
                                                    Directory.EnumerateFiles(grName, string.Concat(lastRow["thread_id"].ToString(), ".zip")).ToList().ForEach(File.Delete);
                                            }
                                        }
                                       
                                    }
                                    //LogEvents("Information", "ProcessLog", "CreateHTMLFromXMLFile started" + lastRow["thread_id"].ToString());
                                    CreateHTMLFromXMLFile(gfolderName, Path.Combine(dirPath, "XMLNodes", lastRow["thread_id"].ToString() + ".xml"), string.Concat(lastRow["created_at"], lastRow["thread_id"].ToString(), ".html"), lastRow["thread_id"].ToString());
                                    //LogEvents("Information", "ProcessLog", "CreateHTMLFromXMLFile completed" + lastRow["thread_id"].ToString());

                                    // Copy the thread file along with its group path. This Path act as a buffer for compression
                                    if (gfolderName.Length > 65) // SharePoint folder lenght limit
                                        gfolderName = gfolderName.Substring(0, 65);
                                    string cmpSourcePath = string.Concat(YammerdirPath, "\\", RemoveUnsupportedFolderNameChars(gfolderName).Trim(), "\\", lastRow["thread_id"].ToString());
                                    string cmpBufFolder = string.Concat(YammerCmpPath, "\\", Year, "\\", RemoveUnsupportedFolderNameChars(gfolderName).Trim(), "\\", lastRow["thread_id"].ToString());

                                    if (!Directory.Exists(cmpBufFolder))
                                        Directory.CreateDirectory(cmpBufFolder);

                                    using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                                    {
                                        process.StartInfo.UseShellExecute = false;
                                        process.StartInfo.CreateNoWindow = true;
                                        process.StartInfo.RedirectStandardOutput = false;
                                        process.StartInfo.FileName = "ROBOCOPY";
                                        process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" /E", cmpSourcePath, cmpBufFolder);
                                        process.Start();
                                        process.WaitForExit(2400 * 60 * 1000);
                                        if (process.HasExited)
                                        {
                                            if (process.ExitCode > 8)
                                            {
                                                LogEvents("Error", "RoboCopyLog", cmpSourcePath + " Compression Buffer file not copied");
                                            }
                                        }
                                    }
                                    if (isModified)
                                    {
                                        //if (gfolderName.Length > 65) // SharePoint folder lenght limit
                                        //    gfolderName = gfolderName.Substring(0, 65);
                                        if (Directory.EnumerateFiles(Path.Combine(YammerdirPath, RemoveUnsupportedFolderNameChars(gfolderName), dr.thread_id.ToString()), "*" + dr.thread_id.ToString() + ".html").Count() == 1)
                                        {
                                           

                                            using (SqlConnection con = new SqlConnection(conn))
                                            {
                                                SqlTransaction transaction;
                                                con.Open();
                                                transaction = con.BeginTransaction();
                                                SqlCommand cmd = new SqlCommand();
                                                cmd.Connection = con;
                                                cmd.Transaction = transaction;
                                                cmd.CommandTimeout = 0;
                                                try
                                                {
                                                    cmd.CommandType = CommandType.StoredProcedure;
                                                    cmd.CommandText = "Yammer_DeleteArchThread";
                                                    cmd.Parameters.AddWithValue("in_nvarchar_thread_id", dr.thread_id.ToString());
                                                    cmd.ExecuteNonQuery();

                                                    transaction.Commit();

                                                }
                                                catch (Exception ex)
                                                {
                                                    Thread.Sleep(500);
                                                    try
                                                    {
                                                        if (ex.ToString().Contains("deadlock"))
                                                        {
                                                            cmd.ExecuteNonQuery();
                                                            transaction.Commit();
                                                        }
                                                        else
                                                            LogEvents("Error", "Delete Archive Thread - " + dr.thread_id.ToString() + "_" + csvFileName, ex.ToString());
                                                    }
                                                    catch (Exception ex1)
                                                    {
                                                        if (!ex1.ToString().Contains("This SqlTransaction has completed; it is no longer usable"))
                                                        {
                                                            LogEvents("Error", "Delete Archive Thread - " + dr.thread_id.ToString() + "_" + csvFileName, ex1.ToString());
                                                            dtFailedThreadId.Rows.Add(dr.thread_id.ToString());
                                                            transaction.Rollback();
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        con.Close();
                                                    }
                                                }
                                                finally
                                                {
                                                    con.Close();
                                                }
                                            }
                                        }
                                    }
                                    //LogEvents("Information", "ProcessLog", "Signal sent" + lastRow["thread_id"].ToString());
                                }
                                // Signal the CountdownEvent
                                countdownEvent.Signal();
                            }
                            catch (Exception ex)
                            {
                                if (ex.ToString().Contains("Object reference not set to an instance of an object") && ex.ToString().Contains("ParseArchievedXML"))
                                    dtFailedThreadId.Rows.Add(dr.thread_id.ToString());
                                else if (!ex.ToString().Contains("This SqlTransaction has completed; it is no longer usable"))
                                    LogEvents("Error", dr.thread_id.ToString() + "_" + csvFileName, ex.ToString());
                                countdownEvent.Signal();
                            }
                        }).Start();
                    }
                    Thread.Sleep(3000);
                    LogEvents("Information", "ProcessLog", threadIds.ToString());
                    countdownEvent.Wait();
                    countdownEvent.Dispose();
                    Thread.Sleep(3000);
                    parts--;
                    skipCount++;
                    //begin modified to fix string truncate issue 20170302
                    //updateThreadStatus("PageDownloaded", "FilesGenerated", threadIds);
                    updateThreadStatus("PageDownloaded", "FilesGenerated", dtThreadId);
                    dtThreadId.Clear();
                    //end modified
                    threadIds = new StringBuilder();
                    LogEvents("Information", "ProcessLog", "ResetArchievedXML started");
                    if (ResetArchievedXML())
                    {
                        LogEvents("Information", "ProcessLog", "Restarting the service as there were few unformatted XMLs altered .");
                        Environment.Exit(0);
                    }
                    LogEvents("Information", "ProcessLog", "ResetArchievedXML completed");
                }
                updateStatus("PageDownloaded", "FilesGenerated");
                updateThreadStatus("FilesGenerated", "PageDownloaded", dtFailedThreadId);
            }
            catch (Exception ex)
            {
                LogEvents("Error", csvFileName, ex.ToString());
                Environment.Exit(0);
            }
        }

        static bool ResetArchievedXML()
        {
            int returnValue = 0;

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Database.CommandTimeout = 0;
                returnValue = Convert.ToInt32(yeticontext.Yammer_ResetArchievedXML());
            }

            if (returnValue > 0)
                return true;
            return false;
        }
      
        public static async Task<dynamic> getUserInformation(BigInteger userId)
        {
          
            string URL = "https://www.yammer.com/api/v1/users/" + userId + ".json";

            int maxRetries = 10;
            for (int retry = 0; retry < maxRetries; retry++) // If we got an network exception, it's usually because we are being throttled, to back off and try again
            {
                try
                {
                    WebRequest webRequest = WebRequest.Create(URL);
                    webRequest.Headers["Authorization"] = "Bearer " + restToken;
                    webRequest.ContentLength = 0;
                    string jsonPayload = new StreamReader(webRequest.GetResponse().GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    return JsonConvert.DeserializeObject(jsonPayload);
                    //break;  // Break out of the re-try for loop
                }
                catch (Exception ex) when (ex is WebException)
                {
                    if (ex.Message == "The remote server returned an error: (403) Forbidden." || ex.Message == "The remote server returned an error: (404) Not Found.")
                    {
                        retry = maxRetries;
                    }
                    else if (ex.Message == "The remote server returned an error: (429)." || ex.Message == "The operation has timed out")
                    {
                        Thread.Sleep(30000);
                    }
                    else
                    {
                        try
                        {
                            WebRequest webRequest = WebRequest.Create(URL);
                            webRequest.Headers["Authorization"] = "Bearer " + restToken;
                            webRequest.ContentLength = 0;
                            string jsonPayload = new StreamReader(webRequest.GetResponse().GetResponseStream(), Encoding.ASCII).ReadToEnd();
                            retry = maxRetries;
                            return JsonConvert.DeserializeObject(jsonPayload);
                        }
                        catch (System.Exception iex)
                        {
                            //Console.WriteLine(filename + " File could not be downloaded " + iex.Message);
                        }
                    }
                }
            }
            return null;
        }
        public static void getAttachment(long fileId, string filter, string threadId)
        {
            //string token = PafHelper.YammerConfiguration.restApiToken;
            //string token = YammerEncryption.Decrypt(HttpUtility.UrlDecode(PafHelper.YammerConfiguration.restApiToken.Replace("%%", "%")));
            if (filter == "uploadedfile")
            {
                string URL = "https://www.yammer.com/api/v1/uploaded_files/" + fileId + "/download";
                int maxRetries = 10;
                for (int retry = 0; retry < maxRetries; retry++) // If we got an network exception, it's usually because we are being throttled, to back off and try again
                {
                    try
                    {
                        using (WebClient client = new WebClient())
                        {
                            client.BaseAddress = URL;
                            client.Headers["Authorization"] = "Bearer " + restToken;
                            client.DownloadFile(new Uri(URL), filedirPath + "\\" + fileId);
                            string fName = System.Uri.UnescapeDataString(client.ResponseHeaders["Content-Disposition"].ToString().Split(';')[1]);
                            string FileType = string.Empty;
                            fName = fName.Replace("filename=\"", "");
                            if (fName.Contains('.'))
                            {
                                FileType = fName.Substring(fName.LastIndexOf('.')).Replace("\"", "");
                                if (FileType.Contains('/') || FileType.Contains('"'))
                                    FileType = string.Empty;
                                fName = fName.Substring(0, fName.LastIndexOf('.')).Trim();
                            }
                            fName = RemoveUnsupportedFileNameChars(fName);

                            string currentFileName = filedirPath + "\\" + fileId;
                            string updatedFileName = CheckFilePathLength(filedirPath, fileId.ToString(), fName) + FileType;

                            if (File.Exists(currentFileName) && !File.Exists(updatedFileName))
                            {
                                File.Move(currentFileName, updatedFileName);
                            }
                        }
                        retry = maxRetries;
                    }
                    catch (Exception ex) when (ex is WebException)
                    {
                        if (ex.Message == "The remote server returned an error: (403) Forbidden.")
                        {
                            try
                            {
                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {
                                    yeticontext.Database.CommandTimeout = 0;
                                    yeticontext.Yammer_LogMissedAttachments(filter,fileId,threadId);
                                }

                                
                                retry = maxRetries;
                            }
                            catch (Exception deadex)
                            {
                                if (!deadex.Message.Contains("deadlocked"))
                                {
                                    throw deadex;
                                }
                                Thread.Sleep(300);
                            }
                        }
                        else if (ex.Message == "The remote server returned an error: (404) Not Found.")
                        {
                            retry = maxRetries;
                            if (!File.Exists(filedirPath + "\\" + fileId + "-" + "V1" + ".txt"))
                            {
                                using (StreamWriter writer = new StreamWriter(filedirPath + "\\" + fileId + "-" + "V1" + ".txt", true))
                                {
                                    writer.WriteLine("File Not Available");
                                }
                            }
                        }
                        else if (ex.Message == "The remote server returned an error: (429)." || ex.Message == "The operation has timed out")
                        {
                            Thread.Sleep(30000);
                        }
                        else
                        {
                            try
                            {
                                using (WebClient client = new WebClient())
                                {
                                    client.BaseAddress = URL;
                                    client.Headers["Authorization"] = "Bearer " + restToken;
                                    client.DownloadFile(new Uri(URL), filedirPath + "\\" + fileId);
                                    string fName = System.Uri.UnescapeDataString(client.ResponseHeaders["Content-Disposition"].ToString().Split(';')[1]);
                                    string FileType = string.Empty;
                                    fName = fName.Replace("filename=\"", "");
                                    if (fName.Contains('.'))
                                    {
                                        FileType = fName.Substring(fName.LastIndexOf('.')).Replace("\"", "");
                                        fName = fName.Substring(0, fName.LastIndexOf('.')).Trim();
                                    }
                                    fName = RemoveUnsupportedFileNameChars(fName);

                                    string currentFileName = filedirPath + "\\" + fileId;
                                    string updatedFileName = CheckFilePathLength(filedirPath, fileId.ToString(), fName) + FileType;

                                    if (File.Exists(currentFileName) && !File.Exists(updatedFileName))
                                    {
                                        File.Move(currentFileName, updatedFileName);
                                    }
                                }
                                retry = maxRetries;
                            }
                            catch (System.Exception iex)
                            {
                                if (!File.Exists(filedirPath + "\\" + fileId + "-" + "V1" + ".txt"))
                                {
                                    using (StreamWriter writer = new StreamWriter(filedirPath + "\\" + fileId + "-" + "V1" + ".txt", true))
                                    {
                                        writer.WriteLine("File Not Available");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (filter == "page")
            {
                string URL = "https://www.yammer.com/api/v1/notes/" + fileId;
                int maxRetries = 10;
                string pageName = string.Empty;
                for (int retry = 0; retry < maxRetries; retry++) // If we got an network exception, it's usually because we are being throttled, to back off and try again
                {
                    try
                    {
                        WebRequest webRequest = WebRequest.Create(URL);
                        webRequest.Headers["Authorization"] = "Bearer " + restToken;
                        webRequest.ContentLength = 0;
                        string jsonPayload = new StreamReader(webRequest.GetResponse().GetResponseStream(), Encoding.ASCII).ReadToEnd();
                        if (jsonPayload.Contains("<download-url") && jsonPayload.Contains("</download-url"))
                        {
                            pageName = jsonPayload.Substring(jsonPayload.IndexOf("<name>") + 6);
                            pageName = pageName.Substring(0, pageName.IndexOf('<')).Trim();
                            pageName = RemoveUnsupportedFileNameChars(pageName);
                            string tempDownloadURL = jsonPayload.Substring(jsonPayload.IndexOf("<download-url>") + 14);
                            tempDownloadURL = tempDownloadURL.Substring(0, tempDownloadURL.IndexOf('<'));
                            URL = tempDownloadURL;
                        }
                        using (WebClient client = new WebClient())
                        {
                            client.BaseAddress = URL;
                            client.Headers["Authorization"] = "Bearer " + restToken;
                            client.DownloadFile(new Uri(URL), pagedirPath + "\\" + fileId);
                            string currentFileName = pagedirPath + "\\" + fileId;
                            string updatedFileName = CheckFilePathLength(pagedirPath, fileId.ToString(), pageName) + ".html";

                            if (File.Exists(currentFileName) && !File.Exists(updatedFileName))
                            {
                                File.Move(currentFileName, updatedFileName);
                            }
                        }
                        retry = maxRetries;
                    }
                    catch (Exception ex) when (ex is WebException)
                    {
                        if (ex.Message == "The remote server returned an error: (403) Forbidden.")
                        {
                            try
                            {
                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {
                                    yeticontext.Database.CommandTimeout = 0;
                                    yeticontext.Yammer_LogMissedAttachments(filter, fileId, threadId);
                                }

                                
                                retry = maxRetries;
                            }
                            catch (Exception deadex)
                            {
                                if (!deadex.Message.Contains("deadlocked"))
                                {
                                    throw deadex;
                                }
                                Thread.Sleep(300);
                            }
                        }
                        else if (ex.Message == "The remote server returned an error: (404) Not Found.")
                        {
                            retry = maxRetries;
                            if (!File.Exists(pagedirPath + "\\" + fileId + "-" + "V1" + ".html"))
                            {
                                using (StreamWriter writer = new StreamWriter(pagedirPath + "\\" + fileId + "-" + "V1" + ".html", true))
                                {
                                    writer.WriteLine("Page Not Available");
                                }
                            }
                        }
                        else if (ex.Message == "The remote server returned an error: (429)." || ex.Message == "The operation has timed out")
                        {
                            Thread.Sleep(6000);
                        }
                        else
                        {
                            try
                            {
                                WebRequest webRequest = WebRequest.Create(URL);
                                webRequest.Headers["Authorization"] = "Bearer " + restToken;
                                webRequest.ContentLength = 0;
                                string jsonPayload = new StreamReader(webRequest.GetResponse().GetResponseStream(), Encoding.ASCII).ReadToEnd();
                                if (jsonPayload.Contains("<download-url") && jsonPayload.Contains("</download-url"))
                                {
                                    string tempDownloadURL = jsonPayload.Substring(jsonPayload.IndexOf("<download-url>") + 14);
                                    tempDownloadURL = tempDownloadURL.Substring(0, tempDownloadURL.IndexOf('<'));
                                    URL = tempDownloadURL;
                                }
                                using (WebClient client = new WebClient())
                                {
                                    client.BaseAddress = URL;
                                    client.Headers["Authorization"] = "Bearer " + restToken;
                                    client.DownloadFile(new Uri(URL), pagedirPath + "\\" + fileId);
                                    string currentFileName = pagedirPath + "\\" + fileId;
                                    string updatedFileName = CheckFilePathLength(pagedirPath, fileId.ToString(), pageName) + ".html";

                                    if (File.Exists(currentFileName) && !File.Exists(updatedFileName))
                                    {
                                        File.Move(currentFileName, updatedFileName);
                                    }
                                }
                            }
                            catch (System.Exception iex)
                            {
                                if (!File.Exists(pagedirPath + "\\" + fileId + "-" + "V1" + ".html"))
                                {
                                    using (StreamWriter writer = new StreamWriter(pagedirPath + "\\" + fileId + "-" + "V1" + ".html", true))
                                    {
                                        writer.WriteLine("Page Not Available");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static string FormatData(string content, string newContent)
        {
            content += string.Concat(strtdStart, newContent, strtdEnd);
            return content;
        }

        private static void SaveAsXmlFile(List<string> xmlnodes, string fileName, string groupName)
        {
            bool isParentExists = false;
            bool isReplyTagAdded = false;
            if (!Directory.Exists(Path.Combine(dirPath, "XMLNodes")))
                Directory.CreateDirectory(Path.Combine(dirPath, "XMLNodes"));
            XmlTextWriter writer = new XmlTextWriter(Path.Combine(dirPath, "XMLNodes", fileName + ".xml"), System.Text.Encoding.Unicode);
            writer.WriteStartDocument(true);
            writer.Formatting = System.Xml.Formatting.Indented;
            writer.Indentation = 2;
            writer.WriteStartElement("Thread");
            foreach (string xmlnode in xmlnodes)
            {
                //begin added for antiXSS 20170310
                AntiXssEncoder.MarkAsSafe(
                   LowerCodeCharts.Default |
                   LowerCodeCharts.Cyrillic |
                   LowerCodeCharts.Arabic |
                   LowerCodeCharts.GreekAndCoptic |
                   LowerCodeCharts.Hebrew |
                   LowerCodeCharts.Thai,
                   LowerMidCodeCharts.HangulJamo,
                   MidCodeCharts.None,
                   UpperMidCodeCharts.CjkRadicalsSupplement |
                   UpperMidCodeCharts.CjkSymbolsAndPunctuation |
                   UpperMidCodeCharts.Hiragana |
                   UpperMidCodeCharts.Katakana |
                   UpperMidCodeCharts.KatakanaPhoneticExtensions |
                   UpperMidCodeCharts.LatinExtendedD |
                   UpperMidCodeCharts.CjkUnifiedIdeographs |
                   UpperMidCodeCharts.CyrillicExtendedA |
                   UpperMidCodeCharts.HangulCompatibilityJamo,
                   UpperCodeCharts.None);
                string encodedXmlnode = AntiXssEncoder.XmlEncode(xmlnode);
                encodedXmlnode = encodedXmlnode.Replace("&apos;", "'");
                encodedXmlnode = encodedXmlnode.Replace("&lt;", "<");
                encodedXmlnode = encodedXmlnode.Replace("&gt;", ">");
                encodedXmlnode = encodedXmlnode.Replace("&#27;", "");
                encodedXmlnode = encodedXmlnode.Replace("&amp;", "&");
                encodedXmlnode = encodedXmlnode.Replace("&#10;", "");
                encodedXmlnode = encodedXmlnode.Replace("&#13;", "");
                encodedXmlnode = encodedXmlnode.Replace("&#11;", "");
                //string encodedXmlnode = xmlnode;

                if (encodedXmlnode.Contains("<script") && encodedXmlnode.Contains("</script>"))
                {
                    encodedXmlnode = encodedXmlnode.Replace("<script", "&lt;script");
                    encodedXmlnode = encodedXmlnode.Replace("</script>", "&lt;/script&gt;");
                }
                //end added

                string[] xmlnod = new string[7];
                //xmlnod = xmlnode.Split('`');
                xmlnod = encodedXmlnode.Split('`');

                if (xmlnod[0] == "parent")
                {
                    if (!isParentExists)
                    {
                        isParentExists = true;
                        writer.WriteStartElement("ParentMessage");
                        createNode(xmlnod[1], xmlnod[2], xmlnod[3], xmlnod[4], xmlnod[5], xmlnod[6], groupName.Replace("/", " "), writer);
                    }
                }
                else
                {
                    if (!isReplyTagAdded)
                    {
                        writer.WriteStartElement("ReplyMessage");
                        isReplyTagAdded = true;
                    }
                    writer.WriteStartElement("Message");
                    createNode(xmlnod[1], xmlnod[2], xmlnod[3], xmlnod[4], xmlnod[5], xmlnod[6], "", writer);
                    writer.WriteEndElement();
                }
            }
            if (isParentExists)
            {
                writer.WriteEndElement();
            }
            if (isReplyTagAdded)
            {
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }
   
        public static void CreateHTMLFromXMLFile(string folderName, string xmlFullName, string htmlFullName, string threadname)
        {
            htmlFullName = RemoveUnsupportedFileNameChars(htmlFullName);

            if (folderName.Length > 65) // SharePoint folder lenght limit
                folderName = folderName.Substring(0, 65);
            string folderNameedit = string.Concat(YammerdirPath, "\\", RemoveUnsupportedFolderNameChars(folderName));

            folderNameedit = string.Concat(folderNameedit, "\\", threadname);
            if (!Directory.Exists(folderNameedit))
                Directory.CreateDirectory(folderNameedit);
            htmlFullName = string.Concat(folderNameedit, "\\", htmlFullName);
            if (File.Exists(htmlFullName))
                File.Delete(htmlFullName);
            XslCompiledTransform myXslTransform;
            myXslTransform = new XslCompiledTransform();
            myXslTransform.Load(AppDomain.CurrentDomain.BaseDirectory + @"XMLHelper\ThreadTemplate.xslt");
            myXslTransform.Transform(xmlFullName, htmlFullName);
        }
        private static void createNode(string messageID, string senderName, string senderEmail, string Timestamp, string Body, string attachment, string groupName, XmlTextWriter writer)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                writer.WriteStartElement("Message_id");
                writer.WriteString(messageID);
                writer.WriteEndElement();
                writer.WriteStartElement("Sender_fullname");
                writer.WriteString(senderName.Replace("'", "''"));
                writer.WriteEndElement();
                writer.WriteStartElement("Sender_email");
                writer.WriteString(senderEmail.Replace("'", "''"));
                writer.WriteEndElement();
                writer.WriteStartElement("Timestamp");
                writer.WriteString(Timestamp);
                writer.WriteEndElement();
                writer.WriteStartElement("Body");
                writer.WriteString(Body);
                writer.WriteEndElement();
                writer.WriteStartElement("attachment");
                writer.WriteString(attachment);
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartElement("Group_Name");
                writer.WriteString(groupName);
                writer.WriteEndElement();
                writer.WriteStartElement("PMessage_id");
                writer.WriteString(messageID);
                writer.WriteEndElement();
                writer.WriteStartElement("PSender_fullname");
                writer.WriteString(senderName.Replace("'", "''"));
                writer.WriteEndElement();
                writer.WriteStartElement("PSender_email");
                writer.WriteString(senderEmail.Replace("'", "''"));
                writer.WriteEndElement();
                writer.WriteStartElement("PTimestamp");
                writer.WriteString(Timestamp);
                writer.WriteEndElement();
                writer.WriteStartElement("PBody");
                writer.WriteString(Body);
                writer.WriteEndElement();
                writer.WriteStartElement("Pattachment");
                writer.WriteString(attachment);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// update YM_ExportDetails table with processing status.
        /// </summary>
        /// <param name="prevStatus"></param>
        /// <param name="newStatus"></param>
        static void updateStatus(string prevStatus, string newStatus)
        {

            try
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    yeticontext.Database.CommandTimeout = 0;
                    yeticontext.Yammer_UpdateProcessingStatus(newStatus, prevStatus, Environment.MachineName);
                }
                
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("deadlocked"))
                {
                    Thread.Sleep(3000);
                    updateStatus(prevStatus, newStatus);
                }
            }
        }
        /// <summary>
        /// upate status of YM_Messages to track the status.
        /// </summary>
        /// <param name="prevStatus"></param>
        /// <param name="newStatus"></param>
        /// <param name="thread_ids"></param>       

        static void updateThreadStatus(string prevStatus, string newStatus, DataTable thread_ids)
        {
            //using (YETIDBEntities yeticontext = new YETIDBEntities())
            //{
            //    yeticontext.Database.CommandTimeout = 0;
            //    yeticontext.Yammer_UpdateThreadStatus(newStatus, prevStatus, Environment.MachineName);
            //}
            using (SqlConnection con = new SqlConnection(conn))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_UpdateThreadStatus";
                cmd.Parameters.Add("in_nvarchar_newStatus", SqlDbType.NVarChar).Value = newStatus;
                cmd.Parameters.Add("in_nvarchar_prevStatus", SqlDbType.NVarChar).Value = prevStatus;
                cmd.Parameters.Add("processedBy", SqlDbType.NVarChar).Value = Environment.MachineName;
                cmd.Parameters.Add("in_datatable_ThreadIds", SqlDbType.Structured).Value = thread_ids;
                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

        //static void updateFileDownloadStatus(string newStatus, string prevStatus, StringBuilder thread_ids)
        //{
        //    if (thread_ids.ToString().StartsWith(","))
        //    {
        //        using (SqlConnection con = new SqlConnection(conn))
        //        {
        //            con.Open();
        //            SqlCommand cmd = new SqlCommand();
        //            cmd.Connection = con;
        //            cmd.CommandTimeout = 0;
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.CommandText = "Yammer_DownloadThreadStatus";
        //            cmd.Parameters.AddWithValue("NEW_STATUS", newStatus);
        //            cmd.Parameters.AddWithValue("PREV_STATUS", prevStatus);
        //            cmd.Parameters.AddWithValue("THREAD_IDS", thread_ids.ToString().Remove(0, 1));
        //            cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
        //            cmd.ExecuteNonQuery();
        //            con.Close();
        //        }
        //    }
        //}

        static void updateFileDownloadStatus(string newStatus, string prevStatus, DataTable thread_ids)
        {

            using (SqlConnection con = new SqlConnection(conn))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_DownloadThreadStatus";
                cmd.Parameters.AddWithValue("NEW_STATUS", newStatus);
                cmd.Parameters.AddWithValue("PREV_STATUS", prevStatus);
                cmd.Parameters.AddWithValue("THREAD_IDS", thread_ids);
                cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                cmd.ExecuteNonQuery();
                con.Close();
            }

        }
        /// <summary>
        /// log information and errors during processing
        /// </summary>
        /// <param name="evenType"></param>
        /// <param name="fileName"></param>
        /// <param name="errorDescription"></param>
        public static void LogEvents(string evenType, string fileName, string errorDescription)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Database.CommandTimeout = 0;
                yeticontext.Yammer_Common_LogEvent("YM_Processing",evenType.ToString(),fileName,errorDescription,Environment.MachineName);
            }
           
        }

        private static string RemoveUnsupportedFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars().Concat(new char[] { '?', '*', ':' }).  // Amazingly GetInvalidFileNameChars() fails to include a few characters not allowed in Windows file systems
                Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private static string RemoveUnsupportedFolderNameChars(string folderName)
        {
            return Path.GetInvalidPathChars().Concat(new char[] { '?', '*', ':', '\\', '%' }).  // Amazingly GetInvalidPathChars() fails to include a few characters not allowed in Windows file systems
                Aggregate(folderName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }
}
