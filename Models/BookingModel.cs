using System;
using System.Collections.Generic;

namespace LockLock.Models
{

    public class BookingModel
    {
        public string BookingID { get; set; }
        public string userID { get; set; }
        public string Name { get; set; }
        public int Num { get; set; }
        public string RoomName { get; set; }
        public string RoomID { get; set; }
        public List<DateTime> timeList { get; set; }
        public bool cancel { get; set; }
        public bool inBlacklist { get; set; }
        public string status { get; set; }
        public string name { get; set; }
        public DateTime timestamp { get; set; }
    }

}