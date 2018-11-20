// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using YammerLibrary;


using System.Data.SqlClient;

using System.Data;

namespace ConsoleRemoveDuplicateThreads
{
    class Program
    {
        private static string conn = string.Empty;
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }
        static async Task MainAsync()
        {

            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
           
           
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerAcctNameURL"];
            SharePointClassLibrary.Configuration.UserName = await getSecretCmdlet.GetSecretAsync();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerAcctPWDURL"];
            SharePointClassLibrary.Configuration.PassWord = await getSecretCmdlet.GetSecretAsync();

            List<SharePointClassLibrary.SPFileInfo> ListSPFiles = new List<SharePointClassLibrary.SPFileInfo>();
            List<string> filepaths = new List<string>();
            //Get list of top level folders
            List<string> ToplevelFolderlist = new List<string>();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["DBNameURL"];
            conn = await getSecretCmdlet.GetSecretAsync();

        
            //Fetch list of Top level Directories of the doclib and insert records in to
            DataSet ds = new DataSet();
            string DocLibName = string.Empty;
            string DocLibProcessStage = string.Empty;
            // Check if any deduplication has to start
            string Year = string.Empty;


            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                Year=  yeticontext.Yammer_GetYearForDedup(Environment.MachineName).ToString();
            }
                             

            if (string.IsNullOrEmpty(Year))
                Environment.Exit(0);

            List<DupThreads_FetchDocLibStatus_Result> List = new List<DupThreads_FetchDocLibStatus_Result>();

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                List = yeticontext.DupThreads_FetchDocLibStatus().ToList();
            }
            
            if (List.Count > 0)
            {
                foreach (DupThreads_FetchDocLibStatus_Result Listitem in List)
                {
                    DocLibName = Listitem.DocLibName.ToString();
                    DocLibProcessStage = Listitem.ProcessStage.ToString();
                }

                //call method to get list of TopLevel Folders
                ToplevelFolderlist = SharePointClassLibrary.AzureSharePointHelper.GetFolderListFromSharePoint(DocLibName);
                DataTable TopLevelFolders = new DataTable();
                TopLevelFolders.Columns.Add("TopLevelFolder", System.Type.GetType("System.String"));
                foreach (string TopLevelFolder in ToplevelFolderlist)
                {
                    TopLevelFolders.Rows.Add(TopLevelFolder);
                }
                // insert the top level folder names in to a different table and update the status as "TopFoldersFetched" in DocLibStatus table

           


                using (SqlConnection con = new SqlConnection(conn))
                {

                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "DupThreads_InsertTopLevelSubfolders";
                    cmd.Parameters.AddWithValue("TopLevelSubfolders", TopLevelFolders);
                    cmd.Parameters.AddWithValue("DocLibName", DocLibName);
                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                if (DocLibProcessStage == "TopLevelFoldersInserted")
                {


                    List<string> TopFolders = new List<string>();
                    using (YETIDBEntities yeticontext = new YETIDBEntities())
                    {
                        TopFolders = yeticontext.DupThreads_FetchTopLevelFolders(DocLibName).ToList();
                    }

                    
                    if (TopFolders.Count > 0)
                    {
                        //Read TopLevelFolders and passthrough each of them to get list of threads
                        foreach (string SubFolderName in TopFolders)
                        {
                            

                            //fetch threads info
                            SharePointClassLibrary.AzureSharePointHelper.GetSubFolders(DocLibName, SubFolderName, "", 2017, out ListSPFiles);
                            DataTable TableSPFiles = new DataTable();
                            //TableSPFiles.Columns.Add("ThreadID", System.Type.GetType("System.String"));
                            TableSPFiles.Columns.Add("DocLibName", System.Type.GetType("System.String"));
                            TableSPFiles.Columns.Add("TopLevelSubFolder", System.Type.GetType("System.String"));
                            TableSPFiles.Columns.Add("ThreadID_FileName", System.Type.GetType("System.String"));
                            TableSPFiles.Columns.Add("ThreadID_Size", System.Type.GetType("System.Int64"));
                            TableSPFiles.Columns.Add("ThreadID_CreatedDate", System.Type.GetType("System.DateTime"));
                            TableSPFiles.Columns.Add("ThreadID_Path", System.Type.GetType("System.String"));
                            //Insert Threadinfo to DB
                            foreach (SharePointClassLibrary.SPFileInfo SPfile in ListSPFiles)
                            {
                                TableSPFiles.Rows.Add(DocLibName, SubFolderName, SPfile.Filename, SPfile.filesize, SPfile.filecreateddate, SPfile.Filepath);
                            }

                            using (YETIDBEntities yeticontext = new YETIDBEntities())
                            {
                                yeticontext.DupThreads_FetchTopLevelFolders(DocLibName).ToList();
                            }


                            using (SqlConnection con = new SqlConnection(conn))
                            {

                                con.Open();
                                SqlCommand cmd = new SqlCommand();
                                cmd.Connection = con;
                                cmd.CommandTimeout = 0;
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.CommandText = "DupThreads_InsertThreadInfo";
                                cmd.Parameters.AddWithValue("SPFileInfo", TableSPFiles);
                                cmd.ExecuteNonQuery();
                                con.Close();
                            }



                        }

                    }


                }

                DataSet dsThreadsTobeDeleted = new DataSet();

                List<DupThreads_FetchThreadstobeDeleted_Result> ListThreadstoDelete = new List<DupThreads_FetchThreadstobeDeleted_Result>();

                using (YETIDBEntities yeticontext = new YETIDBEntities())
                {
                    ListThreadstoDelete=  yeticontext.DupThreads_FetchThreadstobeDeleted(Environment.MachineName).ToList();
                }


                


                if (ListThreadstoDelete.Count > 0)
                {

                    DataTable DThreads = new DataTable();
                    //Access threads data to be deleted. 
                    DThreads = ds.Tables[0];
                    var ListThreadsTobeDeleted1 = DThreads.AsEnumerable().ToList();

                    string message = "";
                    foreach (DupThreads_FetchThreadstobeDeleted_Result ThreadToDelete in ListThreadstoDelete)
                    {
                        try
                        {
                            //Can delete the Thread
                            bool ThreadDeleted = SharePointClassLibrary.AzureSharePointHelper.FileExistAndDelete(ThreadToDelete.ThreadID_Path.ToString(), ThreadToDelete.DocLibName.ToString(), ThreadToDelete.ThreadID_Filename.ToString());
                            // Update Database to indicate that the duplicate thread is deleted from Sharepoint
                            if (ThreadDeleted)
                            {
                                using (YETIDBEntities yeticontext = new YETIDBEntities())
                                {
                                    yeticontext.DupThreads_UpdateDeletedThreadStatus(ThreadToDelete.ThreadID_Filename.ToString(),ThreadToDelete.ThreadID_Path.ToString(),ThreadToDelete.ThreadID_Size,ThreadToDelete.DocLibName);
                                }


                                    
                            }

                            //}
                        }
                        catch
                        (Exception e)
                        {
                            message = "Exception in calling linq query";
                            Console.WriteLine(message.ToString());
                        }

                    }

                    int DedupFinished = 0;


                    using (YETIDBEntities yeticontext = new YETIDBEntities())
                    {
                        DedupFinished = yeticontext.DupThreads_CheckDeleteThreadStatus(DocLibName.ToString());
                    }

                    

                    if (DedupFinished == 1)
                    {
                        UpdateYearStatus(Year);
                    }

                }
                else
                {
                    UpdateYearStatus(Year);
                }
            }

            

        }


        private static void UpdateYearStatus(string Year)
        {

            using (YETIDBEntities yeticontext = new YETIDBEntities())
            {
                yeticontext.Yammer_Update_YearStatus(Year, 1, 0, 0, 0);
            }
            

            
        }
    }
}
