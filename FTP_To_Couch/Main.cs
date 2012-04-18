using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace FTP_To_Couch
{
    class MainClass
    {
        class test
        {
            public string test1 {get;set;}
        }

        public static void Main (string[] args)
        {
            Console.WriteLine ("Hello World!");

            var db = new CouchDb("http://oilytheotter.iriscouch.com");
            //var db = new CouchDb("http://127.0.0.1:5984/");

            if(!db.DatabaseExists(CouchDb.HRT_DB_NAME))
                db.CreateDatabase(CouchDb.HRT_DB_NAME);

            while(true)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string contents = GetFileFromServer (new Uri ("ftp://216.54.15.3/Anrd/hrtrtf.txt"));
                List<BusCheckin> checkins = GetBusCheckinsFromFile (contents);
                List<BusCheckin> checkinsToDb = new List<BusCheckin>();

                int processed = 0;
                int added = 0;
                int routes = 0;
                for(int i=0; i<checkins.Count; i++)
                {
                    var checkin = checkins[i];

                    processed++;
                    if(!db.BusCheckinExists(checkin))
                    {
                        if(checkin.Route == -1)
                        {
                            for(int j=i-1; j>=0; j--)
                            {
                                if(checkins[j].BusId == checkin.BusId && checkins[j].Route != -1)
                                {
                                    checkin.Route = checkins[j].Route;
                                    break;
                                }
                            }
                        }
                        if(checkin.Route == -1)
                            checkin.Route = db.GetRouteForBusId(checkin.BusId);

                        if(checkin.Route != -1)
                            routes++;

                        checkinsToDb.Add(checkin);
                    }
                }

                db.CreateMultipleBusCheckins(CouchDb.HRT_DB_NAME, checkinsToDb);
                added = checkinsToDb.Count;
                
                stopwatch.Stop();
    
                Console.WriteLine(String.Format("Processed {0} checkins. {1} added. {2} have routes. {3} ms", processed, added, routes, stopwatch.ElapsedMilliseconds));

                System.Threading.Thread.Sleep(30000);
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

            if(!String.IsNullOrEmpty(file))
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
