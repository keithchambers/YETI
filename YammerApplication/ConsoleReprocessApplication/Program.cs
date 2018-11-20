// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Web.Security.AntiXss;
using System.Xml.Xsl;
using System.Collections;

namespace ConsoleReprocessApplication
{
    class Program
    {
        private static string dirMissedPath = string.Empty;
        private static string dirXMLPath = ConfigurationManager.AppSettings["dirMissedPath"] + "\\ReprocessXML\\";
        private static string filedirPath = ConfigurationManager.AppSettings["filesPath"];
        private static string tempFiledirPath = ConfigurationManager.AppSettings["TempFilesPath"];
        private static string pagedirPath = ConfigurationManager.AppSettings["pagesPath"];
        private static string tempPagedirPath = ConfigurationManager.AppSettings["TempPagesPath"];
        private static string YammerdirPath = ConfigurationManager.AppSettings["YammerdirPath"];
        public static long m_packageSize = 1024 * 1024 * 100;

        static int timesTried = 0;
        static void Main(string[] args)
        {
            dirMissedPath = ConfigurationManager.AppSettings["dirMissedPath"];

           
            DownloadCSVFiles();
            ExtractFiles();
            LogEvents("Information", "ReProcessLog", "ExtractedFiles");
            Reprocess();
        }
        private static void DownloadCSVFiles()
        {
            string startDate = string.Empty;
            string endDate = string.Empty;
            try
            {
                DataSet ds = new DataSet();
                var isoDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
                string newStartDate = string.Empty;
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                {

                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Yammer_Reprocess_GetTimesTried";
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();

                }
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    startDate = Convert.ToDateTime(dr["StartDate"]).ToString(isoDateTimeFormat.SortableDateTimePattern);
                    endDate = Convert.ToDateTime(dr["EndDate"]).ToString(isoDateTimeFormat.SortableDateTimePattern);
                    timesTried = Convert.ToInt32(dr["TimesTried"]);
                    ExportAPICall(startDate, endDate);
                }
            }
            catch (Exception ex) when (ex is WebException)
            {
                LogEvents("export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());
            }
        }

        private static void ExtractFiles()
        {
            DirectoryInfo ydi = new DirectoryInfo(dirMissedPath);

            FileInfo[] fileList = ydi.GetFiles("*.zip");
            string status = string.Empty;
            if (fileList.Count() > 0)
            {
                foreach (FileInfo yfi in fileList)
                {
                    string zipPath = yfi.FullName;
                    string tempextractPath = ConfigurationManager.AppSettings["DBTempPath"] + zipPath.Substring(zipPath.LastIndexOf("\\")).Split('.')[0];
                    try
                    {
                        using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName.StartsWith("files/") && !File.Exists(Path.Combine(tempFiledirPath, entry.Name)))
                                    entry.ExtractToFile(Path.Combine(tempFiledirPath, entry.Name));
                                else if (entry.FullName.StartsWith("pages/") && !File.Exists(Path.Combine(tempPagedirPath, entry.Name)))
                                    entry.ExtractToFile(Path.Combine(tempPagedirPath, entry.Name));
                            }
                        }

                        if (!Directory.Exists(dirMissedPath + "\\ExtractedCSV\\"))
                            Directory.CreateDirectory(dirMissedPath + "\\ExtractedCSV\\");

                        string destFile = Path.Combine(dirMissedPath + "\\ExtractedCSV\\", zipPath.Substring(zipPath.LastIndexOf("\\") + 1));
                        if (File.Exists(destFile))
                            File.Delete(destFile);
                        File.Move(zipPath, destFile);


                    }
                    catch (Exception ex)
                    {
                        LogEvents("Error", "extractfile" + yfi.FullName, ex.ToString());
                        Environment.Exit(0);
                    }
                    ProcessStatusUpdate(zipPath.Substring(zipPath.LastIndexOf("\\") + 1), "Extracted");


                }
            }
        }

        private static void Reprocess()
        {
            VerifyAndRenameExtractedFiles("uploadedfile");
            VerifyAndRenameExtractedFiles("page");

            //only after all the files extracted and renamed, we start to do apply path for attachments
            bool isRenamedForAll = false;
            isRenamedForAll = IsRenamedForAll();
            if (isRenamedForAll == true)
            {
                ModifyHTMLByThreads();
                ArchiveThreadInDB();
                ZipFolders();
            }



        }
      
        private static void ArchiveThreadInDB()
        {
            try
            {
                string updateQuery = string.Empty;
                string insertQuery = string.Empty;
                bool isFileAvailable = true;
                if (Directory.Exists(dirXMLPath))
                {
                    DirectoryInfo ydi = new DirectoryInfo(dirXMLPath);
                    while (isFileAvailable)
                    {
                        IEnumerable<FileInfo> fileList = ydi.EnumerateFiles("*.xml").Take(100);
                        if (fileList.Count() == 0)
                            isFileAvailable = false;
                        foreach (FileInfo fi in fileList)
                        {
                            string whole_file = System.IO.File.ReadAllText(fi.FullName);
                            whole_file = whole_file.Replace("'", "''");
                            updateQuery = updateQuery + "UPDATE YM_ArchivedThreads SET Modified_Date = GETDATE() WHERE Thread_id = " + fi.Name.Split('.')[0] + "; \n";

                            insertQuery = insertQuery + "INSERT INTO YM_ArchivedThreads (Thread_id, ThreadXMLContent)  SELECT * FROM (SELECT " + fi.Name.Split('.')[0] + " Thread_id,CONVERT(XML,N'" + whole_file + "') XMLData) A "
                                           + " WHERE 0 = (SELECT COUNT(THREAD_ID) FROM YM_ArchivedThreads WHERE Thread_id = " + fi.Name.Split('.')[0] + " ); \n";
                        }
                        if (!string.IsNullOrEmpty(insertQuery))
                        {
                            int updateReturnValue = 0;
                            int insertReturnValue = 0;

                            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                            {
                                con.Open();
                                SqlCommand cmd = new SqlCommand();
                                cmd.Connection = con;
                                cmd.CommandTimeout = 0;
                                cmd.CommandText = updateQuery;
                                updateReturnValue = cmd.ExecuteNonQuery();
                                cmd.CommandText = insertQuery;
                                insertReturnValue = cmd.ExecuteNonQuery();
                                con.Close();
                            }
                            if ((insertReturnValue + updateReturnValue) == fileList.Count())
                            {
                                foreach (FileInfo fi in fileList)
                                {
                                    fi.Delete();
                                }
                            }
                            else
                            {
                                LogEvents("Error", "ArchivedThreads", fileList.ToList()[0].Name + "_" + fileList.ToList()[99].Name + "_" + insertReturnValue.ToString());
                            }
                            insertQuery = string.Empty;
                            updateQuery = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "ArchivedThreads", ex.ToString());
                Environment.Exit(0);
            }
        }
        private static void ResetArchievedThreadStatus()
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_ResetArchievedThreadStatus";
                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

        public static void ModifyHTMLByThreads()
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
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                {

                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Yammer_Reprocess_GetThrdsToModFls";
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                }
                DataTable dtFileThread = new DataTable();
                DataTable dtPageThread = new DataTable();
                dtFileThread = ds.Tables[0];
                dtPageThread = ds.Tables[1];

                var countdownEvent = new CountdownEvent(dtFileThread.Rows.Count);
                int parts = ds.Tables[0].Rows.Count / 1000;
                if ((ds.Tables[0].Rows.Count % 1000) > 0)
                    parts++;
                int skipCount = 0;
                while (parts > 0)
                {
                    countdownEvent = new CountdownEvent(dtFileThread.Select().Skip(skipCount * 1000).Take(1000).Count());
                    foreach (DataRow dr in dtFileThread.Select().Skip(skipCount * 1000).Take(1000))
                    {
                        DataSet dsMes = new DataSet();
                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                        {
                            con.Open();
                            SqlCommand cmd = new SqlCommand();
                            cmd.Connection = con;
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "Yammer_Reprocess_GetMsgsAndArchThrdsByThrdId";
                            cmd.Parameters.AddWithValue("in_nvarchar_thread_id", dr["thread_id"].ToString());
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            da.Fill(dsMes);
                            con.Close();
                        }

                        if (dsMes.Tables[0].Rows.Count > 0 || dsMes.Tables[2].Rows.Count > 0)
                        {
                            threadIds.Append("," + dr["thread_id"].ToString());
                            //begin added for fixing string truncate issue 20170302
                            dtThreadId.Rows.Add(dr["thread_id"].ToString());
                            //end added
                        }

                        new Thread(delegate ()
                        {
                            List<string> xmlnodes = new List<string>();
                            try
                            {
                                bool isNewThread = true;
                                bool isModified = false;
                                DataSet dsMissAtt = dsMes;
                                if (dsMissAtt.Tables[0].Rows.Count > 0 || dsMissAtt.Tables[2].Rows.Count > 0)
                                {
                                    string xmlnode = string.Empty;
                                    DataSet dsLatestYear = new DataSet();
                                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                                    {
                                        con.Open();
                                        SqlCommand cmd = new SqlCommand();
                                        cmd.Connection = con;
                                        cmd.CommandTimeout = 0;
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        cmd.CommandText = "Yammer_Reprocess_GetLatestYearForThreadId";
                                        cmd.Parameters.AddWithValue("in_nvarchar_thread_id", dr["thread_id"].ToString());
                                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                                        da.Fill(dsLatestYear);
                                        con.Close();
                                    }
                                    string latestYear = dsLatestYear.Tables[0].Rows[0]["LatestYear"].ToString();
                                    YammerdirPath = Path.Combine(ConfigurationManager.AppSettings["YammerdirPath"], latestYear);


                                    if (dsMissAtt.Tables[1].Rows.Count > 0 && (dsMissAtt.Tables[0].Rows.Count > 0 || dsMissAtt.Tables[2].Rows.Count > 0))
                                    {
                                        xmlnodes = ParseArchievedXML(dsMissAtt, dr["thread_id"].ToString(), out isModified);
                                    }
                                    if (xmlnodes.Count() > 0)
                                        isNewThread = false;
                                    bool isVersioned = false;

                                    DataRow lastRow = dsMissAtt.Tables[0].Rows[dsMissAtt.Tables[0].Rows.Count - 1];
                                    string gfolderName = string.Empty;
                                    if (Convert.ToString(lastRow["group_id"]) == string.Empty)
                                        gfolderName = "Private_Conversations";
                                    else
                                        gfolderName = string.Concat(lastRow["group_id"], " ", lastRow["group_name"].ToString().Replace("/", " "));

                                    SaveAsXmlFile(xmlnodes, lastRow["thread_id"].ToString(), (Convert.ToString(lastRow["group_id"]) == string.Empty) ? "Private_Conversations" : lastRow["group_name"].ToString().Replace("/", " "));


                                    List<string> directoryList = Directory.EnumerateDirectories(ConfigurationManager.AppSettings["YammerdirPath"]).ToList();
                                    foreach (string direcTory in directoryList)
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
                                    CreateHTMLFromXMLFile(gfolderName, Path.Combine(dirMissedPath, "ReprocessXML", lastRow["thread_id"].ToString() + ".xml"), string.Concat(lastRow["created_at"], lastRow["thread_id"].ToString(), ".html"), lastRow["thread_id"].ToString());


                                    if (gfolderName.Length > 65) // SharePoint folder lenght limit
                                        gfolderName = gfolderName.Substring(0, 65);
                                    if (Directory.EnumerateFiles(Path.Combine(YammerdirPath, RemoveUnsupportedFolderNameChars(gfolderName), dr["thread_id"].ToString()), "*" + dr["thread_id"].ToString() + ".html").Count() == 1)
                                    {
                                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
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
                                                cmd.Parameters.AddWithValue("in_nvarchar_thread_id", dr["thread_id"].ToString());
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
                                                        LogEvents("Error", "Delete Archive Thread - " + dr["thread_id"].ToString() + "_" + csvFileName, ex.ToString());
                                                }
                                                catch (Exception ex1)
                                                {
                                                    if (!ex1.ToString().Contains("This SqlTransaction has completed; it is no longer usable"))
                                                    {
                                                        LogEvents("Error", "Delete Archive Thread - " + dr["thread_id"].ToString() + "_" + csvFileName, ex1.ToString());
                                                        dtFailedThreadId.Rows.Add(dr["thread_id"].ToString());
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
                                countdownEvent.Signal();
                            }
                            catch (Exception ex)
                            {
                                if (ex.ToString().Contains("Reprocessing-Object reference not set to an instance of an object") && ex.ToString().Contains("ParseArchievedXML"))
                                    dtFailedThreadId.Rows.Add(dr["thread_id"].ToString());
                                else if (!ex.ToString().Contains("Reprocessing-This SqlTransaction has completed; it is no longer usable"))
                                    LogEvents("Error", dr["thread_id"].ToString() + "_" + csvFileName, ex.ToString());
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
                    updateThreadStatus("FilesRenamed", "HTMLModified", dtThreadId);
                    dtThreadId.Clear();


                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", csvFileName, ex.ToString());
                Environment.Exit(0);
            }

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
        private static void SaveAsXmlFile(List<string> xmlnodes, string fileName, string groupName)
        {
            bool isParentExists = false;
            bool isReplyTagAdded = false;
            if (!Directory.Exists(Path.Combine(dirMissedPath, "ReprocessXML")))
                Directory.CreateDirectory(Path.Combine(dirMissedPath, "ReprocessXML"));
            XmlTextWriter writer = new XmlTextWriter(Path.Combine(dirMissedPath, "ReprocessXML", fileName + ".xml"), System.Text.Encoding.Unicode);
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
                //string encodedXmlnode = xmlnode;

                if (xmlnode.Contains("<script") && xmlnode.Contains("</script>"))
                {
                    encodedXmlnode = xmlnode.Replace("<script", "&lt;script");
                    encodedXmlnode = xmlnode.Replace("</script>", "&lt;/script&gt;");
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

        static void updateThreadStatus(string prevStatus, string newStatus, DataTable thread_ids)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_Reprocess_UpdateThreadStatus";
                cmd.Parameters.Add("in_nvarchar_newStatus", SqlDbType.NVarChar).Value = newStatus;
                cmd.Parameters.Add("in_nvarchar_prevStatus", SqlDbType.NVarChar).Value = prevStatus;
                cmd.Parameters.Add("processedBy", SqlDbType.NVarChar).Value = Environment.MachineName;
                cmd.Parameters.Add("in_datatable_ThreadIds", SqlDbType.Structured).Value = thread_ids;
                cmd.ExecuteNonQuery();
                con.Close();
            }
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

        public static bool IsRenamedForAll()
        {
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Yammer_Reprocess_IsRenamedForAll";
                    SqlParameter output = new SqlParameter("out_bit_isRenamed", SqlDbType.Bit);
                    output.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(output);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                    return Convert.ToBoolean(output.Value);
                }
            }
            catch (Exception ex)
            {
                LogEvents("Error", "IsRenamedForAll", ex.ToString());
                Environment.Exit(0);
                return false;
            }
        }
        public static void VerifyAndRenameExtractedFiles(string filter)
        {
            string extension = string.Empty;
            string combineNewFileName = string.Empty;
            try
            {
                DataSet ds = new DataSet();
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Yammer_Reprocess_GetAttchmntDtlsForRename";
                    cmd.Parameters.AddWithValue("filter", filter);
                    cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    con.Close();
                }
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    string[] split = dr["name"].ToString().Split('.');
                    string firstPart = string.Join(".", split.Take(split.Length - 1));
                    string lastPart = split.Last();
                    extension = (filter == "uploadedfile") ? (dr["name"].ToString().Contains('.')) ? "." + lastPart : "" : ".html";
                    string pathToSearch = Path.Combine(((filter == "uploadedfile") ? tempFiledirPath : tempPagedirPath), dr["id"] + extension);
                    string newFileName = string.Empty;
                    if (dr["VerCount"].ToString() != "1")
                        newFileName = dr["file_id"] + "-" + dr["id"] + extension;
                    else
                    {
                        string unsupportedCharRemoved = RemoveUnsupportedFileNameChars((filter == "uploadedfile") ? firstPart : dr["name"].ToString());
                        newFileName = CheckFilePathLength(((filter == "uploadedfile") ? filedirPath : pagedirPath), dr["file_id"].ToString(), unsupportedCharRemoved) + extension;
                     
                    }
                    if (File.Exists(pathToSearch))
                    {

                        combineNewFileName = Path.Combine(((filter == "uploadedfile") ? filedirPath : pagedirPath), newFileName);
                        if (File.Exists(combineNewFileName))
                            File.Delete(combineNewFileName);
                        File.Move(pathToSearch, combineNewFileName);
                        if (filter == "uploadedfile")
                            MissAttaStatusUpdate(dr["id"].ToString(), "FilesRenamed");
                        else
                            MissAttaStatusUpdate(dr["id"].ToString(), "PagesRenamed");
                    }
                    else
                    {
                        if (filter == "uploadedfile")
                            MissAttaStatusUpdate(dr["id"].ToString(), "FilesNotAvailable");
                        else
                            MissAttaStatusUpdate(dr["id"].ToString(), "PagesNotAvailable");
                    }
                }

            }
            catch (Exception ex)
            {
                LogEvents("Error", "RenameFiles_" + combineNewFileName, ex.ToString());
                Environment.Exit(0);
            }
        }

        public static void ExportAPICall(string start, string end)
        {
            string startDate = start;
            string endDate = end;
            string token = ConfigurationManager.AppSettings["token"];
            DateTime tempEndate = Convert.ToDateTime(endDate);

            string URL = "https://export.yammer.com/api/v1/export?since=" + startDate + "&until=" + endDate + "&access_token=" + token;
           
            try
            {
               
                bool deleteCorrupted = false;
                UpdateExportDetailsStatus(startDate, endDate, "In Progress", "", timesTried);

               

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
                    if (!Directory.Exists(ConfigurationManager.AppSettings["BackupdirPath"] + "\\RobocopyLogs"))
                        Directory.CreateDirectory(ConfigurationManager.AppSettings["BackupdirPath"] + "\\RobocopyLogs");
                    string logName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}_{2}_{3}.log", ConfigurationManager.AppSettings["BackupdirPath"] + "\\RobocopyLogs", "FileCopyProcessing", /*Year*/ startDate.Substring(0, 4) + "_" + tempEndate.Month, DateTime.Now.Ticks);
                    using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                    {
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.FileName = "ROBOCOPY";
                        process.StartInfo.Arguments = string.Format(
                            CultureInfo.InvariantCulture,
                            ConfigurationManager.AppSettings["RobocopyOneFileCommandTemplate"],
                            AppDomain.CurrentDomain.BaseDirectory + @"DownloadFile",
                            //dirMissedPath + "\\" + startDate.Substring(0, 4),
                            dirMissedPath,
                            "export-" + endDate.Replace(':', '-') + ".zip", logName);
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
                    //if (File.Exists(dirMissedPath + "\\" + startDate.Substring(0, 4) + "\\export-" + endDate.Replace(':', '-') + ".zip"))
                    if (File.Exists(dirMissedPath + "\\export-" + endDate.Replace(':', '-') + ".zip"))
                        File.Delete(tempPath);
                }
                Thread.Sleep(5000);
                using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(dirMissedPath + "\\export-" + endDate.Replace(':', '-') + ".zip"))
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
                            UpdateExportDetailsStatus(startDate, endDate, "Stopped", "DownloadFailed", timesTried + 1);
                            deleteCorrupted = true;
                        }
                    }
                }
                if (deleteCorrupted)
                {
                    File.Delete(dirMissedPath /*+ "\\" + startDate.Substring(0, 4)*/ + "\\export-" + endDate.Replace(':', '-') + ".zip");
                    deleteCorrupted = false;
                    LogEvents("export - " + endDate.Replace(':', '-') + ".zip", " CorruptedRemoved " + dirMissedPath + /*"\\" + startDate.Substring(0, 4) +*/ "\\export-" + endDate.Replace(':', '-') + ".zip");
                    return;
                }
                UpdateExportDetailsStatus(startDate, endDate, "Completed", "DownloadCompleted", timesTried + 1);
                //}

            }
            catch (Exception ex) when (ex is WebException)
            {
                if (ex.Message == "The remote server returned an error: (429)." || ex.Message == "The operation has timed out")
                {
                    UpdateExportDetailsStatus(startDate, endDate, "Stopped", "DownloadFailed", timesTried);
                }
                else
                {
                    UpdateExportDetailsStatus(startDate, endDate, "Stopped", "DownloadFailed", timesTried + 1);
                }

                LogEvents("export - " + endDate.Replace(':', '-') + ".zip", ex.ToString());

                if (File.Exists(dirMissedPath + /*"\\" + startDate.Substring(0, 4) +*/ "\\export-" + endDate.Replace(':', '-') + ".zip"))
                    File.Delete(dirMissedPath + /*"\\" + startDate.Substring(0, 4) +*/ "\\export-" + endDate.Replace(':', '-') + ".zip");
            }
        }

        private static void UpdateExportDetailsStatus(string startDateTime, string endDateTime, string status, string stage, int timesTried, bool toMove = false)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {
             

                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_Reprocess_UpdateStatus";

                cmd.Parameters.Add("@in_nvarchar_startDate", SqlDbType.VarChar).Value = startDateTime;
                cmd.Parameters.Add("@in_nvarchar_endDate", SqlDbType.VarChar).Value = endDateTime;
                cmd.Parameters.Add("@in_nvarchar_status", SqlDbType.VarChar).Value = status;
                cmd.Parameters.Add("@in_nvarchar_stage", SqlDbType.VarChar).Value = stage;
                cmd.Parameters.Add("@in_int_timeTried", SqlDbType.Int).Value = timesTried;
                cmd.Parameters.Add("@in_bit_toMove", SqlDbType.Bit).Value = toMove;

                cmd.ExecuteNonQuery();
                con.Close();


            }
        }
        public static void LogEvents(string fileName, string errorDescription)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {

                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_Common_LogEvent";
                cmd.Parameters.AddWithValue("in_nvarchar_Source", "YM_Download");
                cmd.Parameters.AddWithValue("in_nvarchar_EventType", "Error");
                cmd.Parameters.AddWithValue("in_nvarchar_FileName", fileName);
                cmd.Parameters.AddWithValue("in_nvarchar_ErrorDescription", errorDescription);
                cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                cmd.ExecuteNonQuery();
                con.Close();
            }
        }
        public static void LogEvents(string evenType, string fileName, string errorDescription)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {
                errorDescription = errorDescription.Replace("'", "''");
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_Common_LogEvent";
                cmd.Parameters.AddWithValue("in_nvarchar_Source", "YM_ReProcessing");
                cmd.Parameters.AddWithValue("in_nvarchar_EventType", evenType.ToString());
                cmd.Parameters.AddWithValue("in_nvarchar_FileName", fileName);
                cmd.Parameters.AddWithValue("in_nvarchar_ErrorDescription", errorDescription);
                cmd.Parameters.AddWithValue("processedBy", Environment.MachineName);
                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

        public static void ProcessStatusUpdate(string csvFileName, string newStatus)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {

                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_Reprocess_ProcessStatusUpdate";
                cmd.Parameters.AddWithValue("in_varchar_csvFileName", csvFileName);
                cmd.Parameters.AddWithValue("in_varchar_newStatus", newStatus);

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }
        public static void MissAttaStatusUpdate(string id, string newStatus)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["MSL.Database.v2"].ConnectionString))
            {

                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Yammer_Reprocess_MissAttaStatusUpdate";
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("in_varchar_newStatus", newStatus);

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

        private static string ModifyXMLNode(DataSet dsMissAtt, string thread_id, string modifyNode)
        {
            string groupId = ((object)dsMissAtt.Tables[0].Rows[0]["groupId"] ?? dsMissAtt.Tables[2].Rows[0]["groupId"]).ToString();
            string groupName = ((object)dsMissAtt.Tables[0].Rows[0]["groupName"] ?? dsMissAtt.Tables[2].Rows[0]["groupName"]).ToString();
            int fileCount = dsMissAtt.Tables[0].Rows.Count;
            int pageCount = dsMissAtt.Tables[2].Rows.Count;
            string fileNA = "&amp;emsp;&amp;emsp; File Not Available&lt;br/&gt;";
            string pageNA = "&amp;emsp;&amp;emsp; Page Not Available&lt;br/&gt;";
            //string modifyNode = parentNode.ChildNodes[6].InnerText;
            StringBuilder sbAttach = new StringBuilder(modifyNode);
            int i = 0;
            while (modifyNode.Contains(fileNA))
            {
                int index = modifyNode.IndexOf(fileNA);
                string replaceAttachment = string.Empty;
                if (i < fileCount)
                {
                    ApplyPathsForAttachments("uploadedfile", dsMissAtt.Tables[0].Rows[i]["fileid"].ToString(), groupId, groupName, thread_id, out replaceAttachment);
                    modifyNode = sbAttach.Replace(fileNA, replaceAttachment, index, fileNA.Length).ToString();
                }
                else
                {
                    LogEvents("Reprocessing", thread_id.ToString(), "There is file uploaded duplicate for this thread");
                }
                i++;
            }
            i = 0;
            while (modifyNode.Contains(pageNA))
            {
                int index = modifyNode.IndexOf(pageNA);
                string replaceAttachment = string.Empty;
                if (i < pageCount)
                {
                    ApplyPathsForAttachments("uploadedpage", dsMissAtt.Tables[2].Rows[i]["pageid"].ToString(), groupId, groupName, thread_id, out replaceAttachment);
                    modifyNode = sbAttach.Replace(pageNA, replaceAttachment, index, pageNA.Length).ToString();
                }
                else
                {
                    LogEvents("Reprocessing", thread_id.ToString(), "There is page uploaded duplicate for this thread");
                }
                i++;
            }
            return modifyNode;
        }

        private static List<string> ParseArchievedXML(DataSet dsMissAtt, string thread_id, out bool isModified)
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

            xmlDoc.LoadXml(dsMissAtt.Tables[1].Rows[0]["ThreadXMLContent"].ToString());

            string groupId = ((object)dsMissAtt.Tables[0].Rows[0]["groupId"] ?? dsMissAtt.Tables[2].Rows[0]["groupId"]).ToString();
            string groupName = ((object)dsMissAtt.Tables[0].Rows[0]["groupName"] ?? dsMissAtt.Tables[2].Rows[0]["groupName"]).ToString();
            int fileCount = dsMissAtt.Tables[0].Rows.Count;
            int pageCount = dsMissAtt.Tables[2].Rows.Count;


            XmlNodeList xmlNodesList = xmlDoc.SelectNodes("/Thread/ParentMessage");
            if (xmlNodesList.Count > 0)
            {
                isModified = true;
                XmlNode parentNode = xmlNodesList.Item(0);


                string modifyNode = parentNode.ChildNodes[6].InnerText;
                modifyNode = ModifyXMLNode(dsMissAtt, thread_id, modifyNode);

              

                xmlnode = xmlnode + "`" + "parent";
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[1].InnerText; //ThreadId
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[2].InnerText; //SenderFullname
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[3].InnerText; //SenderEmail
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[4].InnerText; //Timestamp
                xmlnode = xmlnode + "`" + parentNode.ChildNodes[5].InnerText; //Body
                //xmlnode = xmlnode + "`" + parentNode.ChildNodes[6].InnerText; //Attachment
                xmlnode = xmlnode + "`" + modifyNode;
                xmlnodes.Add(xmlnode.Remove(0, 1));
                xmlnode = string.Empty;
                if (parentNode.ChildNodes.Count > 7)
                {
                    if (parentNode.ChildNodes[7].HasChildNodes) //check for reply threads
                    {

                        foreach (XmlNode childNode in parentNode.ChildNodes[7].ChildNodes)
                        {
                            string childModifyNode = childNode.ChildNodes[5].InnerText;
                            childModifyNode = ModifyXMLNode(dsMissAtt, thread_id, childModifyNode);

                            xmlnode = xmlnode + "`" + "child";
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[0].InnerText;//ThreadId
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[1].InnerText;//SenderFullname
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[2].InnerText;//SenderEmail
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[3].InnerText;//Timestamp
                            xmlnode = xmlnode + "`" + childNode.ChildNodes[4].InnerText;//Body
                            //xmlnode = xmlnode + "`" + childNode.ChildNodes[5].InnerText;//Attachment
                            xmlnode = xmlnode + "`" + childModifyNode;
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
                    string childModifyNode = childNode.ChildNodes[5].InnerText;
                    childModifyNode = ModifyXMLNode(dsMissAtt, thread_id, childModifyNode);
                    xmlnode = xmlnode + "`" + "child";
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[0].InnerText;//ThreadId
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[1].InnerText;//SenderFullname
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[2].InnerText;//SenderEmail
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[3].InnerText;//Timestamp
                    xmlnode = xmlnode + "`" + childNode.ChildNodes[4].InnerText;//Body
                    xmlnode = childModifyNode;//Attachment
                    xmlnodes.Add(xmlnode.Remove(0, 1));
                    xmlnode = string.Empty;
                }
            }
            return xmlnodes;
        }
        private static string RemoveUnsupportedFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars().Concat(new char[] { '?', '*', ':' }).  // Amazingly GetInvalidFileNameChars() fails to include a few characters not allowed in Windows file systems
                Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private static void ApplyPathsForAttachments(string filter, string fileId, string groupId, string groupName, string threadId, out string xmlnode)
        {
            bool isGreater = false;
            xmlnode = string.Empty;

            if (filter == "uploadedfile")
            {
                string attach_url = string.Empty;

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
                int file_id = int.Parse(fileId);
                string[] shareFiles = Directory.GetFiles(filedirPath, fileId + "-*.*");
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
                                threadFolderName = threadFolderName + "\\Attachments" + "\\" + fileId;
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
                                    if (!isVersioned)
                                        attach_url += string.Concat("&emsp;&emsp;<a href='", shareFile.ToString(), "'>", fileName, "</a>");
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
                                 attach_url += string.Concat("&emsp;&emsp;<a href='.\\Attachments\\", RemoveUnsupportedFileNameChars(fullName), "'>", fileName, "</a>");

                        }
                        threadFolderName = tempThreadFolderName;
                    }
                    if (Directory.Exists(threadFolderName + "\\Attachments" + "\\" + file_id))
                    {
                        if (hasRecentVersion)
                            attach_url = attach_url + string.Concat("&nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='.\\Attachments" + "\\" + file_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";
                        else
                            attach_url = attach_url + string.Concat("&emsp;&emsp; Recent Version Not Available &nbsp;<a  style=\"font-size:xx-small; font-style:italic\"  href='.\\Attachments" + "\\" + file_id, "'>", "(Click for Other versions)", "</a>") + "<br/>";

                    }
                }
                else
                    attach_url += "&emsp;&emsp; File Still Not Available" + "<br/>";

                //xmlnode = xmlnode + attach_url;
                xmlnode = attach_url;
            }

            if (filter == "uploadedpage")
            {
                string attach_url = string.Empty;
                bool hasRecentVersion = false;
                bool isVersion = false;
                int page_id = int.Parse(fileId);
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
                    attach_url += "&emsp;&emsp; Page Still Not Available" + "<br/>";
                //}
                xmlnode = xmlnode + attach_url;
            }
        }
        static bool ZipFolders()
        {
            bool returnValue = true;
            try
            {
                int Year = 2008;
                while (Year < 2018 && Directory.GetDirectories(Path.Combine(ConfigurationManager.AppSettings["YammerdirPath"], Year.ToString())).Length > 0)
                {
                    string[] folders = Directory.GetDirectories(Path.Combine(ConfigurationManager.AppSettings["YammerdirPath"], Year.ToString()));
                    foreach (string folder in folders)
                    {
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
                    }
                    Year++;
                }
                return returnValue;
            }
            catch (Exception ex)
            {
                LogEvents("Error", "FileCompress", ex.ToString());
                return false;
            }
        }

        static void SplitZipFolder(string inputFolderPath, string outputFolderandFile, string threadId)
        {
            #region otherApproach
      
            #endregion

            int cnt = 1;
            m_packageSize = m_packageSize * 20;
            ArrayList htmlFile = new ArrayList();
            ArrayList ar = GenerateFileList(inputFolderPath, out htmlFile); // generate file list          

            // Output file stream of package.
            //FileStream ostream = null;


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
        private static string RemoveUnsupportedFolderNameChars(string folderName)
        {
            return Path.GetInvalidPathChars().Concat(new char[] { '?', '*', ':' }).  // Amazingly GetInvalidPathChars() fails to include a few characters not allowed in Windows file systems
                Aggregate(folderName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
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
       
    }
}