_design/checkins/_view/by_busid_date
function(doc) {
  emit([doc.BusId, 
        doc.CheckinTime.Year,
        doc.CheckinTime.Mon,
        doc.CheckinTime.Day,
        doc.CheckinTime.Hour,
        doc.CheckinTime.Min,
        doc.CheckinTime.Sec], doc);
}

_design/checkins/_view/valid_routes_by_busid_date
function(doc) {
  if(doc.Route != -1) {
    emit([doc.BusId, 
          doc.CheckinTime.Year,
          doc.CheckinTime.Mon,
          doc.CheckinTime.Day,
          doc.CheckinTime.Hour,
          doc.CheckinTime.Min,
          doc.CheckinTime.Sec], doc.Route);
  }
}