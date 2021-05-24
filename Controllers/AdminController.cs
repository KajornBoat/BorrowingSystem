using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class AdminController : Controller
    {
        private FirestoreDb firestoreDb;
        public AdminController()
        {
            firestoreDb = FirestoreDb.Create("locklock-47b1d");
        }
        public async Task<IActionResult> IndexAsync()
        {
            Tuple<string, string, string> adminUid = await verifyAdminTokenAsync();
            if (adminUid != null)
            {
                TempData["name"] = adminUid.Item2;
                TempData["surname"] = adminUid.Item3;
                DocumentReference userReference = firestoreDb.Collection("users").Document(adminUid.Item1);
                DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                AdminModel admin = userSnapshot.ConvertTo<AdminModel>();

                Query roomQuery = firestoreDb.Collection("room").WhereEqualTo("adminID", adminUid.Item1);
                QuerySnapshot roomQuerySnapshot = await roomQuery.GetSnapshotAsync();

                List<RoomModel> roomList = new List<RoomModel>();

                foreach (string roomID in admin.rooms)
                {
                    DocumentReference roomReference = firestoreDb.Collection("room").Document(roomID);
                    DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                    RoomModel room = roomSnapshot.ConvertTo<RoomModel>();

                    room.RoomID = roomSnapshot.Id;
                    roomList.Add(room);
                }
                return View(roomList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }

        public async Task<IActionResult> updateRoomAsync(RoomModel room)
        {
            Tuple<string, string, string> adminUid = await verifyAdminTokenAsync();
            if (adminUid != null)
            {
                DocumentReference roomReference = firestoreDb.Collection("room").Document(room.RoomID);
                Console.WriteLine(room.objName);
                Console.WriteLine(room.objNum);
                // await roomReference.SetAsync(room, SetOptions.Overwrite);
                await roomReference.UpdateAsync(new Dictionary<FieldPath, object>{
                    { new FieldPath("objName"), room.objName},
                    { new FieldPath("objNum"), room.objNum}
                });

                return RedirectToAction(nameof(Index));
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

    }
}