using System;
using System.Collections.Generic;

namespace FTP_To_Couch
{
    public class BusDateTime
    {
        public int Year { get; set; }
        public int Mon { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Min { get; set; }
        public int Sec { get; set; }
    }
	
    public class Location
    {
        public string Lat { get; set; }
        public string Lon { get; set; }
    }
	
    public class BusCheckin
    {
        public BusDateTime CheckinTime { get; set; }
        public int BusId { get; set; }
        public Location Location { get; set; }
        public bool LocationValid { get; set; }
        public int Adherence { get; set; }
        public bool AdherenceValid { get; set; }
        public bool HasRoute { get; set; }
        public bool RouteLookedUp { get; set; }
        public int Route { get; set; }
        public int Direction { get; set; }
        public int StopId { get; set; }
        public string RawData { get; set; }

        protected BusCheckin ()
        {
        }

        public static BusCheckin Create (string data)
        {
            if (!String.IsNullOrEmpty (data))
            {
                BusCheckin checkin = new BusCheckin ();
                string[] parts = data.Split (',');
                DateTime checkinTime;
                int busId;
				
                if (DateTime.TryParse (parts [1] + "/" + DateTime.Today.Year.ToString () + " " + parts [0], out checkinTime) &&
                    Int32.TryParse (parts [2], out busId))
                {
                    checkin.CheckinTime = new BusDateTime {
						Year = checkinTime.Year,
						Mon = checkinTime.Month,
						Day = checkinTime.Day,
						Hour = checkinTime.Hour,
						Min = checkinTime.Minute,
						Sec = checkinTime.Second
					};
                    checkin.RawData = data;
                    checkin.RouteLookedUp = false;
                    checkin.BusId = busId;
                    string[] loc = parts [3].Split ('/');
                    string lat = loc [0];
                    string lon = loc [1];
                    checkin.Location = new Location {
						Lat = lat.Substring (0, lat.Length - 7) + "." + lat.Substring (lat.Length - 7),
						Lon = lon.Substring (0, lon.Length - 7) + "." + lon.Substring (lon.Length - 7)
					};
                    checkin.LocationValid = parts [4] == "V";
                    checkin.Adherence = Int32.Parse (parts [5]);
                    checkin.AdherenceValid = parts [6] == "V";

                    int route;
                    if (parts.Length > 7 && Int32.TryParse (parts [7], out route))
                    {
                        checkin.HasRoute = true;
                        checkin.Route = route;
                        checkin.Direction = Int32.Parse (parts [8]);
                        int stopId;
                        checkin.StopId = Int32.TryParse (parts [9], out stopId) ? stopId : -1;
                    }
					else
					{
                        checkin.HasRoute = false;
                        checkin.Route = -1;
                        checkin.Direction = -1;
                        checkin.StopId = -1;
                    }

                    return checkin;
                }
            }

            return null;
        }

    }

    public class BusCheckinDoc : BusCheckin
    {
        public string _id {get; set;}
        public BusCheckinDoc(BusCheckin c)
        {
            CheckinTime = c.CheckinTime;
            BusId = c.BusId;
            Location = c.Location;
            LocationValid = c.LocationValid;
            Adherence = c.Adherence;
            AdherenceValid = c.AdherenceValid;
            HasRoute = c.HasRoute;
            RouteLookedUp = c.RouteLookedUp;
            Route = c.Route;
            Direction = c.Direction;
            StopId = c.StopId;
            RawData = c.RawData;
            _id = Guid.NewGuid().ToString();
        }
    }

    public class BusCheckinDocs
    {
        public BusCheckinDoc[] docs { get; set; }

        public BusCheckinDocs(List<BusCheckinDoc> checkinDocs)
        {
            docs = checkinDocs.ToArray();
        }
    }
}
