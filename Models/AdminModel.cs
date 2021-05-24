using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace LockLock.Models
{
   [FirestoreData]
        public class AdminModel
        {
            public string UserID { get; set; }

            [FirestoreProperty]
            public string Email { get; set; }

            [FirestoreProperty]
            public string Firstname { get; set; }

            [FirestoreProperty]
            public string Lastname { get; set; }

            [FirestoreProperty]
            public string Tel { get; set; }

            [FirestoreProperty]
            public string role { get; set; }
            [FirestoreProperty]
            public List<string> rooms { get; set; }

        }
}