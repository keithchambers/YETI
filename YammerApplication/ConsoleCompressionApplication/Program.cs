// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Ionic.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YammerLibrary;

namespace ConsoleCompressionApplication
{
    class Program
    {
        static string uploadStartDate = string.Empty;
        static string uploadEndDate = string.Empty;
        static string Year = string.Empty;
        private static bool RangeInDays = (Convert.ToInt32(ConfigurationManager.AppSettings["rangeInMonths"]) == 0) ? true : false;
        private static string conn = string.Empty;
        private static string YammerCmpPath = string.Empty;
        //private static string restToken = string.Empty;
        static void Main(string[] args)
        {

            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
            //PafHelper obj = new PafHelper(); // to get all the config from paf
            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerCmpPath"];
            YammerCmpPath = await getSecretCmdlet.GetSecretAsync();

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                Year = yeticontext.Yammer_YearCompressingRequest(Environment.MachineName, RangeInDays).ToString();
            }



           

            LogEvents("Information", "FileCompress", "Got year -" + Year + " for compression");
            if (!string.IsNullOrEmpty(Year))
            {

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    List<Yammer_Compress_GetProcessingCount_Result> Result = yeticontext.Yammer_Compress_GetProcessingCount(Convert.ToInt32(Year), Convert.ToInt32(ConfigurationManager.AppSettings["RangeInMonths"]), Convert.ToInt32(ConfigurationManager.AppSettings["RangeInDays"])).ToList();

                    
                        if (Result.Count > 0)
                        {
                            if (string.IsNullOrEmpty(Convert.ToString(Result[0].StartDate)))
                            {
                                LogEvents("Information", "FileCompress", "No data available compress");
                                Environment.Exit(0);
                            }
                            uploadStartDate = Result[0].StartDate.ToString();
                            uploadEndDate = Result[0].EndDate.ToString();
                            LogEvents("Information", "FileCompress", "All generated");
                            if (ZipFolders()) //Compress folders
                            {
                                //int folderCount = 1;
                                string folderName = string.Empty;

                            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["SPdirPath"];
                            string SPDir = await getSecretCmdlet.GetSecretAsync();
                            SPDir = Path.Combine(SPDir, Year);
                                if (!Directory.Exists(SPDir))
                                    Directory.CreateDirectory(SPDir);
                                //else
                                //{
                                //folderCount = Directory.GetDirectories(SPDir, "*.*", SearchOption.TopDirectoryOnly).Length + 1;
                                folderName = SharePointClassLibrary.CommonHelper.FrameFolderName(uploadStartDate, uploadEndDate, true);
                                //}
                                string SPNewDir = Path.Combine(SPDir, folderName.ToString());
                                if (!Directory.Exists(SPNewDir))
                                    Directory.CreateDirectory(SPNewDir);
                                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                                {

                                // Fetch  RobocopyMovecommandTemplate and Yammer Compression patch secrets from Keyvault
                                getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["RobocopyMoveCommandTemplate"];
                                string RobocopyMoveCommandTemplate = await getSecretCmdlet.GetSecretAsync();

                               


                                process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.CreateNoWindow = true;
                                    process.StartInfo.RedirectStandardOutput = false;
                                    process.StartInfo.FileName = "ROBOCOPY";
                                    process.StartInfo.Arguments = string.Format(System.Globalization.CultureInfo.InvariantCulture, RobocopyMoveCommandTemplate, Path.Combine(YammerCmpPath, Year), SPNewDir);
                                    process.Start();
                                    process.WaitForExit(2400 * 60 * 1000);
                                    if (process.HasExited)
                                    {
                                        if (process.ExitCode > 8)
                                        {
                                            LogEvents("Error", "RoboCopyLog", " Compressed folders not moved to SharePoint Directory");
                                            Environment.Exit(0);
                                        }
                                    }
                                }
                                UpdateStatus(uploadStartDate, uploadEndDate, Year);
                                LoadSPDirectoryMapping(uploadStartDate, uploadEndDate, SPNewDir);
                                UpdateYearStatus(Year);
                            }
                        }

                    
                    Year = string.Empty;

                }
            }
            LogEvents("Information", "FileCompress", "Compression method completed");
        }

        private static void LoadSPDirectoryMapping(string uploadStartDate, string uploadEndDate, string folderPath)
        {


            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_LoadSPDirectoryMapping(uploadStartDate, uploadEndDate, folderPath, Directory.EnumerateFiles(folderPath, "*.zip", SearchOption.AllDirectories).Count(), Environment.MachineName);
            }


            
        }

        private static void UpdateYearStatus(string Year)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Update_YearStatus(Year,0,0,1,0);
            }

            
        }
        static void UpdateStatus(string uploadStartDate, string uploadEndDate, string Year)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Common_UpdateStatus(uploadStartDate, uploadEndDate, "FilesCompressed", "CompressionCompleted", null , null);
            }

            
        }
        static bool ZipFolders()
        {
            LogEvents("Information", "FileCompress", "Zipping folders started");
            bool returnValue = true;
            try
            {
                LogEvents("Information", "FileCompress", "Getting directories started ");
                string[] folders = Directory.GetDirectories(Path.Combine(YammerCmpPath, Year));
                LogEvents("Information", "FileCompress", "Getting directories completed ");
                foreach (string folder in folders)
                {
                    LogEvents("Information", "FileCompress", "Zipping  " + folder + " started");
                    string[] subfolders = Directory.GetDirectories(folder);
                    foreach (string subfolder in subfolders)
                    {
                        try
                        {
                            string threadId = subfolder.Substring(subfolder.LastIndexOf("\\") + 1);
                            string groupInfo = folder.Substring(folder.LastIndexOf("\\") + 1);
                            if (!File.Exists(subfolder + ".zip"))
                            {
                                System.IO.Compression.ZipFile.CreateFromDirectory(subfolder, subfolder + ".zip");
                            }
                            else
                            {
                                Thread.Sleep(300);
                                File.Delete(subfolder + ".zip");
                                System.IO.Compression.ZipFile.CreateFromDirectory(subfolder, subfolder + ".zip");
                            }
                            FileInfo f = new FileInfo(subfolder + ".zip");

                            if (File.Exists(subfolder + ".zip"))
                            {
                                if (f.Length > 2147483648)
                                {
                                    File.Delete(subfolder + ".zip");
                                    SplitZipFolder(subfolder, folder, threadId);
                                }

                            }
                            List<string> zipList = Directory.EnumerateFiles(folder, threadId + "*").ToList();
                            if (zipList.Count > 0)
                            {
                                for (int i = 0; i < zipList.Count; i++)
                                {
                                    using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zipList[i]))
                                    {
                                        foreach (ZipArchiveEntry entry in archive.Entries)
                                        {
                                            try
                                            {
                                                var stream = entry.Open(); //If files can be read, then zip file is not corrupted
                                            }
                                            catch (Exception ex)
                                            {
                                                LogEvents("Error", subfolder + ".zip", ex.ToString());
                                                returnValue = false;
                                            }
                                        }
                                    }
                                }
                                Directory.Delete(subfolder, true);
                            }

                        }
                        catch (Exception ex)
                        {
                            LogEvents("Error", subfolder + ".zip", ex.ToString());
                            returnValue = false;
                        }
                    }
                    LogEvents("Information", "FileCompress", "Zipping  " + folder + " completed");
                }
                return returnValue;
            }
            catch (Exception ex)
            {
                LogEvents("Error", "FileCompress", ex.ToString());
                return false;
            }
        }
        public static void LogEvents(string evenType, string fileName, string errorDescription)
        {


            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Common_LogEvent("YM_Compressing", evenType.ToString(), fileName, errorDescription, Environment.MachineName);
            }
            
        }
        /// <summary>
        /// The size of Package(100MB).
        /// </summary>
        public static long m_packageSize = 1024 * 1024 * 100;

        /// <summary>
        /// This method generate the package as per the <c>PackageSize</c> declare.
        /// Currently <c>PackageSize</c> is 2 GB.
        /// </summary>
        /// <param name="inputFolderPath">Input folder Path.</param>
        /// <param name="outputFolderandFile">Output folder Path.</param>

        static void SplitZipFolder(string inputFolderPath, string outputFolderandFile, string threadId)
        {
            #region otherApproach
        
            #endregion

            int cnt = 1;
            m_packageSize = m_packageSize * 20;
            ArrayList htmlFile = new ArrayList();
            ArrayList ar = GenerateFileList(inputFolderPath, out htmlFile); // generate file list          

        


            // Output Zip file name.
            string outPath = Path.Combine(outputFolderandFile, threadId + "-" + cnt + ".zip");
            long HTMLstreamlenght = 0;

            using (ZipOutputStream oZipStream = CreateNewStream(htmlFile, outPath, out HTMLstreamlenght))
            {

                // Initialize the zip entry object.
                ZipEntry oZipEntry = new ZipEntry();

                // numbers of files in file list.
                var counter = ar.Count;
                try
                {
                    using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
                    {
                        Array.Sort(ar.ToArray());  // Sort the file list array.
                        foreach (string Fil in ar.ToArray()) // for each file, generate a zip entry
                        {
                            if (!Fil.EndsWith(@"/")) // if a file ends with '/' its a directory
                            {

                                if (!zip.ContainsEntry(Path.GetFileName(Fil.ToString())))
                                {
                                    oZipEntry = zip.AddEntry("Attachments/" + Path.GetFileName(Fil.ToString()), Fil);
                                    counter--;
                                    try
                                    {
                                        if (counter >= 0)
                                        {
                                            long streamlenght = HTMLstreamlenght;
                                            const int BufferSize = 4096;
                                            byte[] obuffer = new byte[BufferSize];
                                            if (oZipStream.Position + streamlenght < m_packageSize)
                                            {
                                                using (FileStream ostream = File.OpenRead(Fil))
                                                {
                                                    //ostream = File.OpenRead(Fil);
                                                    using (ZipOutputStream tempZStream = new ZipOutputStream(File.Create(Directory.GetCurrentDirectory() + "\\test.zip"), false))
                                                    {
                                                        tempZStream.PutNextEntry(oZipEntry.FileName);
                                                        int tempread;
                                                        while ((tempread = ostream.Read(obuffer, 0, obuffer.Length)) > 0)
                                                        {
                                                            tempZStream.Write(obuffer, 0, tempread);
                                                        }
                                                        streamlenght = tempZStream.Position;
                                                        tempZStream.Dispose();
                                                        tempZStream.Flush();
                                                        tempZStream.Close(); // close the zip stream.
                                                                             // ostream.Dispose();
                                                    }

                                                    File.Delete(Directory.GetCurrentDirectory() + "\\test.zip");
                                                }
                                            }

                                            if (oZipStream.Position + streamlenght > m_packageSize)
                                            {
                                                zip.RemoveEntry(oZipEntry);
                                                zip.Dispose();
                                                oZipStream.Dispose();
                                                oZipStream.Flush();
                                                oZipStream.Close(); // close the zip stream.
                                                cnt = cnt + 1;
                                                outPath = Path.Combine(outputFolderandFile, threadId + "-" + cnt.ToString() + ".zip"); // create new output zip file when package size.                                           
                                                using (ZipOutputStream oZipStream2 = CreateNewStream(htmlFile, outPath, out HTMLstreamlenght))
                                                {
                                                    oZipStream2.PutNextEntry(oZipEntry.FileName);
                                                    using (FileStream ostream = File.OpenRead(Fil))
                                                    {
                                                        int read;
                                                        while ((read = ostream.Read(obuffer, 0, obuffer.Length)) > 0)
                                                        {
                                                            oZipStream2.Write(obuffer, 0, read);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                oZipStream.PutNextEntry(oZipEntry.FileName);
                                                using (FileStream ostream = File.OpenRead(Fil))
                                                {
                                                    int read;
                                                    while ((read = ostream.Read(obuffer, 0, obuffer.Length)) > 0)
                                                    {
                                                        oZipStream.Write(obuffer, 0, read);
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            Console.WriteLine("No more file existed in directory");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                        zip.RemoveEntry(oZipEntry.FileName);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("File Existed {0} in Zip {1}", Path.GetFullPath(Fil.ToString()), outPath);
                                }
                            }

                        }
                        zip.Dispose();
                    }
                    oZipStream.Dispose();
                    oZipStream.Flush();
                    oZipStream.Close();// close stream
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());

                }
                finally
                {
                    oZipStream.Dispose();
                    oZipStream.Flush();
                    oZipStream.Close();// close stream

                    Console.WriteLine("Remain Files{0}", counter);
                }
            }
        }
        private static ZipOutputStream CreateNewStream(ArrayList htmlFile, string outPath, out long HTMLstreamlenght)
        {
            ZipOutputStream oZipStream = new ZipOutputStream(File.Create(outPath), false);// create zip stream
                                                                                          // Compression level of zip file.

            oZipStream.CompressionLevel = 0;
            const int BufferSize = 4096;
            byte[] obuffer = new byte[BufferSize];
            HTMLstreamlenght = 0;
            //FileStream htmlstream = null;
            ZipEntry oHtmlEntry = new ZipEntry();
            using (Ionic.Zip.ZipFile htmlZip = new Ionic.Zip.ZipFile())
            {
                foreach (string htmlFil in htmlFile.ToArray()) // for each file, generate a zip entry
                {
                    oHtmlEntry = htmlZip.AddEntry(Path.GetFileName(htmlFil.ToString()), htmlFil);
                    oZipStream.PutNextEntry(oHtmlEntry.FileName);
                    using (FileStream htmlstream = File.OpenRead(htmlFil))
                    {
                        int read;
                        while ((read = htmlstream.Read(obuffer, 0, obuffer.Length)) > 0)
                        {
                            oZipStream.Write(obuffer, 0, read);
                        }
                    }
                }
                htmlZip.Dispose();
            }
            HTMLstreamlenght = oZipStream.Position;


            return oZipStream;

        }

        /// <summary>
        /// This method return the list of files from the directory
        /// Also read the child directory also, but not add the 0 length file.
        /// </summary>
        /// <param name="Dir">Name of directory.</param>
        /// <returns>return the list of all files including into subdirectory files </returns>
        private static ArrayList GenerateFileList(string Dir, out ArrayList htmlFiles)
        {
            ArrayList fils = new ArrayList();
            htmlFiles = new ArrayList();

            foreach (string file in Directory.GetFiles(Dir, "*.*", SearchOption.AllDirectories)) // add each file in directory
            {
                FileInfo f = new FileInfo(file);

                if (f.Length > 0)
                {
                    if (f.Extension == ".html")
                        htmlFiles.Add(file);
                    else
                        fils.Add(file);
                }
            }

            return fils; // return file list
        }
    }
}
