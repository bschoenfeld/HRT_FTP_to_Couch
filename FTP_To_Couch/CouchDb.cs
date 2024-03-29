using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace FTP_To_Couch
{
    public class CouchDb
    {
        public static string HRT_DB_NAME = "hrt";

        private static JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private string _dbUrl;
        private string _username;
        private string _password;

        public CouchDb(string dbUrl)
        {
            if(dbUrl.Contains("@"))
            {
                int usernameStartIndex = dbUrl.IndexOf("://") + 3;
                int passwordEndIndex = dbUrl.IndexOf("@");
                string usernamePassword = dbUrl.Substring(usernameStartIndex, passwordEndIndex - usernameStartIndex);
                _username = usernamePassword.Substring(0, usernamePassword.IndexOf(':'));
                _password = usernamePassword.Substring(usernamePassword.IndexOf(':') + 1);
                _dbUrl = dbUrl.Substring(0, usernameStartIndex) + dbUrl.Substring(passwordEndIndex + 1);
            }
            else
            {
                _dbUrl = dbUrl;
                _username = null;
                _password = null;
            }

            if(!_dbUrl.EndsWith("/"))
                _dbUrl += "/";
        }

        public bool DatabaseExists(string dbName)
        {
            var dbs = _serializer.Deserialize<string[]>(Request(_dbUrl + "_all_dbs", "GET", null));
            foreach(var db in dbs)
                if(db == dbName)
                    return true;
            return false;
        }

        public void CreateDatabase(string dbName)
        {
            Request(_dbUrl + dbName, "PUT", null);
        }

        public void CreateDocument(string dbName, object document)
        {
            Request(_dbUrl + dbName + "/" + Guid.NewGuid(), "PUT", _serializer.Serialize(document));
        }

        public void CreateMultipleBusCheckins(string dbName, List<BusCheckin> checkins)
        {
            List<BusCheckinDoc> docs = new List<BusCheckinDoc>();
            foreach(var checkin in checkins)
                docs.Add(new BusCheckinDoc(checkin));

            Request(_dbUrl + dbName + "/_bulk_docs", "POST", _serializer.Serialize(new BusCheckinDocs(docs)));
        }

        public bool BusCheckinExists(BusCheckin checkin)
        {
            var key = String.Format("[{0},{1},{2},{3},{4},{5},{6}]", checkin.BusId,
                                                                     checkin.CheckinTime.Year,
                                                                     checkin.CheckinTime.Mon,
                                                                     checkin.CheckinTime.Day,
                                                                     checkin.CheckinTime.Hour,
                                                                     checkin.CheckinTime.Min,
                                                                     checkin.CheckinTime.Sec);

            var response = Request(_dbUrl + HRT_DB_NAME + "/_design/checkins/_view/by_busid_date?key=" + key, "GET", null);
            var summary = (Dictionary<string, object>)_serializer.DeserializeObject(response);

            if(((object[])summary["rows"]).Length == 0)
                return false;
            else
                return true;
        }

        public int GetRouteForBusId(int busId)
        {
            var query = String.Format("?startkey=[{0},{{}}]&endkey=[{0}]&descending=true&limit=1", busId);
            var response = Request(_dbUrl + HRT_DB_NAME + "/_design/checkins/_view/valid_routes_by_busid_date" + query, "GET", null);
            var responseObj = (Dictionary<string, object>)_serializer.DeserializeObject(response);
            var rows = (object[])responseObj["rows"];
            if(rows.Length == 0)
                return -1;
            else
                return (int)((Dictionary<string, object>)rows[0])["value"];
        }

        public void DoPullReplication(string remoteSource)
        {
            var content = "{\"source\":\"" + remoteSource + HRT_DB_NAME + "\",\"target\":\"" + HRT_DB_NAME + "\"}";
            Request(_dbUrl + "_replicate", "POST", content);
        }

        public void DoPushReplication(string remoteTarget)
        {
            var content = "{\"source\":\"" + HRT_DB_NAME + "\",\"target\":\"" + remoteTarget + HRT_DB_NAME + "\"}";
            Request(_dbUrl + "_replicate", "POST", content);
        }

        private string Request(string url, string type, string content)
        {
            var request = WebRequest.Create (url);
            request.Method = type;
            request.ContentLength = 0;
            if(!String.IsNullOrEmpty(_username) && !String.IsNullOrEmpty(_password))
                request.Credentials = new NetworkCredential(_username, _password);

            if(!String.IsNullOrEmpty(content))
            {
                request.ContentType = "application/json";
                request.ContentLength = content.Length;

                using(var dataStream = new StreamWriter(request.GetRequestStream ()))
                {
                    dataStream.Write(content);
                }
            }

            using(var response = request.GetResponse())
            using(var dataStream = new StreamReader(response.GetResponseStream()))
            {
                return dataStream.ReadToEnd();
            }
        }
    }
}

