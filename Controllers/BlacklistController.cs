using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Type;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BlacklistController : Controller
    {
        private FirestoreDb firestoreDb;
        public BlacklistController()
        {
            firestoreDb = FirestoreDb.Create("locklock-47b1d");
        }

        public async Task<IActionResult> IndexAsync()
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                TempData["name"] = adminUID.Item2;
                TempData["surname"] = adminUID.Item3;

                List<BlackListModel> userBlackList = new List<BlackListModel>();

                DocumentReference adminReference = firestoreDb.Collection("users").Document(adminUID.Item1);
                DocumentSnapshot adminSnapshot = await adminReference.GetSnapshotAsync();
                AdminModel admin = adminSnapshot.ConvertTo<AdminModel>();

                foreach (string roomID in admin.rooms)
                {
                    Query blacklistQuery = firestoreDb.Collection("blacklist").WhereEqualTo("roomID", roomID);
                    QuerySnapshot blacklistQuerySnapshot = await blacklistQuery.GetSnapshotAsync();

                    DocumentReference roomReference = firestoreDb.Collection("room").Document(roomID);
                    DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                    RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                    foreach (DocumentSnapshot blacklistSnapshot in blacklistQuerySnapshot)
                    {
                        if (blacklistSnapshot.Exists)
                        {
                            BlackListDataModel blacklistData = blacklistSnapshot.ConvertTo<BlackListDataModel>();

                            DocumentReference userReference = firestoreDb.Collection("users").Document(blacklistData.userID);
                            DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                            UserModel userData = userSnapshot.ConvertTo<UserModel>();

                            BlackListModel userBlacklistData = new BlackListModel()
                            {
                                BlacklistID = blacklistSnapshot.Id,
                                userID = blacklistData.userID,
                                Name = userData.Firstname + " " + userData.Lastname,
                                Tel = userData.Tel,
                                Email = userData.Email,
                                RoomName = roomData.name
                            };
                            userBlackList.Add(userBlacklistData);
                        }
                        else
                        {
                            Console.WriteLine("Document does not exist!", blacklistSnapshot.Id);
                        }
                    }
                }
                return View(userBlackList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }

        }
        public async Task<IActionResult> addAsync(string userID, string roomID)
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            if (userID == null) return BadRequest();
            if (adminUID != null)
            {

                BlackListDataModel blackListData = new BlackListDataModel()
                {
                    userID = userID,
                    timeCreate = System.DateTime.UtcNow,
                    adminID = adminUID.Item1,
                    roomID = roomID
                };
                await firestoreDb.Collection("blacklist").AddAsync(blackListData);
                return RedirectToAction("Index", "BookingRoom", new { room = roomID });
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }
        public async Task<IActionResult> cancelAsync(string id)
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                DocumentReference blacklistReference = firestoreDb.Collection("blacklist").Document(id);
                await blacklistReference.DeleteAsync();
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }
        private async Task<Tuple<string, string, string>> verifyAdminTokenAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                DocumentReference userReference = firestoreDb.Collection("users").Document(decodedToken.Uid);
                DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                UserModel user = userSnapshot.ConvertTo<UserModel>();

                if (user.role == "admin")
                {
                    return new Tuple<string, string, string>(decodedToken.Uid, user.Firstname, user.Lastname);
                }
                else
                {
                    Console.WriteLine("User role");
                    return null;
                }
            }
            catch
            {
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }

        [FirestoreData]
        public class BlackListDataModel
        {
            public string BlacklistID { get; set; }
            [FirestoreProperty]
            public System.DateTime timeCreate { get; set; }
            [FirestoreProperty]
            public string userID { get; set; }
            [FirestoreProperty]
            public string adminID { get; set; }
            [FirestoreProperty]
            public string roomID { get; set; }

        }

    }
}