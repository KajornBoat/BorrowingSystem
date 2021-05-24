using Google.Cloud.Firestore;
using System.ComponentModel;

namespace LockLock.Models
{
    [FirestoreData]
    public class SignUpModel
    {
        [FirestoreProperty]
        public string Email { get; set; }
        [FirestoreProperty]
        public string Firstname { get; set; }
        [FirestoreProperty]
        public string Lastname { get; set; }
        [FirestoreProperty]
        [DisplayName("Telephone Number")]
        public string Tel { get; set; }
        [FirestoreProperty] 
        public string role { get; set; }
        public string Password { get; set; }
    }
}