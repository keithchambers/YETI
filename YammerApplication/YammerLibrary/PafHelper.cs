using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS.IT.CFE.Framework;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace YammerLibrary
{
    public class PafHelper
    {
        public PafHelper()
        {
            try
            {
                YammerConfiguration.exportApiToken = Paf.Config.GetAppSetting(ConfigurationManager.AppSettings["ExportKey"]);
                YammerConfiguration.restApiToken = Paf.Config.GetAppSetting(ConfigurationManager.AppSettings["RestKey"]);                
                YammerConfiguration.serviceAccountName = Paf.Config.GetAppSetting("ServiceAccountName");
                YammerConfiguration.serviceAccountPass = Paf.Config.GetAppSetting("ServiceAccountPassword");
                string conString = Paf.Config.GetConnectionString(ConfigurationManager.AppSettings["DBKey"]);
                using (SqlConnection con = new SqlConnection(conString))
                {
                    con.Open();
                    YammerConfiguration.connectionString = conString;
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                WriteEventLog(ex);
            }
        }

        public void WriteEventLog(Exception ex)
        {
            string sSource;
            string sLog;
            string sEvent;

            sSource = "YammereDiscovery";
            sLog = "eDiscovery";
            sEvent = ex.ToString();

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);
            EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Error, 1001);
        }
        public class elementsconfig : PafConfiguration
        {
            public override string ApplicationName
            {
                get { return ConfigurationManager.AppSettings["AppName"]; }
            }
            public override string AzureADAuthString
            {
                get { return ConfigurationManager.AppSettings["AppAuth"]; }
            }
            public override MS.IT.CFE.Framework.Cache.ICacheStore FailoverCacheStore
            {
                get { return null; }
            }
            public override string PlatformServicesUrl
            {
                get { return ConfigurationManager.AppSettings["APIUrl"]; }
            }
        }

        public class YammerConfiguration
        {
            public static string exportApiToken { get; set; }

            public static string restApiToken { get; set; }

            public static string connectionString { get; set; }

            public static string serviceAccountName { get; set; }

            public static string serviceAccountPass { get; set; }
        }
    }
   
}
