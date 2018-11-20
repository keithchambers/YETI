// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Data;
using System.Threading;
using YammerLibrary;

namespace ConsoleDownloadApplication
{
    class Program
    {
        private static string dirPath = string.Empty;
        private static string Year = string.Empty;
        static int timesTried = 0;
        private static string conn = string.Empty;
        private static string RobocopyOneFileCommandTemplate = string.Empty;
        private static string DurationInHrs = string.Empty;

        static void Main(string[] args)
        {

            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
            
            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
            //LogEvents("StartDownload", "Start");

            //PafHelper obj = new PafHelper(); // to get all the config from paf

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["RobocopyOneFileCommandTemplate"];
            RobocopyOneFileCommandTemplate = await getSecretCmdlet.GetSecretAsync();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["DurationInHrs"];
             DurationInHrs = await getSecretCmdlet.GetSecretAsync();



            using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    Year = yeticontext.Yammer_YearDownloadRequest(Environment.MachineName).ToString();
                }


               
                if (!string.IsNullOrEmpty(Year))
                {

                getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["dirPath"];
                dirPath = await getSecretCmdlet.GetSecretAsync();

                dirPath = dirPath + "\\" + Year;

                    await StoppedRequests();

                    await NewRequests();

                    Year = string.Empty;
                }
            
        }
        /// <summary>
        /// If there are less than 6 records are in status 'Stopped' or 'In progress', 
        /// will start the new request for downloading new data
        /// </summary>
        private static async Task NewRequests()
        {
            timesTried = 0;
            await SetDuration();
        }
        /// <summary>
        /// continue to download the data in status 'Stopped' or 'In progress' if time tried is less than configured.
        /// </summary>
        private static async Task StoppedRequests()
        {
            string startDate = string.Empty;
            string endDate = string.Empty;
            try
            {
                DataSet ds = new DataSet();
                var isoDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
                string newStartDate = string.Empty;

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    List<Yammer_Download_GetTimesTried_Result> Result = yeticontext.Yammer_Download_GetTimesTried(Year, Environment.MachineName).ToList();

                    
                    foreach (Yammer_Download_GetTimesTried_Result Listitem in Result)
                    {
                        startDate = Convert.ToDateTime(Listitem.StartDate).ToString(isoDateTimeFormat.SortableDateTimePattern);
                        endDate = Convert.ToDateTime(Listitem.EndDate).ToString(isoDateTimeFormat.SortableDateTimePattern);
                        timesTried = Convert.ToInt32(Listitem.TimesTried);
                        await ExportAPICall(startDate, endDate);
                    }
                }
            }
            catch (Exception ex) when (ex is WebException)
            {
                LogEvents("export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());
            }
        }

        private static async Task SetDuration()
        {
            string startDate = string.Empty;
            string endDate = string.Empty;
            int inProgressCount = 0;
            int countDownCount = 0;
            try
            {
                var isoDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
                string newStartDate = string.Empty;


                
               
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    newStartDate = yeticontext.Yammer_Download_GetLastEnddate(Year).ToString();

                    inProgressCount = Convert.ToInt32(yeticontext.Yammer_Download_GetInProgressCount(Year,Environment.MachineName));
                   
                    countDownCount = 5 - inProgressCount;
                    if (inProgressCount >= 5)
                    {
                        return;
                    }
                    if (!string.IsNullOrEmpty(newStartDate))
                    {
                        DateTime startdate = Convert.ToDateTime(newStartDate);
                        if (startdate.Year.ToString() == Year)
                            newStartDate = startdate.ToString(isoDateTimeFormat.SortableDateTimePattern);
                        else
                        {
                            UpdateYearStatus(Year);
                            return;
                        }
                    }
                    else
                        newStartDate = Year + "-01-01T00:00:00";
                }
                var countdownEvent = new CountdownEvent(countDownCount);
                for (int i = 0; i < countDownCount; i++)
                {
                    startDate = newStartDate;
                    DateTime tempEndDate = Convert.ToDateTime(startDate).AddHours(Convert.ToInt32(DurationInHrs)).AddMilliseconds(-1);
                    endDate = tempEndDate.ToString(isoDateTimeFormat.SortableDateTimePattern);

                    //if (tempEndDate < DateTime.Now.AddHours(-DateTime.Now.Hour).AddMinutes(-DateTime.Now.Minute).AddSeconds(-DateTime.Now.Second))
                    if (tempEndDate < DateTime.Now)
                    {


                        using (YETIDBEntities yeticontext = new YETIDBEntities())
                        {
                          yeticontext.Yammer_Download_NewExportDetails(startDate,endDate, "Not Started", Environment.MachineName);
                        }

                          

                        DateTime startDateD = Convert.ToDateTime(endDate).AddSeconds(1);
                        if (startDateD.Year.ToString() == Year)
                            newStartDate = startDateD.ToString(isoDateTimeFormat.SortableDateTimePattern);
                        else
                            i = countDownCount;

                        //new Thread(delegate ()
                        {
                            await ExportAPICall(startDate, endDate);
                            countdownEvent.Signal();
                        }//).Start();
                        Thread.Sleep(10000); //Giving 10 sec gap to start the download for the assigned start & end date
                    }
                    else
                        return;
                }
                countdownEvent.Wait();
                countdownEvent.Dispose();
            }
            catch (Exception ex) when (ex is WebException)
            {
                LogEvents("export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());
            }
        }
        /// <summary>
        /// call Yammer export API to download date during the period we configured
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static async Task ExportAPICall(string start, string end)
        {
            string startDate = start;
            string endDate = end;
            //string token = PafHelper.YammerConfiguration.exportApiToken;

            //string token = YammerEncryption.Decrypt(HttpUtility.UrlDecode(PafHelper.YammerConfiguration.exportApiToken.Replace("%%", "%")));
            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerExportTokenURL"];
            string token = await getSecretCmdlet.GetSecretAsync();


            DateTime tempEndate = Convert.ToDateTime(endDate);
            string URL = "https://export.yammer.com/api/v1/export?since=" + startDate + "&until=" + endDate + "&access_token=" + token;
            //string URL = "https://export.yammer.com/api/v1/export?since=" + startDate + "&until=" + endDate + "&model=Message&include=csv&access_token=" + token;                                

            try
            {
                if (timesTried > Convert.ToInt32(ConfigurationManager.AppSettings["MaxAllowableRetries"]))
                {
                    UpdateStatus(startDate, endDate, "Failed", "DownloadFailed", timesTried);
                    SplitDownloadPeriod(startDate, endDate);
                    return;
                }
                else
                {
                    bool deleteCorrupted = false;
                    UpdateStatus(startDate, endDate, "In Progress", "", timesTried);

                    if (!Directory.Exists(dirPath + "\\" + tempEndate.Month))
                        Directory.CreateDirectory(dirPath + "\\" + tempEndate.Month);

                    Thread.Sleep(5000);
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"DownloadFile"))
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"DownloadFile");
                    string tempPath = AppDomain.CurrentDomain.BaseDirectory + @"DownloadFile\\export-" + endDate.Replace(':', '-') + ".zip";
                    using (WebClient wc = new WebClient())
                    {
                        wc.Proxy = null;
                        wc.DownloadFile(URL, tempPath);
                        wc.Dispose();
                    }
                    Thread.Sleep(5000);
                    if (File.Exists(tempPath))
                    {
                        if (!Directory.Exists(ConfigurationManager.AppSettings["logpath"] + "\\RobocopyLogs"))
                            Directory.CreateDirectory(ConfigurationManager.AppSettings["logpath"] + "\\RobocopyLogs");

                        
                        getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["LogPath"];
                        string logPath = await getSecretCmdlet.GetSecretAsync();
                        string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", logPath + "\\RobocopyLogs", "FileCopyProcessing", Year + "_" + tempEndate.Month, DateTime.Now.Ticks);
                        using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                        {




                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.RedirectStandardOutput = false;
                            process.StartInfo.FileName = "ROBOCOPY";
                            process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, RobocopyOneFileCommandTemplate, AppDomain.CurrentDomain.BaseDirectory + @"DownloadFile", dirPath + "\\" + tempEndate.Month, "export-" + endDate.Replace(':', '-') + ".zip", logName);
                            process.Start();
                            process.WaitForExit(2400 * 60 * 1000);
                            if (process.HasExited)
                            {
                                if (process.ExitCode <= 8)
                                {
                                    File.Delete(logName);
                                }
                                else
                                {
                                    LogEvents("RoboCopyLog", "check this path: " + logName);
                                    Environment.Exit(0);
                                }
                            }
                        }
                        Thread.Sleep(5000);
                        if (File.Exists(dirPath + "\\" + tempEndate.Month + "\\export-" + endDate.Replace(':', '-') + ".zip"))
                            File.Delete(tempPath);
                    }
                    Thread.Sleep(5000);
                    using (ZipArchive archive = ZipFile.OpenRead(dirPath + "\\" + tempEndate.Month + "\\export-" + endDate.Replace(':', '-') + ".zip"))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            try
                            {
                                var stream = entry.Open(); //If files can be read, then zip file is not corrupted
                            }
                            catch (Exception ex)
                            {
                                LogEvents("export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());
                                UpdateStatus(startDate, endDate, "Stopped", "DownloadFailed", timesTried + 1);
                                deleteCorrupted = true;
                            }
                        }
                    }
                    if (deleteCorrupted)
                    {
                        File.Delete(dirPath + "\\" + tempEndate.Month + "\\export-" + endDate.Replace(':', '-') + ".zip");
                        deleteCorrupted = false;
                        LogEvents("export - " + endDate.Replace(':', '-') + ".zip", " CorruptedRemoved " + dirPath + "\\" + tempEndate.Month + "\\export-" + endDate.Replace(':', '-') + ".zip");
                        return;
                    }
                    UpdateStatus(startDate, endDate, "Completed", "DownloadCompleted", timesTried + 1);
                }

            }
            catch (Exception ex) when (ex is WebException)
            {
                if (ex.Message == "The remote server returned an error: (429)." || ex.Message == "The operation has timed out")
                {
                    UpdateStatus(startDate, endDate, "Stopped", "DownloadFailed", timesTried);
                }
                else
                {
                    UpdateStatus(startDate, endDate, "Stopped", "DownloadFailed", timesTried + 1);
                }

                LogEvents("export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());

                if (File.Exists(dirPath + "\\" + tempEndate.Month + "\\export-" + endDate.Replace(':', '-') + ".zip"))
                    File.Delete(dirPath + "\\" + tempEndate.Month + "\\export-" + endDate.Replace(':', '-') + ".zip");
            }
        }
        /// <summary>
        /// Log the job information and error
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="errorDescription"></param>
        public static void LogEvents(string fileName, string errorDescription)
        {
            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Common_LogEvent("YM_Download", "Error",fileName,errorDescription,Environment.MachineName);
            }


        }
        /// <summary>
        /// if one period file is downloaded failed in configured times, 
        /// the time duration will be split to 1 hr and the day’s data will be in 24 parts
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        public static void SplitDownloadPeriod(string startDate, string endDate)
        {
            string newEndDate = string.Empty;
            var isoDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            DateTime newstartdate = Convert.ToDateTime(startDate);
            TimeSpan diff = Convert.ToDateTime(endDate).AddSeconds(1) - Convert.ToDateTime(startDate);

            if (diff.Minutes == 5)
                return;
            else if(diff.Hours != 1)
            {

             
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_Download_DeleteExportDetails(startDate, endDate);

                



                    for (int i = 0; i < Convert.ToInt32(DurationInHrs); i++)
                    {

                        DateTime tempEndDate = Convert.ToDateTime(newstartdate).AddHours(1).AddMilliseconds(-1);
                        newEndDate = tempEndDate.ToString(isoDateTimeFormat.SortableDateTimePattern);

                        
                            yeticontext.Yammer_Download_NewExportDetails(newstartdate.ToString(), newEndDate, "Stopped", Environment.MachineName);

                        
                            newstartdate = Convert.ToDateTime(newstartdate).AddHours(1);
                        
                    }
                    
                }
            }
            else if(diff.Hours == 1)
            {
                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {

                    yeticontext.Yammer_Download_DeleteExportDetails(startDate, endDate);
                    
                    for (int i = 0; i < 12; i++) //splitting 1 hr data for 5 min so 12 parts
                    {

                        DateTime tempEndDate = Convert.ToDateTime(newstartdate).AddMinutes(5).AddMilliseconds(-1);
                        newEndDate = tempEndDate.ToString(isoDateTimeFormat.SortableDateTimePattern);


                        
                            yeticontext.Yammer_Download_NewExportDetails(newstartdate.ToString(), newEndDate, "Stopped", Environment.MachineName);



                            newstartdate = Convert.ToDateTime(newstartdate).AddMinutes(5);
                    }
                }
            }

        }        
     
        private static void UpdateStatus(string startDateTime,string endDateTime,string status,string stage, int timesTried,bool toMove = false)
        {
            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Common_UpdateStatus(startDateTime, endDateTime,status,stage,timesTried,toMove);
            }
        }
        static void UpdateYearStatus(string Year)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Update_DownloadCompletion(Year);
            }

           
        }    
           
    }
}
