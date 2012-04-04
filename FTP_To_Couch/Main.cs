using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace FTP_To_Couch
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            Console.WriteLine ("Hello World!");
            
            string contents = GetFileFromServer (new Uri ("ftp://216.54.15.3/Anrd/hrtrtf.txt"));
            List<BusCheckin> checkins = GetBusCheckinsFromFile (contents);
            
            var serializer = new JavaScriptSerializer ();
            
            int count = 0;
            foreach (var checkin in checkins)
            {
                Console.WriteLine ("working " + ++count);
                var json = serializer.Serialize (checkin);
                var guid = Guid.NewGuid ();
                
                var docUrl = "http://127.0.0.1:5984/hrt/" + guid;
                var request = WebRequest.Create (docUrl);
                request.Method = "PUT";
                request.ContentType = "application/json";
                request.ContentLength = json.Length;
                
                var dataStream = new StreamWriter (request.GetRequestStream ());
                dataStream.Write (json);
                dataStream.Close ();
                
                request.GetResponse ().Close ();
            }
        }
        
        public static string GetFileFromServer (Uri serverUri)
        {
            // The serverUri parameter should start with the ftp:// scheme.
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return null;
            }
            // Get the object used to communicate with the server.
            WebClient request = new WebClient ();

            try
            {
                byte[] newFileData = request.DownloadData (serverUri.ToString ());
                string fileString = System.Text.Encoding.UTF8.GetString (newFileData);
                return fileString;
            } catch (WebException e)
            {
                Console.WriteLine (e.ToString ());
            }
            return null;
        }

        public static List<BusCheckin> GetBusCheckinsFromFile (string file)
        {
            List<BusCheckin> busCheckins = new List<BusCheckin> ();

            if (!String.IsNullOrEmpty (file))
            {
                foreach (string line in file.Split('\n', '\r'))
                {
                    BusCheckin checkin = BusCheckin.Create (line);
                    if (checkin != null)
                        busCheckins.Add (checkin);
                }
            }

            return busCheckins;
        }
    }
}
