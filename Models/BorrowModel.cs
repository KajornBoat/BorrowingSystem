
using Google.Cloud.Firestore;
using System;

namespace LockLock.Models
{
    [FirestoreData]
    public class BorrowModel
    {
        public string BorrowID { get; set; }

        [FirestoreProperty]
        public string roomID { get; set; }

        [FirestoreProperty]
        public DateTime time { get; set; }

        [FirestoreProperty]
        public string transactionID { get; set; }

        [FirestoreProperty]
        public bool cancel { get; set; }

        [FirestoreProperty]
        public bool otherGroup { get; set; }
    }
}