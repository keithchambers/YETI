// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Configuration;
using Microsoft.SharePoint.Client;
using System.Text.RegularExpressions;
using System.Security;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Globalization;

namespace SharePointClassLibrary
{
    public class SPFileInfo
    {
        public string Filepath
        {
            get;
            set;
        }
        public string Filename
        {
            get;
            set;
        }
        public long filesize
        {
            get;
            set;
        }
        public DateTime filecreateddate
        {
            get;
            set;
        }
    }


    public class AzureSharePointHelper
    {
        private static string YammerdirPath = ConfigurationManager.AppSettings["YammerdirPath"];
        private static string LogPath = ConfigurationManager.AppSettings["LogPath"];
        private static int fileCount = 0;
        private static List<string> filePaths = new List<string>();
        private static List<FileCollection> SPFileCollection = new List<FileCollection>();
        public static List<string> ListBlobContainer(string containerName)
        {
            List<string> groupList = new List<string>();
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName + "-file");

            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null,true,BlobListingDetails.None))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    groupList.Add(blob.Name);
                }
            }
            return groupList;
        }
        public static bool DownloadAndProcessErrorLog(string containerName, string fileName)
        {
            // Setup the connection to Windows Azure Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                 CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName + "-package");

           CloudBlob blob = container.GetBlobReference(fileName);

            if (!Directory.Exists(Path.Combine(LogPath, containerName)))
                Directory.CreateDirectory(Path.Combine(LogPath, containerName));
            //copy blob from cloud to local gallery
            blob.DownloadToFile(LogPath + "\\" + containerName + "\\" + fileName, FileMode.Create);
            DirectoryInfo ydi = new DirectoryInfo(Path.Combine(LogPath, containerName));
            FileInfo[] fileList = ydi.GetFiles("*.err");

            bool isError = false;
            foreach (FileInfo fi in fileList)
            {
                string whole_file = System.IO.File.ReadAllText(fi.FullName);
                string[] lines = whole_file.Split(new string[] { "]\r\n[" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Count(); i++)
                {
                    string errorLine = lines[i];
                    if ((errorLine.Contains("[Folder]") && errorLine.Contains("The given key was not present in the dictionary.")) || (errorLine.Contains("[ListItem]") && errorLine.Contains("already exists")) || errorLine.Contains("already exists"))
                        isError = false;
                    else
                    {
                        isError = true;
                        break;
                    }
                }
            }
            if(!isError)
            {
                System.IO.File.Delete(LogPath + "\\" + containerName + "\\" + fileName);
                Directory.EnumerateFiles(LogPath + "\\" + containerName, "*.*", SearchOption.AllDirectories).ToList().ForEach(System.IO.File.Delete);
                Directory.Delete(LogPath + "\\" + containerName);
            }
            return isError;
        }

        public static int GetSubFolders(string libraryName, string folderName, string SPDirPath, int spyear, out List<SPFileInfo> SPFiles)
        {
            SPDirPath = SPDirPath + "\\" + folderName.Replace("/", "\\");

            SPFiles = new List<SPFileInfo>();
            using (ClientContext context = GetContextObject())
            {
                Web web = context.Web;
                context.Load(web, website => website.ServerRelativeUrl);
                context.ExecuteQuery();
                // Console.WriteLine("web loaded");
                var docLibs = context.LoadQuery(web.Lists.Where(l => l.BaseTemplate == 101));
                context.ExecuteQuery();
                // Console.WriteLine("doclibs loaded");
                {
                    FolderCollection folderList;
                    if (string.IsNullOrEmpty(folderName))
                        folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName).Folders;
                    else
                        folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Folders;
                    context.Load(folderList);
                    context.ExecuteQuery();
                    var folders = folderList.ToList();
                    //Console.WriteLine("group type null started");
                    foreach (Folder folder in folders)
                    {
                        if (folder.Name == "Forms")
                            continue;
                        if (!folder.Files.AreItemsAvailable)
                        {
                            if (string.IsNullOrEmpty(folderName))
                                GetFileCollection(web, context, libraryName, folder.Name, SPDirPath, spyear, ref SPFiles);
                            else
                                GetFileCollection(web, context, libraryName, folderName + "/" + folder.Name, SPDirPath, spyear, ref SPFiles);
                        }
                        //Console.WriteLine(folder.Name + "---- File count " + folder.ItemCount);
                    }
                }
            }

            return fileCount;
        }

        public static void GetFileCollection(Web web, ClientContext context, string libraryName, string folderName, string SPDirPath, int spyear, ref List<SPFileInfo> SPFiles)
        {

            //Console.WriteLine("Getfolders1");
            FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Folders;
            context.Load(folderList);
            context.ExecuteQuery();
            var folders = folderList.ToList();
            if (folders.Count > 0)
            {
                //Console.WriteLine("Getfolders1");
                foreach (Folder folder in folders)
                {
                    if (folder.Name == "Forms")
                        continue;
                    if (!folder.Files.AreItemsAvailable)
                    {
                        GetFileCollection(web, context, libraryName, folderName + "/" + folder.Name, SPDirPath, spyear, ref SPFiles);
                    }
                }
            }
            else
            {
                
                FileCollection fileList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Files;

                context.Load(fileList);
                context.ExecuteQuery();
                var files = fileList.ToList();
                fileCount = fileCount + files.Count();

                //Create a SPFileInfo object;

                foreach (Microsoft.SharePoint.Client.File file in fileList)
                {
                    SPFileInfo SpFile = new SPFileInfo();
                    SpFile.Filename = file.Name;
                    SpFile.filesize = file.Length;
                    SpFile.filecreateddate = file.TimeCreated;


                    string[] split = folderName.Split('/');
                    string firstPart = string.Join("/", split.Take(split.Length - 1));
                    string lastPart = split.Last();

                    int folderId = 0;
                    Int32.TryParse(lastPart, out folderId);
                    string groupFolderName = string.Empty;
                    if (folderId > 0)
                    {
                        groupFolderName = split.Take(split.Length - 1).ToList()[0].ToString();
                        if (groupFolderName == "Private_Conversations" && spyear < 2014)
                            groupFolderName = folderName.Replace("/", "\\");
                    }
                    else
                    {
                        groupFolderName = lastPart;
                    }
                    //folderName.Substring(folderName.LastIndexOf("/") + 1);
                    //Console.WriteLine(groupFolderName + "---- File Name " + file.Name);
                    string filePath = Path.Combine(folderName, file.Name);
                    //Console.WriteLine(filePath);
                    SpFile.Filepath = folderName;
                    SPFiles.Add(SpFile);
                    //filePaths.Add(filePath);                    
                }

            }
        }








        //1 for find and deleted
        //2 for not found
        //-1 for error
        public static int DeleteExistingThreads(string path, string docLib, string groupName, string threadId)
        {
            bool res = false;
            int intRes = -1;
            List<string> SPdirectoryList = SharePointClassLibrary.AzureSharePointHelper.GetFolderListFromSharePoint(path);

            
            if (SPdirectoryList.Count > 0)
            {
                if (SPdirectoryList.Contains(groupName))
                {
                    res = SharePointClassLibrary.AzureSharePointHelper.DeleteFileFromSharePoint(Path.Combine(path.Replace(docLib + "\\", string.Empty), groupName), docLib, string.Concat(threadId, ".zip"));
                    if (res == true)
                        intRes = 1;
                    else
                        intRes = 2;
                    return intRes;
                }
                else
                {

                    foreach (string SPdirecTory in SPdirectoryList)
                    {
                        if (SPdirecTory.Contains(' '))
                            break;

                        intRes = DeleteExistingThreads(Path.Combine(path, SPdirecTory), docLib, groupName, threadId);
                        if (intRes != -1)
                            return intRes;
                    }

                }
            }

            return intRes;
        }
        public static bool GetLogFileFromContainer(string guid, string containerName, out bool isErrorFileExists)
        {
            isErrorFileExists = false;
            string errorfileName = string.Empty;
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName + "-package");

            bool returnValue = false;
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    if (blob.Name.EndsWith(".log") && blob.Name.Contains(guid))
                        returnValue = true;
                    if (blob.Name.EndsWith(".err") && blob.Name.Contains(guid))
                    {
                        errorfileName = blob.Name;
                        isErrorFileExists = true;
                    }
                }
            }
            if (isErrorFileExists)
            {
                isErrorFileExists = DownloadAndProcessErrorLog(containerName, errorfileName);
            }
            return returnValue;
        }

        public static void DeleteBlobContainer(string containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName + "-package");
            container.Delete();
            container = blobClient.GetContainerReference(containerName + "-file");
            container.Delete();
        }

        public static void DeleteQueue(string QueueUri)
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            
            string queueName = QueueUri.Substring(QueueUri.LastIndexOf("/") + 1);
            if (queueName.Contains('?'))
            {
                queueName = queueName.Substring(0, queueName.LastIndexOf("?"));
            }
            // Retrieve reference to a previously created container.
            CloudQueue queue = queueClient.GetQueueReference(queueName);            
            queue.Delete();
        }

        public static void DeleteAllQueue()
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();


            // Retrieve reference to a previously created container.
            IEnumerable<CloudQueue> queueList = queueClient.ListQueues();
            int count = queueList.Count();
            foreach (CloudQueue queue in queueList)
            {
                queue.Delete();
                Console.Write("\r{0} - Queue Remaining to delete   ", count--);
            }
        }
        public static void DeleteAllBlobContainer()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            IEnumerable<CloudBlobContainer> containerList = blobClient.ListContainers();
            int count = containerList.Count();
            foreach (CloudBlobContainer container in containerList)
            {
                container.Delete();
                Console.Write("\r{0} - Container Remaining to delete   ", count--);
            }
        }

        public static void DownloadFileFromSharePoint(string fileName)
        {
            using (ClientContext clientContext = GetContextObject())
            {
                try
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web, website => website.ServerRelativeUrl);
                    clientContext.ExecuteQuery();
                    Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                    string strSiteRelavtiveURL = regex.Replace(Configuration.FileUrl, string.Empty);
                    string strServerRelativeURL = string.Concat(web.ServerRelativeUrl, "/", strSiteRelavtiveURL, "/", fileName);

                    Microsoft.SharePoint.Client.File oFile = web.GetFileByServerRelativeUrl(strServerRelativeURL);
                    clientContext.Load(oFile);
                    ClientResult<Stream> stream = oFile.OpenBinaryStream();
                    clientContext.ExecuteQuery();
                    Stream filestrem = ReadFully(stream.Value);
                    if(!Directory.Exists(System.IO.Path.Combine(YammerdirPath, fileName.Split('/')[0])))
                        Directory.CreateDirectory(System.IO.Path.Combine(YammerdirPath, fileName.Split('/')[0]));
                    string filepath = System.IO.Path.Combine(YammerdirPath, fileName);

                    using (FileStream fileStream = System.IO.File.Create(filepath, (int)filestrem.Length))
                    {
                        byte[] bytesInStream = new byte[filestrem.Length];
                        filestrem.Read(bytesInStream, 0, bytesInStream.Length); 
                        fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    }
                }
                catch(Exception ex)
                {
                    //if (ex.Message == "File Not Found")
                }
            }
        }

        public static void DownloadAllFileFromSharePoint(List<string> groupList)
        {
            using (ClientContext clientContext = GetContextObject())
            {
                ClientResult<Stream> stream = null;
                Web web = clientContext.Web;
                clientContext.Load(web, website => website.ServerRelativeUrl);
                clientContext.ExecuteQuery();
                Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                string strSiteRelavtiveURL = regex.Replace(Configuration.FileUrl, string.Empty);                
                foreach (string fileName in groupList)
                {
                    try
                    {
                        string strServerRelativeURL = string.Concat(web.ServerRelativeUrl, "/", strSiteRelavtiveURL, "/", fileName);

                        Microsoft.SharePoint.Client.File oFile = web.GetFileByServerRelativeUrl(strServerRelativeURL);
                        clientContext.Load(oFile);
                        stream = oFile.OpenBinaryStream();
                        clientContext.ExecuteQuery();
                        Stream filestrem = ReadFully(stream.Value);

                        string filepath = System.IO.Path.Combine(YammerdirPath, fileName);

                        using (FileStream fileStream = System.IO.File.Create(filepath, (int)filestrem.Length))
                        {
                            byte[] bytesInStream = new byte[filestrem.Length];
                            filestrem.Read(bytesInStream, 0, bytesInStream.Length);
                            fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        //if (ex.Message == "File Not Found")
                    }
                }  
            }
        }

        public static bool CheckAllFileUploadedToSharePOint(string containerName)
        {
            bool allFileExists = true;
            using (ClientContext clientContext = GetContextObject())
            {
                ClientResult<Stream> stream = null;
                Web web = clientContext.Web;
                clientContext.Load(web, website => website.ServerRelativeUrl);
                clientContext.ExecuteQuery();
                Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                string strSiteRelavtiveURL = regex.Replace(Configuration.FileUrl, string.Empty);
                List<string> groupList = ListBlobContainer(containerName);
                foreach (string fileName in groupList)
                {
                    try
                    {
                        string strServerRelativeURL = string.Concat(web.ServerRelativeUrl, "/", strSiteRelavtiveURL, "/", fileName);

                        Microsoft.SharePoint.Client.File oFile = web.GetFileByServerRelativeUrl(strServerRelativeURL);
                        clientContext.Load(oFile);                        
                        stream = oFile.OpenBinaryStream();
                        clientContext.ExecuteQuery();                        
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "File Not Found")                        
                            allFileExists = false;
                    }
                }
            }
            
            return allFileExists;
        }

        public static void DeleteFilesFromSharePoint(List<string> Deletefile)
        {
            using (ClientContext clientContext = GetContextObject())
            {
                Web web = clientContext.Web;
                clientContext.Load(web, website => website.ServerRelativeUrl);
                clientContext.ExecuteQuery();
                
                Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                string strSiteRelavtiveURL = regex.Replace(Configuration.FileUrl, string.Empty);
                foreach (string filePath in Deletefile)
                {
                    var fileToDelete = web.GetFileByServerRelativeUrl(web.ServerRelativeUrl + "/" + strSiteRelavtiveURL + "/" + filePath);
                    fileToDelete.DeleteObject();
                    //fileToDelete.Recycle();
                    clientContext.ExecuteQuery();
                }
            }
        }
        public static bool DeleteFileFromSharePoint(string ThreadFolderPath, string TargetDocLib, string DeleteThreadFile)
        {
            try
            {
                bool existAndDeleted = FileExistAndDelete(ThreadFolderPath, TargetDocLib, DeleteThreadFile);
                if (existAndDeleted == false)
                {
                    using (ClientContext clientContext = GetContextObject())
                    {
                        Web web = clientContext.Web;
                        clientContext.Load(web, website => website.ServerRelativeUrl);
                        clientContext.ExecuteQuery();
                        Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                        string documentLibrary = regex.Replace(TargetDocLib, string.Empty);
                        FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + documentLibrary + "/" + ThreadFolderPath).Folders;
                        clientContext.Load(folderList);
                        clientContext.ExecuteQuery();
                        var folders = folderList.ToList();

                        foreach (Folder folder in folders)
                        {
                            string ThreadFolderPath2 = ThreadFolderPath + "\\" + folder.Name;
                            existAndDeleted = FileExistAndDelete(ThreadFolderPath2, TargetDocLib, DeleteThreadFile);
                            if (existAndDeleted == true)
                                break;
                        }

                    }

                }
                return existAndDeleted;
            }
            catch (Exception ex)
            {
                //throw ex;
                return false;
            }
        }
        static public bool FileExistAndDelete(string ThreadFolderPath, string TargetDocLib, string DeleteThreadFile)
        {
            try
            {
                using (ClientContext clientContext = GetContextObject())
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web, website => website.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                    string documentLibrary = regex.Replace(TargetDocLib, string.Empty);
                    var fileToDelete = web.GetFileByServerRelativeUrl(web.ServerRelativeUrl + "/" + documentLibrary + "/" + ThreadFolderPath + "/" + DeleteThreadFile);
                    clientContext.Load(fileToDelete);
                    clientContext.ExecuteQuery();


                    fileToDelete.DeleteObject();
                    fileToDelete.Recycle();
                    clientContext.ExecuteQuery();

                }
               
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("File Not Found"))
                    return false;
                else
                {
                    return false;
                    throw ex;
                }
            }
        }


        static public bool CheckIfFileExists(string ThreadFolderPath, string TargetDocLib, string DeleteThreadFile, long filesize)
        {
            bool FileExists = false;
            try
            {
                using (ClientContext clientContext = GetContextObject())
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web, website => website.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                    string documentLibrary = regex.Replace(TargetDocLib, string.Empty);
                    var fileToCheck = web.GetFileByServerRelativeUrl(web.ServerRelativeUrl + "/" + documentLibrary + "/" + ThreadFolderPath + "/" + DeleteThreadFile);
                    clientContext.Load(fileToCheck);
                    clientContext.ExecuteQuery();


                 
                 
                    FileExists = fileToCheck.Exists && (fileToCheck.Length == filesize);
                           
                    

                }

                return FileExists;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("File Not Found"))
                    return false;
                else
                {
                    throw ex;
                }
            }
        }


        public static bool DeleteFileFromSharePoint(string ThreadFolderPath, string DeleteThreadFile)
        {
            try
            {
                using (ClientContext clientContext = GetContextObject())
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web, website => website.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                    string documentLibrary = regex.Replace(Configuration.FileUrl, string.Empty);
                    var fileToDelete = web.GetFileByServerRelativeUrl(web.ServerRelativeUrl + "/" + documentLibrary + "/" + ThreadFolderPath + "/" + DeleteThreadFile);
                    fileToDelete.DeleteObject();
                    fileToDelete.Recycle();
                    clientContext.ExecuteQuery();
                    FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + documentLibrary + "/" + ThreadFolderPath).Folders;
                    clientContext.Load(folderList);
                    clientContext.ExecuteQuery();
                    var folders = folderList.ToList();
                    foreach (Folder folder in folders)
                    {
                        fileToDelete = web.GetFileByServerRelativeUrl(web.ServerRelativeUrl + "/" + documentLibrary + "/" + ThreadFolderPath + "/" + folder.Name + "/" + DeleteThreadFile);
                        fileToDelete.DeleteObject();
                        fileToDelete.Recycle();
                        clientContext.ExecuteQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool DeleteAllFileFromSharePoint(string libraryName, string ThreadFolderPath)
        {
            try
            {
                using (ClientContext clientContext = GetContextObject())
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web, website => website.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                    string documentLibrary = regex.Replace(Configuration.FileUrl, string.Empty);
                    
                    FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + ThreadFolderPath).Folders;
                    clientContext.Load(folderList);
                    clientContext.ExecuteQuery();
                    var folders = folderList.ToList();
                    foreach (Microsoft.SharePoint.Client.Folder folderDelete in folders)
                    {
                        folderDelete.DeleteObject();
                        folderDelete.Recycle();
                        clientContext.ExecuteQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public static bool CheckFileUploadedToSharePOint(string libraryName, string folderName, string FileName)
        {
            bool isFileExists = true;
            using (ClientContext context = GetContextObject())
            {
                ClientResult<Stream> stream = null;
                Web web = context.Web;
                context.Load(web, website => website.ServerRelativeUrl);
                context.ExecuteQuery();

                var docLibs = context.LoadQuery(web.Lists.Where(l => l.BaseTemplate == 101));
                context.ExecuteQuery();

                try
                {
                    string strServerRelativeURL = string.Concat(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName, "/", FileName);

                    Microsoft.SharePoint.Client.File oFile = web.GetFileByServerRelativeUrl(strServerRelativeURL);
                    context.Load(oFile);
                    stream = oFile.OpenBinaryStream();
                    context.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("File Not Found"))
                        isFileExists = false;
                }
            }
            return isFileExists;
        }
        public static int GetThreadCount(string libraryName, string folderName, string SPDirPath,int spyear, out List<string> fileListPaths)
        {
            SPDirPath = SPDirPath +"\\"+ folderName.Replace("/", "\\");
            fileListPaths = new List<string>();
            using (ClientContext context = GetContextObject())
            {
                Web web = context.Web;
                context.Load(web, website => website.ServerRelativeUrl);
                context.ExecuteQuery();
                Console.WriteLine("web loaded");
                var docLibs = context.LoadQuery(web.Lists.Where(l => l.BaseTemplate == 101));
                context.ExecuteQuery();
                Console.WriteLine("doclibs loaded");
               
                {
                    FolderCollection folderList;
                    if (string.IsNullOrEmpty(folderName))
                        folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName).Folders;
                    else
                        folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Folders;
                    context.Load(folderList);
                    context.ExecuteQuery();
                    var folders = folderList.ToList();
                    Console.WriteLine("group type null started");
                    foreach (Folder folder in folders)
                    {
                        if (folder.Name == "Forms")
                            continue;
                        if (!folder.Files.AreItemsAvailable)
                        {
                            if (string.IsNullOrEmpty(folderName))
                                GetFolders(web, context, libraryName, folder.Name, SPDirPath, spyear);
                            else
                                GetFolders(web, context, libraryName, folderName + "/" + folder.Name, SPDirPath, spyear);
                        }
                        Console.WriteLine(folder.Name + "---- File count " + folder.ItemCount);
                    }
                }
            }
            fileListPaths = filePaths;
            return fileCount;
        }

        public static void GetFolders(Web web,ClientContext context, string libraryName, string folderName,string SPDirPath,int spyear)
        {
            Console.WriteLine("Getfolders1");
            FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Folders;
            context.Load(folderList);
            context.ExecuteQuery();
            var folders = folderList.ToList();
            if (folders.Count > 0)
            {
                Console.WriteLine("Getfolders1");
                foreach (Folder folder in folders)
                {
                    if (folder.Name == "Forms")
                        continue;
                    if (!folder.Files.AreItemsAvailable)
                    {
                        GetFolders(web, context, libraryName, folderName + "/" + folder.Name, SPDirPath, spyear);
                    }
                }
            }
            else
            {
                Console.WriteLine("Getfolders3");
                FileCollection fileList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Files;
                context.Load(fileList);
                context.ExecuteQuery();
                var files = fileList.ToList();
                fileCount = fileCount + files.Count();
                foreach (Microsoft.SharePoint.Client.File file in fileList)
                {
                    string[] split = folderName.Split('/');
                    string firstPart = string.Join("/", split.Take(split.Length - 1));
                    string lastPart = split.Last();
                    int folderId = 0;
                    Int32.TryParse(lastPart, out folderId);
                    string groupFolderName = string.Empty;
                    if (folderId > 0)
                    {
                        groupFolderName = split.Take(split.Length - 1).ToList()[0].ToString();
                        if (groupFolderName == "Private_Conversations" && spyear < 2014)
                            groupFolderName = folderName.Replace("/", "\\");
                    }
                    else
                    {
                        groupFolderName = lastPart;
                    }
                    //folderName.Substring(folderName.LastIndexOf("/") + 1);
                    Console.WriteLine(groupFolderName + "---- File Name " + file.Name);
                    string filePath = Path.Combine(SPDirPath, groupFolderName, file.Name);
                    Console.WriteLine(filePath);
                    filePaths.Add(filePath);
                    //filePaths.Add(filePath);
                }
            }
        }
        public static List<string> GetFileListFromSP(string libraryName, string folderName, string groupType, string SPDirPath)
        {
            List<string> filePaths = new List<string>();
            using (ClientContext context = GetContextObject())
            {
                Web web = context.Web;
                context.Load(web, website => website.ServerRelativeUrl);
                context.ExecuteQuery();

                IEnumerable<List> docLibs = context.LoadQuery(web.Lists.Where(l => l.BaseTemplate == 101));
                
                context.ExecuteQuery();

                if (groupType == "Multi")
                {
                    FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Folders;
                    context.Load(folderList);
                    context.ExecuteQuery();
                    var folders = folderList.ToList();

                    foreach (Folder folder in folders)
                    {
                        Console.WriteLine(folder.Name + "---- File count " + folder.ItemCount);

                        FileCollection fileList = folder.Files;
                        context.Load(fileList);
                        context.ExecuteQuery();
                        foreach (Microsoft.SharePoint.Client.File file in fileList)
                        {
                            Console.WriteLine(folder.Name + "---- File Name " + file.Name);
                            string filePath = Path.Combine(SPDirPath, folderName.Replace("/", "\\"), folder.Name, file.Name);
                            //string groupfullName = Path.Combine(folderName, folder.Name).Replace("\\", "/");
                            //filePaths.Add(new Tuple<string,string> (filePath, groupfullName));
                            filePaths.Add(filePath);
                        }
                    }
                }
                else if (groupType == "Single")
                {
                    FileCollection fileList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + libraryName + "/" + folderName).Files;
                    context.Load(fileList);
                    context.ExecuteQuery();
                    var files = fileList.ToList();

                    foreach (Microsoft.SharePoint.Client.File file in fileList)
                    {
                        Console.WriteLine(folderName + "---- File Name " + file.Name);
                        string filePath = Path.Combine(SPDirPath, folderName.Replace("/", "\\"),file.Name);
                        //filePaths.Add(new Tuple<string, string>(filePath, folderName.Replace("\\", "/")));
                        filePaths.Add(filePath);
                    }
                }
                else if (groupType == "")
                {
                
                }
            }
            return filePaths;
        }

        public static bool CreateDocumentLibrary(string libraryName)
        {
            using (ClientContext clientContext = GetContextObject())
            {
                Web web = clientContext.Web;
                clientContext.Load(web, website => website.ServerRelativeUrl);
                clientContext.ExecuteQuery();
                Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                string documentLibrary = regex.Replace(Configuration.FileUrl, string.Empty);
                ListCreationInformation cr = new ListCreationInformation();
                cr.Title = libraryName;
                cr.TemplateType = 101;
                cr.QuickLaunchOption = QuickLaunchOptions.On;
                var dlAdd = web.Lists.Add(cr);
                web.QuickLaunchEnabled = true;
                web.Update();
                clientContext.Load(dlAdd);
                clientContext.ExecuteQuery();
                NavigationNodeCreationInformation nr = new NavigationNodeCreationInformation();
                nr.Title = libraryName;
                nr.AsLastNode = true;
                nr.Url = Configuration.SiteUrl + libraryName + "/Forms/AllItems.aspx";
                var dlAdd1 = web.Navigation.QuickLaunch.Add(nr);
                web.QuickLaunchEnabled = true;
                web.Update();
                clientContext.Load(dlAdd1);
                clientContext.ExecuteQuery();
            }
            return true;
        }

        public static List<string> GetFolderListFromSharePoint(string targetDocLib)
        {
            List<string> yearFolderList = new List<string>();
            try
            {
                using (ClientContext clientContext = GetContextObject())
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web, website => website.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    Regex regex = new Regex(Configuration.SiteUrl, RegexOptions.IgnoreCase);
                    string strSiteRelavtiveURL = regex.Replace(targetDocLib, string.Empty);
                    FolderCollection folderList = web.GetFolderByServerRelativeUrl(web.ServerRelativeUrl + "/" + strSiteRelavtiveURL).Folders;
                    clientContext.Load(folderList);
                    clientContext.ExecuteQuery();
                    var folders = folderList.ToList();
                    foreach (Folder folder in folders)
                    {
                        if (folder.Name != "Forms") //Direct files at library comes under Forms folder
                            yearFolderList.Add(folder.Name);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return yearFolderList;
        }
        private static ClientContext GetContextObject()
        {
            ClientContext context = new ClientContext(Configuration.SiteUrl);
            context.Credentials = new SharePointOnlineCredentials(Configuration.UserName, GetPasswordFromConsoleInput(Configuration.PassWord));
            return context;
        }

        private static Stream ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return new MemoryStream(ms.ToArray()); ;
            }
        }

        private static SecureString GetPasswordFromConsoleInput(string password)
        {
            //Get the user's password as a SecureString
            SecureString securePassword = new SecureString();
            char[] arrPassword = password.ToCharArray();
            foreach (char c in arrPassword)
            {
                securePassword.AppendChar(c);
            }

            return securePassword;
        }
    }

    public class CommonHelper
    {
        public static string FrameFolderName(string uploadStartDate,string uploadEndDate,bool forCompression)
        {
            string FolderName = string.Empty;
            DateTime startdate = Convert.ToDateTime(uploadStartDate);
            DateTime enddate = Convert.ToDateTime(uploadEndDate);
            if (startdate.Day != enddate.Day || startdate.Month != enddate.Month)
            {
                FolderName = ((!forCompression)?"Groups":"") + startdate.Day.ToString() + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startdate.Month).Substring(0, 3) + "to" + enddate.Day.ToString() + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(enddate.Month).Substring(0, 3);
            }
            else
            {
                FolderName = ((!forCompression) ? "Groups" : "") + startdate.Day.ToString() + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startdate.Month).Substring(0, 3);
            }
            return FolderName;
        }

    }
    public class Configuration
    {
        public static string SiteUrl = ConfigurationManager.AppSettings["SP_Url"];
        public static string UserName { get; set; }
        public static string PassWord { get; set; }

        public static string FileUrl = ConfigurationManager.AppSettings["SP_targetLibrary"];
    }
    class FolderInfo
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public int ItemCount { get; set; }
    }

}
