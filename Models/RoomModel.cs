using Google.Cloud.Firestore;

namespace LockLock.Models
{
    [FirestoreData]
    public class RoomModel
    {
        public string RoomID { get; set; }

        [FirestoreProperty]
        public string adminID { get; set; }

        [FirestoreProperty]
        public string name { get; set; }

        [FirestoreProperty]
        public uint number { get; set; }


        [FirestoreProperty]
        public string objName { get; set; }
        

        [FirestoreProperty]
        public uint objNum { get; set; }
    }
}