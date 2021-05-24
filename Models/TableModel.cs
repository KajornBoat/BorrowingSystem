using System.Collections.Generic;
using System;

namespace LockLock.Models
{
    public class TableModel
    {
        public string objName { get; set; }

        public string timeLength { get; set; }

        public List<string> name { get; set; }

        public Tuple<string, uint>[,] table { get; set; }

        public string firstName { get; set; }

        public string lastName { get; set; }

        public string roomName { get; set; }

        public string roomID { get; set; }

        public int roomNum { get; set; }

        public string adminEmail { get; set; }
    }
}