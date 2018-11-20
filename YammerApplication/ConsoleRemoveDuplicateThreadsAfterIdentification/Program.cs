using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YammerLibrary;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;

namespace ConsoleRemoveDuplicateThreadsAfterindetification
{



    class ThreadTobeDeleted
    {
        public string ThreadID_DFilename { get; set; }
        public string ThreadID_DPath { get; set; }
        public String ThreadID_DSize { get; set; }
     }
    class ThreadTobeRetained
    {
        String ThreadID_RFilename { get; set; }
        String ThreadID_RPath { get; set; }
        String ThreadID_RSize { get; set; }
    }
   

    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
            //Declare list variables
            List<ThreadTobeDeleted> ListThreadsTobeDeleted = new List<ThreadTobeDeleted>();
            List<ThreadTobeRetained> ListThreadsTobeRetained = new List<ThreadTobeRetained>();

            GetSecretCmdlet getSecretCmdlet = new GetSecretCmdlet();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["DBNameURL"];
            //await getSecretCmdlet.GetSecretAsync();

            //SharePointClassLibrary.Configuration.UserName = PafHelper.YammerConfiguration.serviceAccountName;
            //SharePointClassLibrary.Configuration.PassWord = PafHelper.YammerConfiguration.serviceAccountPass;
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerAcctNameURL"];
            SharePointClassLibrary.Configuration.UserName = await getSecretCmdlet.GetSecretAsync();
            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["YammerAcctPWDURL"];
            SharePointClassLibrary.Configuration.PassWord = await getSecretCmdlet.GetSecretAsync();

            List<SharePointClassLibrary.SPFileInfo> ListSPFiles = new List<SharePointClassLibrary.SPFileInfo>();
            List<string> filepaths = new List<string>();
            //Get list of top level folders
            List<string> ToplevelFolderlist = new List<string>();

            getSecretCmdlet.secretURI = ConfigurationManager.AppSettings["DBNameURL"];
            string conn = await getSecretCmdlet.GetSecretAsync();


            //Fetch list of threads to be deleted from Sharepoint from DB.





            // SharePointClassLibrary.AzureSharePointHelper.GetSubFolders("DevDayYammer2017", "Groups3Aug", "", 2017, out ListSPFiles);
            //Fetch list of Top level Directories of the doclib and insert records in to
            DataSet ds = new DataSet();
            string DocLibName = string.Empty;
            string DocLibProcessStage = string.Empty;
            using (SqlConnection con = new SqlConnection(conn))
            {

                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "DupThreads_FetchThreadstobeDeleted";
                cmd.Parameters.AddWithValue("ProcessedBy", Environment.MachineName.ToString());
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                con.Close();
            }


            if (ds.Tables.Count > 0)
            {

                DataTable DThreads = new DataTable();
                //Access threads data to be deleted. 
                DThreads = ds.Tables[0];
                var ListThreadsTobeDeleted1 = DThreads.AsEnumerable().ToList();
                DThreads = ds.Tables[1];
                var ListThreadsTobeRetained2 = DThreads.AsEnumerable().ToList();

                string message = "";
                foreach (DataRow ThreadD in ListThreadsTobeDeleted1)
                {
                    try
                    {
                        var ThreadR = ListThreadsTobeRetained2.Where(r => r.Field<string>("ThreadID_Filename") == ThreadD["threadID_Filename"].ToString()).SingleOrDefault();


                        if (
                            (Convert.ToInt64(ThreadR["ThreadID_Size"].ToString()) > Convert.ToInt64(ThreadD["ThreadID_Size"].ToString())) &&
                            (Convert.ToDateTime(ThreadR["ThreadID_CreatedDate"].ToString()) > Convert.ToDateTime(ThreadD["ThreadID_CreatedDate"].ToString())) &&
                            SharePointClassLibrary.AzureSharePointHelper.CheckIfFileExists(ThreadR["ThreadID_Path"].ToString(), ThreadR["DocLibname"].ToString(), ThreadR["ThreadID_FileName"].ToString(), Convert.ToInt64(ThreadR["ThreadID_Size"].ToString())) &&
                            (ThreadR["ThreadID_Filename"].ToString() == ThreadD["ThreadID_Filename"].ToString())
                            )
                        {
                            //Can delete the Thread
                            bool ThreadDeleted = SharePointClassLibrary.AzureSharePointHelper.FileExistAndDelete(ThreadD["ThreadID_Path"].ToString(), ThreadD["DocLibname"].ToString(), ThreadD["ThreadID_Filename"].ToString());
                            // Update Database to indicate that the duplicate thread is deleted from Sharepoint
                            if (ThreadDeleted)
                            {
                                using (SqlConnection con = new SqlConnection(conn))
                                {

                                    con.Open();
                                    SqlCommand cmd = new SqlCommand();
                                    cmd.Connection = con;
                                    cmd.CommandTimeout = 0;
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.CommandText = "DupThreads_UpdateDeletedThreadStatus";
                                    cmd.Parameters.AddWithValue("ThreadID_Filename", ThreadD["ThreadID_filename"].ToString());
                                    cmd.Parameters.AddWithValue("ThreadID_Path", ThreadD["ThreadID_Path"].ToString());
                                    cmd.Parameters.AddWithValue("ThreadID_Size", Convert.ToInt64(ThreadD["ThreadID_Size"]));
                                    cmd.Parameters.AddWithValue("DocLibName", ThreadD["DocLibName"].ToString());
                                    cmd.ExecuteNonQuery();
                                    con.Close();
                                }
                            }

                        }
                    }
                    catch
                    (Exception e)
                    {
                      message = "Exception in calling linq query";
                        Console.WriteLine(message.ToString());
                    }

                }


                


                



            }






            //



            Console.ReadLine();



        }
    }
}
