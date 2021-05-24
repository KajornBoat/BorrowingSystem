
using Google.Cloud.Firestore;
using System;

namespace LockLock.Models
{
    [FirestoreData]
    public class TransactionModel
    {
        public string TransactionID { get; set; }

        [FirestoreProperty]
        public string roomID { get; set; }

        [FirestoreProperty]
        public DateTime timestamp { get; set; }

        [FirestoreProperty]
        public string userID { get; set; }

        [FirestoreProperty]
        public bool cancel { get; set; }
    }
}