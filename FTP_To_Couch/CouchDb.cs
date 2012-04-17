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

        public CouchDb(string dbUrl)
        {
            _dbUrl = dbUrl;
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
            var response = Request(_dbUrl + "_replicate", "POST", content);
        }

        public void DoPushReplication(string remoteTarget)
        {
            var content = "{\"source\":\"" + HRT_DB_NAME + "\",\"target\":\"" + remoteTarget + HRT_DB_NAME + "\"}";
            var response = Request(_dbUrl + "_replicate", "POST", content);
        }

        private static string Request(string url, string type, string content)
        {
            var request = WebRequest.Create (url);
            request.Method = type;
            request.ContentLength = 0;

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

