using Google.Cloud.Firestore;
using System;

namespace LockLock.Models
{
    [FirestoreData]
    public class BlacklistDataModel
    {
        public string BlacklistID { get; set; }

        [FirestoreProperty]
        public string adminID { get; set; }

        [FirestoreProperty]
        public DateTime timeCreate { get; set; }

        [FirestoreProperty]
        public string userID { get; set; }
    }
}